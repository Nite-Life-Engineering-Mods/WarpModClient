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
        private static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(15); // Can make this configurable
        public bool EnteredCharging = false;
        public bool EnteredWarping = false;
        public bool EnteredCooldown = false;

        public static bool BeginCooldown(long gridId, int ticks = 120)
        {
            ClientWarpState state;
            if (WarpStartReceiver.ActiveWarps.TryGetValue(gridId, out state))
            {
                state.State = WarpVisualState.Cooldown;
                state.CooldownTicksRemaining = ticks;
                return true;
            }
            return false;
        }


        public static bool IsCharging(long gridId)
        {
            //MyAPIGateway.Utilities.ShowNotification($"Checking if is Charging.", 4000, "Red");

            ClientWarpState state;
            return WarpStartReceiver.ActiveWarps.TryGetValue(gridId, out state) && state.State == WarpVisualState.Charging;
        }

        public static bool IsCoolingDown(long gridId, out TimeSpan remaining)
        {
            ClientWarpState active;
            if (WarpStartReceiver.ActiveWarps.TryGetValue(gridId, out active) &&
            active.State == WarpVisualState.Cooldown)
            {
                double time = active.CooldownTicksRemaining / 60.0;
                remaining = TimeSpan.FromSeconds(time);
                // Is cooling down.
                return true;
            }
            remaining = TimeSpan.FromSeconds(0);
            return false;

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

            if (PendingWarpData != null)
            {
                MyAPIGateway.Utilities.ShowNotification($"Pending Warp Data passed.", 5000, "White");

            }

            if (ChargingTicksRemaining > 0 || PendingWarpData == null)
                return;
            MyAPIGateway.Multiplayer.SendMessageToServer(WarpControls.WARP_REQUEST_ID, PendingWarpData);
            PendingWarpData = null; // Clear it to avoid duplicate sends
        }


        public static TimeSpan? GetCooldownRemaining(long gridId)
        {
            ClientWarpState state;
            if (WarpStartReceiver.ActiveWarps.TryGetValue(gridId, out state) &&
                state.State == WarpVisualState.Cooldown)
            {
                double secondsRemaining = state.CooldownTicksRemaining / 60.0;
                return TimeSpan.FromSeconds(secondsRemaining);
            }

            return null;
        }

    }



}
