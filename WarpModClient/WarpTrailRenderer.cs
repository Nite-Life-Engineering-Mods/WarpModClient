using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using Sandbox.Game.Entities;
using VRageMath;
using VRage.Utils;
using VRage.Game;
using System.Collections.Generic;

namespace WarpDriveClient
{
    public static class WarpTrailRenderer
    {
        public static void DrawWarpTrailsFromThrusters(IMyCubeGrid grid)
        {
            if (grid?.Physics == null || !grid.Physics.Enabled)
                return;

            var matrix = grid.WorldMatrix;
            var forwardDir = matrix.Forward;

            var thrusters = new List<IMyThrust>();
            var gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
            if (gridTerminal == null)
                return;

            gridTerminal.GetBlocksOfType(thrusters, t => t.IsFunctional);

            var material = MyStringId.GetOrCompute("SciFiEngineThrustMiddle");
            float alpha = 0.6f + 0.3f * (float)System.Math.Sin(MyAPIGateway.Session.GameplayFrameCounter * 0.1f);
            Vector4 baseColor = new Vector4(0.25f, 0.9f, 1.0f, alpha); // Cyan

            foreach (var thruster in thrusters)
            {
                var dir = thruster.WorldMatrix.Backward;
                if (Vector3D.Dot(dir, forwardDir) < 0.9)
                    continue;

                var start = thruster.WorldMatrix.Translation - dir * 1.5;
                var end = start - dir * 40;

                for (int i = 0; i < 2; i++)
                {
                    float t = (i + 1) / 2f;
                    float radius = 6f * (1f - t * 0.5f);
                    MySimpleObjectDraw.DrawLine(start, end, material, ref baseColor, radius);
                }
            }
        }
    }
}
