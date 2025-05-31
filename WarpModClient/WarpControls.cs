using ProtoBuf;
using Sandbox.Game.Debugging;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace WarpDriveClient
{

    public enum WarpMode
    {
        Guided,
        Free
    }


    [ProtoContract]
    public class WarpRequestMessage
    {
        [ProtoMember(1)] public long GridId;
        [ProtoMember(2)] public Vector3D? Destination;
        [ProtoMember(3)] public double Speed;
        [ProtoMember(4)] public WarpMode Mode;
    }

    public static class WarpControls
    {
        private static bool _controlsCreated = false;
        private static readonly Dictionary<long, string> gpsInputStorage = new Dictionary<long, string>();
        public static Dictionary<long, ClientWarpState> ChargingWarps = new Dictionary<long, ClientWarpState>();

        // Define warp speeds per block subtype
        private static readonly Dictionary<string, double> BlockSubtypeSpeeds = new Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "CivilianWarpDriveSmall", 25000 },
            { "CivilianWarpDriveLarge", 50000 }
        };

        public static ushort WARP_REQUEST_ID = 42700;


        public static double Get_Speed(string Subtype)
        {
            double speed;
            if (BlockSubtypeSpeeds.TryGetValue(Subtype, out speed))
            {
                return speed / 1000;
            }
            return 0;
        }

        public static void Create()
        {
            if (_controlsCreated || MyAPIGateway.Utilities.IsDedicated)
                return;
            string val;

            MyAPIGateway.TerminalControls.CustomControlGetter += (block, controls) =>
            {
                if (block?.BlockDefinition.SubtypeName == null || !BlockSubtypeSpeeds.ContainsKey(block.BlockDefinition.SubtypeName))
                    return;

                var gpsInput = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyTerminalBlock>("WarpGPSInput");
                gpsInput.Title = MyStringId.GetOrCompute("Target GPS");
                gpsInput.Tooltip = MyStringId.GetOrCompute("Enter a GPS coordinate or leave blank for Free Warp");
                gpsInput.Getter = b => gpsInputStorage.TryGetValue(b.EntityId, out val) ? new StringBuilder(val) : new StringBuilder();
                gpsInput.Setter = (b, v) => gpsInputStorage[b.EntityId] = v.ToString();
                gpsInput.Enabled = b => true;
                gpsInput.Visible = b => true;

                var warpButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>("StartWarp");
                warpButton.Title = MyStringId.GetOrCompute("Toggle Warp");
                warpButton.Tooltip = MyStringId.GetOrCompute("Start guided or free warp");
                warpButton.Enabled = b => true;
                warpButton.Visible = b => true;
                warpButton.Action = ToggleWarpAction;

                controls.Add(gpsInput);
                controls.Add(warpButton);
            };

            var action = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("ToggleWarp");
            action.Name = new StringBuilder("Toggle Warp");
            action.Icon = "Textures\\GUI\\Icons\\Actions\\Toggle.dds";
            action.Enabled = block => block != null && BlockSubtypeSpeeds.ContainsKey(block.BlockDefinition.SubtypeName);
            action.ValidForGroups = false;
            action.Action = ToggleWarpAction;
            action.Writer = (block, builder) => builder.Append("Warp");

            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(action);


            _controlsCreated = true;
        }

        private static void ToggleWarpAction(IMyTerminalBlock block)
        {
            string val;
            var blockRef = block as IMyCubeBlock;
            if (blockRef == null)
                return;

            var grid = blockRef.CubeGrid;
            if (grid == null)
                return;
            ClientWarpState existing;
            if (WarpStartReceiver.ActiveWarps.TryGetValue(grid.EntityId, out existing) &&
                existing.State == WarpVisualState.Cooldown)
            {
                MyAPIGateway.Utilities.ShowNotification($"Warp is cooling down...", 5000, "Red");
                return;
            }

            if (ClientWarpState.IsCharging(grid.EntityId))
            {
                //MyAPIGateway.Utilities.ShowNotification($"Warp charge cancelled.", 4000, "Red");
                ClientWarpState.TryCancelWarp(grid.EntityId);
                return;
            }

            if (ClientWarpState.IsWarping(grid.EntityId))
            {
                ClientWarpState.TryCancelWarp(grid.EntityId);
                ClientWarpState.BeginCooldown(grid.EntityId);
                return;
            }

            string input = gpsInputStorage.TryGetValue(blockRef.EntityId, out val) ? val : null;

            string gpsName = "Destination";
            Vector3D? destination = null;
            Vector3D gps;
            string name;
            double configuredSpeed = 0;
            if (TryParseGPS(input, out gps, out name))
            {
                destination = gps;
                gpsName = name;
                //gpsInputStorage.Remove(blockRef.EntityId); // Removes info from the gps field.
            }

            WarpMode mode = destination.HasValue ? WarpMode.Guided : WarpMode.Free;

            double speed = BlockSubtypeSpeeds.TryGetValue(blockRef.BlockDefinition.SubtypeName, out configuredSpeed)
                ? configuredSpeed : 30000;
            long entityId = block.EntityId;

            if (mode == WarpMode.Guided)
            {
                speed = speed / 2; // Half speed for guided warp.
            }

            var msg = new WarpRequestMessage
            {
                GridId = grid.EntityId,
                Destination = destination,
                Speed = speed,
                Mode = mode
            };

            var data = MyAPIGateway.Utilities.SerializeToBinary(msg);
            ClientWarpState state;
            if (!WarpStartReceiver.ActiveWarps.TryGetValue(grid.EntityId, out state))
            {
                state = new ClientWarpState
                {
                    GridId = grid.EntityId,
                    StepVector = Vector3D.Zero, // Will be updated on server response
                    StartMatrix = grid.WorldMatrix,
                    ChargingTicksRemaining = 10 * 60, // 10 seconds
                    CooldownTicksRemaining = 0,
                    State = WarpVisualState.Charging,
                    PendingWarpData = data,
                    speed = speed
                };
                WarpStartReceiver.ActiveWarps[grid.EntityId] = state;
            }
            else
            {
                state.StepVector = Vector3D.Zero;
                state.StartMatrix = grid.WorldMatrix;
                state.State = WarpVisualState.Charging;
                state.ChargingTicksRemaining = 10 * 60;
                state.CooldownTicksRemaining = 0;
                state.PendingWarpData = data;
                state.speed = speed;
            }

            MyAPIGateway.Utilities.ShowNotification(mode == WarpMode.Guided ? $"Charging for warp to {gpsName}..." : $"Charging...", 6000, "White");
        }

        private static bool TryParseGPS(string text, out Vector3D result, out string name)
        {
            result = Vector3D.Zero;
            name = "Destination";

            if (string.IsNullOrWhiteSpace(text) || !text.StartsWith("GPS:"))
                return false;

            var parts = text.Split(':');
            if (parts.Length < 5)
                return false;

            name = parts[1];

            return double.TryParse(parts[2], out result.X)
                && double.TryParse(parts[3], out result.Y)
                && double.TryParse(parts[4], out result.Z);
        }
    }
}
