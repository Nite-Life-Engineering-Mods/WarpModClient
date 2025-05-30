using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace WarpDriveMod_RC3a
{
    public class MyStatPlayerWarpState : IMyHudStat
    {
        public MyStatPlayerWarpState()
        {
            Id = MyStringHash.GetOrCompute("player_warpstate");
        }

        private float m_currentValue;
        private string m_valueStringCache;

        public MyStringHash Id { get; protected set; }

        public float CurrentValue
        {
            get { return m_currentValue; }
            protected set
            {
                if (m_currentValue == value)
                {
                    return;
                }
                m_currentValue = value;
                m_valueStringCache = null;
            }
        }

        public virtual float MaxValue => 1f;
        public virtual float MinValue => 0.0f;

        public string GetValueString()
        {
            if (m_valueStringCache == null)
            {
                m_valueStringCache = ToString();
            }
            return m_valueStringCache;
        }

        public void Update()
        {
            MyEntityStatComponent statComp = MyAPIGateway.Session.Player?.Character?.Components.Get<MyEntityStatComponent>();

            if (statComp == null)
            {
                return;
            }
            MyEntityStat warpstate;
            statComp.TryGetStat(MyStringHash.GetOrCompute("WarpState"), out warpstate);
            CurrentValue = warpstate.Value / warpstate.MaxValue;
        }

        public override string ToString() => string.Format("{0:0}", (float)(CurrentValue * 100.0));
    }
}