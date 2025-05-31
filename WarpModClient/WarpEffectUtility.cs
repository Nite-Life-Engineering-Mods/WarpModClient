using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;
using System.Collections.Generic;
using System;

namespace WarpDriveClient
{
    public static class WarpEffectUtility
    {
        private static readonly Dictionary<long, MyParticleEffect> ActiveEffects = new Dictionary<long, MyParticleEffect>();

        public static void PlayEffect(IMyCubeGrid grid, string effectName = "Warp", float scale = 2f)
        {
            if (grid == null || ActiveEffects.ContainsKey(grid.EntityId))
                return;

            MatrixD matrix = GetWarpMatrix(grid);

            Vector3D position = matrix.Translation;
            MyParticleEffect effect;

            if (MyParticlesManager.TryCreateParticleEffect(effectName, ref matrix, ref position, uint.MaxValue, out effect))
            {
                effect.UserScale = scale;
                ActiveEffects[grid.EntityId] = effect;
            }
        }

        public static void Update(IMyCubeGrid grid)
        {
            MyParticleEffect effect;
            if (grid == null || !ActiveEffects.TryGetValue(grid.EntityId, out effect))
                return;

            if (effect.IsStopped)
            {
                ActiveEffects.Remove(grid.EntityId);
                return;
            }

            MatrixD matrix = GetWarpMatrix(grid);
            effect.WorldMatrix = matrix;
        }

        public static void StopEffect(IMyCubeGrid grid)
        {
            if (grid == null)
                return;
            MyParticleEffect effect;
            if (ActiveEffects.TryGetValue(grid.EntityId, out effect))
            {
                effect.Stop();
                ActiveEffects.Remove(grid.EntityId);
            }
        }

        private static MatrixD GetWarpMatrix(IMyCubeGrid grid)
        {
            MatrixD matrix = grid.WorldMatrix;

            // Flip orientation so the particle faces backward
            matrix.Forward = -matrix.Forward;
            matrix.Up = -matrix.Up;
            matrix.Right = Vector3D.Cross(matrix.Forward, matrix.Up);

            // Offset in front of the ship (visual forward)
            double offset = Math.Max(grid.LocalAABB.HalfExtents.Z * 2.0, 30.0);
            matrix.Translation -= matrix.Forward * offset;

            return matrix;
        }
    }
}
