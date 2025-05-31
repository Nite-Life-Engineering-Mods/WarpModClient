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
            if (grid?.Physics == null)
                return;

            ClientWarpState state;
            if (!ClientWarpState.TryGetWarpState(grid.EntityId, out state) || state.State != WarpVisualState.Warping)
                return;


            var matrix = grid.WorldMatrix;
            var forwardDir = matrix.Forward;

            var thrusters = new List<IMyThrust>();
            var gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
            if (gridTerminal == null)
                return;

            gridTerminal.GetBlocksOfType(thrusters);

            var material = MyStringId.GetOrCompute("SciFiEngineThrustMiddle");
            float alpha = 0.6f + 0.3f * (float)System.Math.Sin(MyAPIGateway.Session.GameplayFrameCounter * 0.1f);
            Vector4 baseColor = new Vector4(0.643137f, 0.917647f, 1.0f, alpha); // Cyan

            foreach (var thruster in thrusters)
            {
                //MyAPIGateway.Utilities.ShowMessage("Thrusters:", $"{thruster.EntityId}");
                var dir = thruster.WorldMatrix.Backward;
                if (Vector3D.Dot(dir, forwardDir) < 0.9)
                    continue;

                var start = thruster.WorldMatrix.Translation - dir * 1.5;
                var end = start - dir * 120; // Length of trail
                //MyAPIGateway.Utilities.ShowMessage("Util:", $"Drawing from {start} to {end}");


                for (int i = 0; i < 2; i++)
                {
                    float t = (i + 1) / 2f;
                    float radius = 1.2f * (1f - t * 0.3f); // Thickness of trail
                    MySimpleObjectDraw.DrawLine(start, end, material, ref baseColor, 2f);
                }

            }
        }
    }
}
