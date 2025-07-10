using System.Collections.Generic;
using System;
using Sandbox.ModAPI;
using VRageMath;
using ProtoBuf;

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
        public bool EnteredCharging = false;
        public bool EnteredWarping = false;
        public bool EnteredCooldown = false;
        public double speed;
        public Vector3D LastCorrection;
        public int InternalTickCounter;

        public static bool BeginCooldown(long gridId, int ticks = 900)
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
        public static bool TryGetWarpState(long gridId, out ClientWarpState state)
        {
            return WarpStartReceiver.ActiveWarps.TryGetValue(gridId, out state);
        }


        public static bool IsWarping(long gridId)
        {
            ClientWarpState state;
            return WarpStartReceiver.ActiveWarps.TryGetValue(gridId, out state) && state.State == WarpVisualState.Warping;
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
                if (IsWarping(gridId))
                {
                    var msg = new WarpRequestMessage
                    {
                        GridId = gridId,
                        Destination = Vector3D.Zero,
                        Speed = 0,
                        Mode = WarpMode.Free
                    };

                    var data = MyAPIGateway.Utilities.SerializeToBinary(msg); MyAPIGateway.Multiplayer.SendMessageToServer(WarpControls.WARP_REQUEST_ID, data);
                    MyAPIGateway.Utilities.ShowNotification("Warp has been completed.", 6000, "White");
                    return true;
                }
                if (IsCharging(gridId))
                {
                    WarpStartReceiver.ActiveWarps.Remove(gridId);
                    MyAPIGateway.Utilities.ShowNotification("Charging has been cancelled.", 6000, "White");
                    return true;
                }
            }
            return false;
        }

        public void TrySendPendingRequest()
        {
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
