using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using VRage.Game.Components;
using VRage.ModAPI;

namespace WarpDriveClient
{
    [ProtoContract]
    public class WarpCorrectionPacket
    {
        [ProtoMember(1)] public long GridId;
        [ProtoMember(2)] public Vector3D ServerPosition;
    }

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class WarpCorrectionReceiver : MySessionComponentBase
    {
        private const ushort PACKET_ID_CORRECTION = 42156;

        public override void LoadData()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(PACKET_ID_CORRECTION, OnCorrection);
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(PACKET_ID_CORRECTION, OnCorrection);
        }

        private void OnCorrection(ushort id, byte[] data, ulong sender, bool fromServer)
        {
            var msg = MyAPIGateway.Utilities.SerializeFromBinary<WarpCorrectionPacket>(data);
            IMyEntity ent;
            if (!MyAPIGateway.Entities.TryGetEntityById(msg.GridId, out ent))
                return;

            var currentPos = ent.PositionComp.GetPosition();
            double error = Vector3D.DistanceSquared(currentPos, msg.ServerPosition);

            // Snap threshold: only correct if client is more than 5 meters off
            if (error > 25.0)
            {
                var m = ent.WorldMatrix;
                m.Translation = msg.ServerPosition;
                ent.PositionComp.SetWorldMatrix(ref m);
                ent.Physics?.ClearSpeed();
            }
        }
    }
}
