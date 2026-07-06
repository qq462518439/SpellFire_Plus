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
        {
            Me = me;
            Target = target;
            Objects = objects ?? new List<WowObjectSnapshot>();
            Limit = limit;
        }

        public WowObjectSnapshot Me { get; }

        public WowObjectSnapshot Target { get; }

        public IReadOnlyList<WowObjectSnapshot> Objects { get; }

        public int Limit { get; }

        public int Count
        {
            get { return Objects.Count; }
        }
    }
}
