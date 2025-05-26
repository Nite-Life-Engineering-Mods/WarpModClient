using Sandbox.ModAPI;
using VRage.Game.Components;

namespace WarpDriveClient
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class WarpControlInit : MySessionComponentBase
    {
        private bool _iscreated = false;
        public override void UpdateBeforeSimulation()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                WarpControls.Create();
                _iscreated = true;
            }
        }
    }

}