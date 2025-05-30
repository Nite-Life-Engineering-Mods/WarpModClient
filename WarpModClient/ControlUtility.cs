using Sandbox.ModAPI;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using System.Collections.Generic;

namespace WarpDriveClient
{
    public static class ControlUtility
    {
        private struct GyroState
        {
            public bool Override;
            public float Pitch, Yaw, Roll;

            public GyroState(bool ov, float pitch, float yaw, float roll)
            {
                Override = ov;
                Pitch = pitch;
                Yaw = yaw;
                Roll = roll;
            }
        }

        private struct CockpitControlState
        {
            public bool Thrusters;
            public bool Wheels;
            public bool Handbrake;

            public CockpitControlState(bool thrusters, bool wheels, bool handbrake)
            {
                Thrusters = thrusters;
                Wheels = wheels;
                Handbrake = handbrake;
            }
        }

        private static readonly Dictionary<long, CockpitControlState> controlBackup =
            new Dictionary<long, CockpitControlState>();
        private static readonly Dictionary<long, GyroState> gyroBackup =
            new Dictionary<long, GyroState>();


        public static void LockControls(IMyCubeGrid grid)
        {
            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks, b => b.FatBlock is IMyCockpit);

            foreach (var block in blocks)
            {
                var cockpit = block.FatBlock as IMyCockpit;
                if (cockpit?.IsUnderControl == true)
                {
                    long id = cockpit.EntityId;

                    if (!controlBackup.ContainsKey(id))
                    {
                        controlBackup[id] = new CockpitControlState(
                            cockpit.ControlThrusters,
                            cockpit.ControlWheels,
                            cockpit.HandBrake
                        );
                    }

                    cockpit.ControlThrusters = false;
                    cockpit.ControlWheels = false;
                    cockpit.HandBrake = true;
                }
            }
            var gyroBlocks = new List<IMyGyro>();
            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid)
                ?.GetBlocksOfType(gyroBlocks);

            foreach (var gyro in gyroBlocks)
            {
                if (!gyroBackup.ContainsKey(gyro.EntityId))
                {
                    gyroBackup[gyro.EntityId] = new GyroState(
                        gyro.GyroOverride,
                        gyro.Pitch,
                        gyro.Yaw,
                        gyro.Roll
                    );
                }

                gyro.GyroOverride = true;
                gyro.Pitch = 0f;
                gyro.Yaw = 0f;
                gyro.Roll = 0f;
            }

        }

        public static void RestoreControls(IMyCubeGrid grid)
        {
            if (grid == null)
                return;

            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks, b => b.FatBlock is IMyCockpit);

            foreach (var block in blocks)
            {
                var cockpit = block.FatBlock as IMyCockpit;
                if (cockpit == null)
                    continue;

                long id = cockpit.EntityId;
                CockpitControlState original;
                if (controlBackup.TryGetValue(id, out original))
                {
                    cockpit.ControlThrusters = original.Thrusters;
                    cockpit.ControlWheels = original.Wheels;
                    cockpit.HandBrake = original.Handbrake;
                    controlBackup.Remove(id);
                }
            }
            var gyroBlocks = new List<IMyGyro>();
            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid)
                ?.GetBlocksOfType(gyroBlocks);

            foreach (var gyro in gyroBlocks)
            {
                GyroState g;
                if (gyroBackup.TryGetValue(gyro.EntityId, out g))
                {
                    gyro.GyroOverride = g.Override;
                    gyro.Pitch = g.Pitch;
                    gyro.Yaw = g.Yaw;
                    gyro.Roll = g.Roll;

                    gyroBackup.Remove(gyro.EntityId);
                }
            }

        }
    }
}
