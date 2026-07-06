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
        {
            Me = me;
            Target = target;
            Objects = objects ?? new List<WowObjectSnapshot>();
            Limit = limit;
            LocalGuid = localGuid;
            TargetGuid = targetGuid;
            Scanned = scanned;
        }

        public WowObjectSnapshot Me { get; }

        public WowObjectSnapshot Target { get; }

        public IReadOnlyList<WowObjectSnapshot> Objects { get; }

        public int Limit { get; }

        public ulong LocalGuid { get; }

        public ulong TargetGuid { get; }

        public int Scanned { get; }

        public int Count
        {
            get { return Objects.Count; }
        }
    }
}
