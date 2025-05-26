using ProtoBuf;
using Sandbox.Game.Debugging;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
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

        // Define warp speeds per block subtype
        private static readonly Dictionary<string, double> BlockSubtypeSpeeds = new Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "CivilianWarpDriveSmall", 25000 },
            { "CivilianWarpDriveLarge", 50000 }
        };

        public static ushort WARP_REQUEST_ID = 42700;

        public static void Create()
        {
            if (_controlsCreated || MyAPIGateway.Utilities.IsDedicated)
                return;
            string val;
            double _speed;

            IMyTerminalAction startWarp = MyAPIGateway.TerminalControls.CreateAction<IMyUpgradeModule>("ToggleWarp");
            startWarp.Enabled = block => BlockSubtypeSpeeds.ContainsKey(block.BlockDefinition.SubtypeName);
            startWarp.Name = new StringBuilder("Warp");
            startWarp.Action = b =>
            {
                var block = b as IMyCubeBlock;
                if (block == null)
                    return;

                var grid = block.CubeGrid;
                if (grid == null)
                    return;
                string input = gpsInputStorage.TryGetValue(block.EntityId, out val) ? val : null;

                string gpsName = "Destination";
                Vector3D? destination = null;
                Vector3D gps;
                string name;
                if (TryParseGPS(input, out gps, out name))
                {
                    destination = gps;
                    gpsName = name;
                }

                WarpMode mode = destination.HasValue ? WarpMode.Guided : WarpMode.Free;

                MyAPIGateway.Utilities.ShowNotification(mode == WarpMode.Guided ? $"Initiating warp to {gpsName}" : "Initiating free warp", 3000, "White");

                double speed = 30000; // fallback default
                double configuredSpeed = 0;
                var subtype = block.BlockDefinition.SubtypeName;
                if (!string.IsNullOrWhiteSpace(subtype) && BlockSubtypeSpeeds.TryGetValue(subtype, out configuredSpeed))
                {
                    speed = configuredSpeed;
                }

                var msg = new WarpRequestMessage
                {
                    GridId = grid.EntityId,
                    Destination = destination,
                    Speed = speed,
                    Mode = mode
                };

                MyAPIGateway.Multiplayer.SendMessageToServer(WARP_REQUEST_ID, MyAPIGateway.Utilities.SerializeToBinary(msg));

            };
            startWarp.Icon = "Textures\\GUI\\Icons\\Actions\\Toggle.dds";
            MyAPIGateway.TerminalControls.AddAction<IMyUpgradeModule>(startWarp);

            var gpsInput = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyUpgradeModule>("WarpGPSInput");
            gpsInput.Title = MyStringId.GetOrCompute("Target GPS");
            gpsInput.Tooltip = MyStringId.GetOrCompute("Enter a GPS coordinate or leave blank for Free Warp");
            gpsInput.Getter = b => gpsInputStorage.TryGetValue(b.EntityId, out val) ? new StringBuilder(val) : new StringBuilder();
            gpsInput.Setter = (b, v) => gpsInputStorage[b.EntityId] = v.ToString();
            gpsInput.Enabled = b => true;
            gpsInput.Visible = block => BlockSubtypeSpeeds.ContainsKey(block.BlockDefinition.SubtypeName);

            MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(gpsInput);

            var warpButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyUpgradeModule>("StartWarp");
            warpButton.Title = MyStringId.GetOrCompute("Toggle Warp");
            warpButton.Tooltip = MyStringId.GetOrCompute("Start guided or free warp");
            warpButton.Visible = block => BlockSubtypeSpeeds.ContainsKey(block.BlockDefinition.SubtypeName);
            warpButton.Action = b =>
            {
                var block = b as IMyCubeBlock;
                if (block == null)
                    return;

                var grid = block.CubeGrid;
                if (grid == null)
                    return;
                string input = gpsInputStorage.TryGetValue(block.EntityId, out val) ? val : null;

                string gpsName = "Destination";
                Vector3D? destination = null;
                Vector3D gps;
                string name;
                if (TryParseGPS(input, out gps, out name))
                {
                    destination = gps;
                    gpsName = name;
                }

                WarpMode mode = destination.HasValue ? WarpMode.Guided : WarpMode.Free;

                MyAPIGateway.Utilities.ShowNotification(mode == WarpMode.Guided ? $"Initiating warp to {gpsName}" : "Initiating free warp", 3000, "White");

                double speed = 30000; // fallback default
                double configuredSpeed = 0;
                var subtype = block.BlockDefinition.SubtypeName;
                if (!string.IsNullOrWhiteSpace(subtype) && BlockSubtypeSpeeds.TryGetValue(subtype, out configuredSpeed))
                {
                    speed = configuredSpeed;
                }

                var msg = new WarpRequestMessage
                {
                    GridId = grid.EntityId,
                    Destination = destination,
                    Speed = speed,
                    Mode = mode
                };

                MyAPIGateway.Multiplayer.SendMessageToServer(WARP_REQUEST_ID, MyAPIGateway.Utilities.SerializeToBinary(msg));

            };
            warpButton.Enabled = b => true;
            MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(warpButton);

            var toggleAction = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("ToggleWarp");
            toggleAction.Name = new StringBuilder("Toggle Warp");
            toggleAction.Action = b =>
            {
                var block = b as IMyCubeBlock;
                if (block == null)
                    return;

                var grid = block.CubeGrid;
                if (grid == null)
                    return;
                string input = gpsInputStorage.TryGetValue(block.EntityId, out val) ? val : null;

                string gpsName = "Destination";
                Vector3D? destination = null;
                Vector3D gps;
                string name;
                if (TryParseGPS(input, out gps, out name))
                {
                    destination = gps;
                    gpsName = name;
                }

                WarpMode mode = destination.HasValue ? WarpMode.Guided : WarpMode.Free;

                MyAPIGateway.Utilities.ShowNotification(mode == WarpMode.Guided ? $"Initiating warp to {gpsName}" : "Initiating free warp", 3000, "White");

                double speed = 30000; // fallback default
                double configuredSpeed = 0;
                var subtype = block.BlockDefinition.SubtypeName;
                if (!string.IsNullOrWhiteSpace(subtype) && BlockSubtypeSpeeds.TryGetValue(subtype, out configuredSpeed))
                {
                    speed = configuredSpeed;
                }

                var msg = new WarpRequestMessage
                {
                    GridId = grid.EntityId,
                    Destination = destination,
                    Speed = speed,
                    Mode = mode
                };

                MyAPIGateway.Multiplayer.SendMessageToServer(WARP_REQUEST_ID, MyAPIGateway.Utilities.SerializeToBinary(msg));
            };
            toggleAction.Writer = (block, builder) => builder.Append("Toggle Warp");
            toggleAction.Enabled = block => block?.BlockDefinition.SubtypeName?.ToLower().Contains("warpdrive") == true;
            toggleAction.ValidForGroups = false;

            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(toggleAction);


            _controlsCreated = true;
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
