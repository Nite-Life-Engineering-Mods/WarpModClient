using Sandbox.ModAPI;
using ProtoBuf;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using Sandbox.Game;
using VRage.ModAPI;

namespace WarpDriveClient
{
        [ProtoContract]
        public class WarpStopMessage
        {
            [ProtoMember(1)] public long GridId;
            [ProtoMember(2)] public string Reason;
            [ProtoMember(3)] public int TimeMs = 5000;
            [ProtoMember(4)] public string Font = "Red";
        }

        [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
        public class WarpStopReceiver : MySessionComponentBase
        {
            private const ushort PACKET_ID_STOP = 42155;

            public override void LoadData()
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(PACKET_ID_STOP, OnStop);
            }

            protected override void UnloadData()
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(PACKET_ID_STOP, OnStop);
            }

            private void OnStop(ushort id, byte[] data, ulong sender, bool fromServer)
            {
                var msg = MyAPIGateway.Utilities.SerializeFromBinary<WarpStopMessage>(data);

                if (WarpStartReceiver.ActiveWarps.Remove(msg.GridId))
                {
                    IMyEntity ent;
                    if (MyAPIGateway.Entities.TryGetEntityById(msg.GridId, out ent))
                    {
                        var snapMatrix = ent.WorldMatrix;
                        snapMatrix.Translation = ent.PositionComp.GetPosition();
                        ent.PositionComp.SetWorldMatrix(ref snapMatrix);
                        ent.Physics?.ClearSpeed();
                        ClientWarpState.BeginCooldown(msg.GridId);
                    }

                    if (!string.IsNullOrWhiteSpace(msg.Reason))
                    {
                        MyAPIGateway.Utilities.ShowNotification(msg.Reason, msg.TimeMs, msg.Font);

                    }

            }
            else
                {
                    MyAPIGateway.Utilities.ShowMessage("WarpDebug", $"Stop packet received but GridId {msg.GridId} not found in ActiveWarps.");
                }
            }
        }
}
