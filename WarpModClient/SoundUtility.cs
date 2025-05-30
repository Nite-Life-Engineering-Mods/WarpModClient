using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace WarpDriveClient
{

    public static class SoundUtility
    {
        private static readonly Dictionary<long, MyEntity3DSoundEmitter> Emitters = new Dictionary<long, MyEntity3DSoundEmitter>();

        public static void Play(IMyEntity entity, MySoundPair sound)
        {
            if (entity == null) return;
            MyEntity3DSoundEmitter emitter;
            if (!Emitters.TryGetValue(entity.EntityId, out emitter))
            {
                emitter = new MyEntity3DSoundEmitter((MyEntity)entity);
                Emitters[entity.EntityId] = emitter;
            }

            emitter.Entity = (MyEntity)entity;
            emitter.StopSound(true); // Always restart to avoid overlapping
            emitter.PlaySound(sound, alwaysHearOnRealistic: true);
        }

        public static void Stop(IMyEntity entity)
        {
            if (entity == null) return;
            MyEntity3DSoundEmitter emitter;
            if (Emitters.TryGetValue(entity.EntityId, out emitter))
            {
                emitter.StopSound(true);
            }
        }

        public static bool IsPlaying(IMyEntity entity)
        {
            MyEntity3DSoundEmitter emitter;
            if (Emitters.TryGetValue(entity.EntityId, out emitter) && emitter.IsPlaying)
            {
                return true;
            }
            return false;
        }

        public static void StopAll()
        {
            foreach (var emitter in Emitters.Values)
            {
                emitter?.StopSound(true);
            }
            Emitters.Clear();
        }
    }

}
