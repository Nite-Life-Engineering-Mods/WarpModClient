using ProtoBuf;
using Sandbox.Game;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
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
                if (WarpStartReceiver.ActiveWarps.TryGetValue(ent.EntityId, out state))
                {
                    state.StartMatrix = message.ToMatrix();
                    state.StepVector = message.StepVector;
                    ent.PositionComp.SetWorldMatrix(ref state.StartMatrix);
                    ent.Physics?.ClearSpeed();
                    SoundUtility.Play(ent as IMyCubeGrid, WarpSounds.WarpTravel);
                }
                
            }

            

        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session != null)
                if (MyAPIGateway.Session.IsServer)
                    return;
            foreach (var kv in WarpStartReceiver.ActiveWarps)
            {
                MyAPIGateway.Utilities.ShowNotification($"State: {kv.Value.State}", 20, "Red");
                kv.Value.TrySendPendingRequest();
            }
            foreach (var pair in ActiveWarps.ToList())
            {
                var warp = pair.Value;
                IMyEntity ent;

                if (!MyAPIGateway.Entities.TryGetEntityById(warp.GridId, out ent))
                    continue;
                
                switch (warp.State)
                {
                    case WarpVisualState.Charging:
                        MyAPIGateway.Utilities.ShowNotification($"State: Charging", 20, "Red");
                        SoundUtility.Play(ent as IMyCubeGrid, WarpSounds.WarpCharge);
                        if (--warp.ChargingTicksRemaining <= 0)
                        {
                            MyAPIGateway.Utilities.ShowNotification($"State: Warping", 20, "Red");

                            warp.State = WarpVisualState.Warping;
                            
                        }
                        break;

                    case WarpVisualState.Warping:
                        var matrix = ent.WorldMatrix;
                        matrix.Translation += warp.StepVector;
                        ent.PositionComp.SetWorldMatrix(ref matrix);
                        ent.Physics?.ClearSpeed();

                        var grid = ent as IMyCubeGrid;
                        if (grid != null)
                            WarpTrailRenderer.DrawWarpTrailsFromThrusters(grid);
                        break;

                    case WarpVisualState.Cooldown:
                        if (--warp.CooldownTicksRemaining <= 0)
                            ActiveWarps.Remove(warp.GridId);
                        break;
                }
            }

        }

    public struct WarpData
    {
        public MatrixD StartMatrix;
        public Vector3D StepVector;
    }
}
    }
