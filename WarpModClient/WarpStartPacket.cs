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

        internal static readonly Dictionary<long, WarpData> ActiveWarps = new Dictionary<long, WarpData>();

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

            if (!ActiveWarps.ContainsKey(message.GridId))
            {
                MatrixD matrix = ToMatrix(message.StartMatrixValues);

                ActiveWarps[message.GridId] = new WarpData
                {
                    StartMatrix = matrix,
                    StepVector = message.StepVector
                };
                IMyEntity ent;
                if (MyAPIGateway.Entities.TryGetEntityById(message.GridId, out ent))
                {
                    ent.PositionComp.SetWorldMatrix(ref matrix);
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            foreach (var key in ActiveWarps.Keys.ToList())
            {
                IMyEntity ent;
                if (MyAPIGateway.Entities.TryGetEntityById(key, out ent))
                {
                    var data = ActiveWarps[key];
                    var matrix = ent.WorldMatrix;
                    matrix.Translation += data.StepVector;
                    ent.PositionComp.SetWorldMatrix(ref matrix);
                    ent.Physics?.ClearSpeed();

                    // ✅ Use classic cast for .NET Framework 4.8
                    var grid = ent as IMyCubeGrid;
                    if (grid != null)
                    {
                        WarpTrailRenderer.DrawWarpTrailsFromThrusters(grid);
                    }
                }
            }
        }



        private static MatrixD ToMatrix(double[] arr)
        {
            return new MatrixD
            {
                M11 = arr[0],
                M12 = arr[1],
                M13 = arr[2],
                M14 = arr[3],
                M21 = arr[4],
                M22 = arr[5],
                M23 = arr[6],
                M24 = arr[7],
                M31 = arr[8],
                M32 = arr[9],
                M33 = arr[10],
                M34 = arr[11],
                M41 = arr[12],
                M42 = arr[13],
                M43 = arr[14],
                M44 = arr[15]
            };
        }
    }

    public struct WarpData
    {
        public MatrixD StartMatrix;
        public Vector3D StepVector;
    }
}
