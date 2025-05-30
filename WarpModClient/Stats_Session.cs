using System.Text;
using System;
using System.Linq;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Components;
using Sandbox.ModAPI;
using Sandbox.Definitions;
using SpaceEngineers.Game.ModAPI;
using VRageMath;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.Entity;
using VRage.Game.Components;
using VRage.Utils;
using Sandbox.Game.World;

using VRage.Input;
using WarpDriveClient;

namespace WarpDriveMod_RC3a
{
    public struct CharacterStats
    {
        public IMyCharacter character;
		public MyEntityStat warpspeed;
        public MyEntityStat warpstate;
		public MyEntityStat shieldvalue;
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Session : MySessionComponentBase
    {
        private static int hudUpdateClock = 0;
        private static List<IMyPlayer> playersHudStats = new List<IMyPlayer>();

        private bool isServer;		

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            // MyAPIGateway.Session.SessionSettings.AutoHealing = false;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
        }

        public override void UpdateAfterSimulation()
        {
            hudUpdateClock++;
            if (hudUpdateClock >= 120)
                hudUpdateClock = 0;

            if (hudUpdateClock == 0)
            {
                UpdateHud();
            }
        }

		public void UpdateHud()
		{
			playersHudStats.Clear();
			MyAPIGateway.Players.GetPlayers(playersHudStats);

			foreach (IMyPlayer player in playersHudStats)
			{
				if (player == null || player.IsBot)
				{
					continue;
				}

                var character = player.Character;
                if (character == null)
                    continue;


                MyEntityStatComponent statComp = player.Character?.Components?.Get<MyEntityStatComponent>();
				if (statComp == null)
				{
					continue;
				}
				MyEntityStat warpstate;
				statComp.TryGetStat(MyStringHash.GetOrCompute("WarpState"), out warpstate);

				MyEntityStat warpspeed;
				statComp.TryGetStat(MyStringHash.GetOrCompute("WarpSpeed"), out warpspeed);

                var controller = player.Controller?.ControlledEntity as IMyShipController;
                if (controller != null && controller.CubeGrid != null)
                {
                    long gridId = controller.CubeGrid.EntityId;
                    ClientWarpState state;
                    if (ClientWarpState.TryGetWarpState(gridId, out state))
                    {
                        if (state.State == WarpVisualState.Warping)
                        {
                            warpstate?.Increase(1f, null);
                            warpspeed.Value = (float)(state.speed / 1000f); // km/s
                        }
                        else
                        {
                            warpstate?.Decrease(1f, null);
                            warpspeed.Value = 0f;
                        }
                    }
                    else
                    {
                        warpspeed.Value = 0f;
                        warpstate?.Decrease(1f, null);
                    }
                }





                //Set the warp speed value
                //Set the shield value			

            }// end for each player
        }// end UpdateHudStats
	}// end public class session
}// end namespace
