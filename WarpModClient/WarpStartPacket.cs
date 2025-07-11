﻿using ProtoBuf;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace WarpDriveClient
{

    [ProtoContract]
    public class WarpStartMessage
    {
        [ProtoMember(1)] public long GridId;
        [ProtoMember(2)] public double[] StartMatrixValues;
        [ProtoMember(3)] public Vector3D StepVector;

        public MatrixD ToMatrix()
        {
            var m = new MatrixD();
            var arr = StartMatrixValues;
            m.M11 = arr[0]; m.M12 = arr[1]; m.M13 = arr[2]; m.M14 = arr[3];
            m.M21 = arr[4]; m.M22 = arr[5]; m.M23 = arr[6]; m.M24 = arr[7];
            m.M31 = arr[8]; m.M32 = arr[9]; m.M33 = arr[10]; m.M34 = arr[11];
            m.M41 = arr[12]; m.M42 = arr[13]; m.M43 = arr[14]; m.M44 = arr[15];
            return m;
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class WarpStartReceiver : MySessionComponentBase
    {
        private const ushort PACKET_ID_START = 42154;


        public static Dictionary<long, ClientWarpState> ActiveWarps = new Dictionary<long, ClientWarpState>();

        public override void LoadData()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(PACKET_ID_START, OnStart);
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(PACKET_ID_START, OnStart);
        }

        private void OnStart(ushort id, byte[] data, ulong sender, bool fromServer)
        {
            var message = MyAPIGateway.Utilities.SerializeFromBinary<WarpStartMessage>(data);
            IMyEntity ent;
            if (MyAPIGateway.Entities.TryGetEntityById(message.GridId, out ent))
            {
                ClientWarpState state;
                if (!ActiveWarps.TryGetValue(ent.EntityId, out state))
                {
                    state = new ClientWarpState
                    {
                        GridId = message.GridId,
                        EnteredWarping = false,
                        speed = message.StepVector.Length() * 60, // meters/sec
                    };
                    ActiveWarps[ent.EntityId] = state;
                }

                state.StartMatrix = message.ToMatrix();
                state.StepVector = message.StepVector;
                ent.PositionComp.SetPosition(state.StartMatrix.Translation);
                state.State = WarpVisualState.Warping;
                ent.Physics?.ClearSpeed();
            }
        }


        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session != null)
                if (MyAPIGateway.Session.IsServer)
                    return;
            foreach (var pair in ActiveWarps.ToList())
            {
                
                var warp = pair.Value;

                if (warp.State == WarpVisualState.Cooldown)
                {
                    if (!warp.EnteredCooldown)
                    {
                        // Restores controls. This does not run again.
                        ControlUtility.RestoreControls(MyAPIGateway.Entities.GetEntityById(warp.GridId) as IMyCubeGrid);
                        warp.EnteredCooldown = true;
                    }
                    if (--warp.CooldownTicksRemaining <= 0)
                        ActiveWarps.Remove(warp.GridId);

                    continue;
                }

                IMyEntity ent;
                if (!MyAPIGateway.Entities.TryGetEntityById(warp.GridId, out ent))
                    continue;

                switch (warp.State)
                {
                    case WarpVisualState.Charging:
                        warp.StartMatrix = ent.WorldMatrix;
                        if (!warp.EnteredCharging)
                        {
                            IMyCubeGrid grid = ent as IMyCubeGrid;
                            WarpEffectUtility.PlayEffect(grid);
                            SoundUtility.Play(grid, WarpSounds.WarpCharge);
                            warp.EnteredCharging = true;
                        }

                        WarpEffectUtility.Update(ent as IMyCubeGrid);

                        if (--warp.ChargingTicksRemaining <= 0)
                        {
                            IMyCubeGrid grid = ent as IMyCubeGrid;
                            WarpEffectUtility.StopEffect(grid);
                            warp.EnteredWarping = false; // reset flag for next state
                            warp.TrySendPendingRequest();
                        }
                        break;

                    case WarpVisualState.Warping:
                        if (!warp.EnteredWarping)
                        {
                            ControlUtility.LockControls(ent as IMyCubeGrid);
                            warp.EnteredWarping = true;
                        }
                        if (!SoundUtility.IsPlaying(ent))
                        {
                            SoundUtility.Play(ent as IMyCubeGrid, WarpSounds.WarpTravel);
                        }

                        var matrix = ent.WorldMatrix;
                        matrix.Translation += warp.StepVector;
                        Vector3D current = matrix.Translation;
                        Vector3D desired = warp.LastCorrection;
                        double drift = Vector3D.Distance(current, desired); // in meters
                        double factor = drift / 10000.0;
                        //MyAPIGateway.Utilities.ShowMessage("Drift", $"{factor}");
                        if (factor < 0.01) factor = 0.25;
                        else if (factor > 0.3) factor = 0.5;

                        Vector3D corrected = Vector3D.Lerp(current, desired, factor);
                        matrix.Translation = corrected;


                        if (++warp.InternalTickCounter % 3 == 0)
                        {
                            ent.PositionComp.SetPosition(corrected);
                        }

                        ent.Physics?.ClearSpeed();
                        if (ent != null)
                        {
                            WarpTrailRenderer.DrawWarpTrailsFromThrusters(ent as IMyCubeGrid);
                        }

                        break;
                }

            }

        }

        public static List<IMyPlayer> GetPlayersInGrid(IMyCubeGrid grid)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            List<IMyPlayer> playersInGrid = new List<IMyPlayer>();

            foreach (var player in players)
            {
                if (player.Controller == null)
                    continue;

                var controller = player.Controller.ControlledEntity;
                if (controller == null)
                    continue;

                var controlledEntity = controller.Entity;
                if (controlledEntity == null)
                    continue;

                var block = controlledEntity as IMyCubeBlock;
                if (block == null)
                    continue;

                if (block.CubeGrid == grid)
                    playersInGrid.Add(player);
            }

            return playersInGrid;
        }



        public struct WarpData
    {
        public MatrixD StartMatrix;
        public Vector3D StepVector;
    }
}
    }
