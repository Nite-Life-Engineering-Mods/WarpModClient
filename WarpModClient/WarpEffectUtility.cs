using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;
using System.Collections.Generic;

namespace WarpDriveClient
{
    public static class WarpEffectUtility
    {
        private const string DefaultEffectName = "Warp";
        private const float DefaultScale = 1.5f;

        public static void PlayEffect(IMyCubeGrid grid, string effectName = DefaultEffectName, float scale = DefaultScale)
        {
            if (grid == null)
                return;

            MatrixD matrix = grid.WorldMatrix;
            matrix.Translation = grid.GetPosition();
            MyParticleEffect effect;
            if (MyParticlesManager.TryCreateParticleEffect(effectName, out effect))
            {
                effect.WorldMatrix = matrix;
                effect.UserScale = scale;
            }
        }
    }
}
