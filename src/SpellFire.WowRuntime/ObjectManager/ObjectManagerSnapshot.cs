using System;
using System.Collections.Generic;

namespace SpellFire.WowRuntime.ObjectManager
{
    public sealed class ObjectManagerSnapshot
    {
        public ObjectManagerSnapshot(
            WowObjectSnapshot me,
            WowObjectSnapshot target,
            IReadOnlyList<WowObjectSnapshot> objects,
            int limit)
            : this(me, target, objects, limit, 0, 0, 0)
        {
        }

        public ObjectManagerSnapshot(
            WowObjectSnapshot me,
            WowObjectSnapshot target,
            IReadOnlyList<WowObjectSnapshot> objects,
            int limit,
            ulong localGuid,
            ulong targetGuid,
            int scanned)
            : this(me, target, objects, limit, localGuid, targetGuid, scanned, DateTime.UtcNow)
        {
        }

        public ObjectManagerSnapshot(
            WowObjectSnapshot me,
            WowObjectSnapshot target,
            IReadOnlyList<WowObjectSnapshot> objects,
            int limit,
            ulong localGuid,
            ulong targetGuid,
            int scanned,
            DateTime snapshotUtc)
        {
            Me = me;
            Target = target;
            Objects = objects ?? new List<WowObjectSnapshot>();
            Limit = limit;
            LocalGuid = localGuid;
            TargetGuid = targetGuid;
            Scanned = scanned;
            SnapshotUtc = snapshotUtc.Kind == DateTimeKind.Utc ? snapshotUtc : snapshotUtc.ToUniversalTime();
        }

        public WowObjectSnapshot Me { get; }

        public WowObjectSnapshot Target { get; }

        public IReadOnlyList<WowObjectSnapshot> Objects { get; }

        public int Limit { get; }

        public ulong LocalGuid { get; }

        public ulong TargetGuid { get; }

        public int Scanned { get; }

        public DateTime SnapshotUtc { get; }

        public long AgeMs
        {
            get
            {
                double age = (DateTime.UtcNow - SnapshotUtc).TotalMilliseconds;
                return age < 0 ? 0 : (long)age;
            }
        }

        public int Count
        {
            get { return Objects.Count; }
        }

        public int PlayerCount
        {
            get { return CountByKind(ObjectKind.Player); }
        }

        public int UnitCount
        {
            get { return CountByKind(ObjectKind.Unit); }
        }

        public int GameObjectCount
        {
            get { return CountByKind(ObjectKind.GameObject); }
        }

        public int ItemCount
        {
            get { return CountByKind(ObjectKind.Item); }
        }

        public int CorpseCount
        {
            get { return CountByKind(ObjectKind.Corpse); }
        }

        private int CountByKind(ObjectKind kind)
        {
            int count = 0;
            foreach (WowObjectSnapshot item in Objects)
            {
                if (item.Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
