using System;
using System.Collections.Generic;
using SpellFire.WowRuntime.ObjectManager;

namespace SpellFire.WowRuntime.World
{
    public sealed class RuntimeWorldSnapshot
    {
        public RuntimeWorldSnapshot(int processId, ObjectManagerSnapshot objects)
            : this(processId, objects, null, null)
        {
        }

        public RuntimeWorldSnapshot(int processId, ObjectManagerSnapshot objects, PlayerSnapshot player)
            : this(processId, objects, player, null)
        {
        }

        public RuntimeWorldSnapshot(int processId, ObjectManagerSnapshot objects, PlayerSnapshot player, WorldPhaseSnapshot phase)
        {
            ProcessId = processId;
            Objects = objects;
            Player = player;
            Phase = phase;
            SnapshotUtc = objects == null ? DateTime.UtcNow : objects.SnapshotUtc;
        }

        public int ProcessId { get; }

        public DateTime SnapshotUtc { get; }

        public long AgeMs
        {
            get
            {
                double age = (DateTime.UtcNow - SnapshotUtc).TotalMilliseconds;
                return age < 0 ? 0 : (long)age;
            }
        }

        public ObjectManagerSnapshot Objects { get; }

        public PlayerSnapshot Player { get; }

        public WorldPhaseSnapshot Phase { get; }

        public WowObjectSnapshot Me
        {
            get { return Objects == null ? null : Objects.Me; }
        }

        public WowObjectSnapshot Target
        {
            get { return Objects == null ? null : Objects.Target; }
        }

        public int ObjectCount
        {
            get { return Objects == null ? 0 : Objects.Count; }
        }

        public bool InWorld
        {
            get { return Phase != null && Phase.InGame && !Phase.LoadingOrConnecting; }
        }

        public bool HasPlayer
        {
            get { return Player != null || Me != null; }
        }

        public bool HasTarget
        {
            get { return Target != null && Target.IsValid; }
        }

        public int PlayerCount
        {
            get { return Objects == null ? 0 : Objects.PlayerCount; }
        }

        public int UnitCount
        {
            get { return Objects == null ? 0 : Objects.UnitCount; }
        }

        public int GameObjectCount
        {
            get { return Objects == null ? 0 : Objects.GameObjectCount; }
        }

        public int ItemCount
        {
            get { return Objects == null ? 0 : Objects.ItemCount; }
        }

        public int CorpseCount
        {
            get { return Objects == null ? 0 : Objects.CorpseCount; }
        }

        public WowObjectSnapshot NearestUnit
        {
            get { return FindNearest(ObjectKind.Unit); }
        }

        public WowObjectSnapshot NearestGameObject
        {
            get { return FindNearest(ObjectKind.GameObject); }
        }

        private WowObjectSnapshot FindNearest(ObjectKind kind)
        {
            if (Objects == null || Objects.Objects == null)
            {
                return null;
            }

            IReadOnlyList<WowObjectSnapshot> items = Objects.Objects;
            WowObjectSnapshot nearest = null;
            for (int i = 0; i < items.Count; i++)
            {
                WowObjectSnapshot item = items[i];
                if (item == null || item.Kind != kind || !item.IsValid)
                {
                    continue;
                }

                if (nearest == null || item.DistanceFromMe < nearest.DistanceFromMe)
                {
                    nearest = item;
                }
            }

            return nearest;
        }
    }
}
