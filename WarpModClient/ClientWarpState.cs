using System.Collections.Generic;
using System;
using Sandbox.ModAPI;
using VRageMath;

namespace WarpDriveClient
{
    public enum WarpVisualState
    {
        Idle,
        Charging,
        Warping,
        Cooldown
    }

    public class ClientWarpState
    {
        public long GridId;
        public Vector3D StepVector;
        public MatrixD StartMatrix;
        public WarpVisualState State;
        public int ChargingTicksRemaining;
        public int CooldownTicksRemaining;
        public byte[] PendingWarpData;
        private static readonly Dictionary<long, DateTime> GridCooldowns = new Dictionary<long, DateTime>();
        private static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(15); // Can make this configurable

        public static void BeginCooldown(long gridId)
        {
            GridCooldowns[gridId] = DateTime.UtcNow + DefaultCooldown;
        }

        public static bool IsCharging(long gridId)
        {
            //MyAPIGateway.Utilities.ShowNotification($"Checking if is Charging.", 4000, "Red");

            ClientWarpState state;
            return WarpStartReceiver.ActiveWarps.TryGetValue(gridId, out state) && state.State == WarpVisualState.Charging;
        }

        public static bool IsCoolingDown(long gridId)
        {
            DateTime until;
            return GridCooldowns.TryGetValue(gridId, out until) && DateTime.UtcNow < until;
        }

        public static bool TryCancelWarp(long gridId)
        {
            if (WarpStartReceiver.ActiveWarps.ContainsKey(gridId))
            {
                WarpStartReceiver.ActiveWarps.Remove(gridId);
                MyAPIGateway.Utilities.ShowNotification("Warp canceled.", 2000, "Red");
                return true;
            }
            return false;
        }

        public void TrySendPendingRequest()
        {
            if (ChargingTicksRemaining.ToString() == null)
                MyAPIGateway.Utilities.ShowNotification($"Charging Ticks not passed.", 1000, "Red");
            //MyAPIGateway.Utilities.ShowNotification($"Charging: {ChargingTicksRemaining}", 1000, "Red");

            if (PendingWarpData == null)
                MyAPIGateway.Utilities.ShowNotification($"Pending Warp Data not passed.", 1000, "Red");

            if (ChargingTicksRemaining > 0 || PendingWarpData == null)
                return;
            MyAPIGateway.Multiplayer.SendMessageToServer(WarpControls.WARP_REQUEST_ID, PendingWarpData);
            PendingWarpData = null; // Clear it to avoid duplicate sends
        }


        public static TimeSpan? GetCooldownRemaining(long gridId)
        {
            DateTime until;
            if (GridCooldowns.TryGetValue(gridId, out until))
            {
                var remaining = until - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? (TimeSpan?)remaining : null;

            }
            return null;
        }
    }



    }
