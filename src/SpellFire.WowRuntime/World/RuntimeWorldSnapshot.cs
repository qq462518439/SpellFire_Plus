using System;
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
    }
}
