using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
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

        public static void PlayEffect(IMyCubeGrid grid, string effectName = "Warp", float scale = 2f)
        {
            if (grid != null)
            {
                MatrixD matrix = grid.WorldMatrix;

                // Flip orientation so the particle faces backward (for visual forward motion)
                matrix.Forward = -matrix.Forward;
                matrix.Up = -matrix.Up;
                matrix.Right = Vector3D.Cross(matrix.Forward, matrix.Up);

                // Offset the matrix *backward* along the flipped forward vector (which is ship's front)
                double offset = Math.Max(grid.LocalAABB.HalfExtents.Z * 2.0, 30.0);
                matrix.Translation -= matrix.Forward * offset;

                Vector3D position = matrix.Translation;
                MyParticleEffect effect;
                if (MyParticlesManager.TryCreateParticleEffect(effectName, ref matrix, ref position, uint.MaxValue, out effect))
                {
                    effect.UserScale = scale;
                }

            }



        }
    }
}
