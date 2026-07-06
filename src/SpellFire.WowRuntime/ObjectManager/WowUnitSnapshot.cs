using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.ObjectManager
{
    public class WowUnitSnapshot : WowObjectSnapshot
    {
        public WowUnitSnapshot(
            ulong guid,
            int entry,
            string name,
            ObjectKind kind,
            Vector3 position,
            bool isValid,
            bool isAlive,
            bool inCombat,
            ulong targetGuid)
            : this(guid, entry, name, kind, position, isValid, isAlive, inCombat, targetGuid, 0, 0)
        {
        }

        public WowUnitSnapshot(
            ulong guid,
            int entry,
            string name,
            ObjectKind kind,
            Vector3 position,
            bool isValid,
            bool isAlive,
            bool inCombat,
            ulong targetGuid,
            uint baseAddress,
            float distanceFromMe)
            : base(guid, entry, name, kind, position, isValid, baseAddress, distanceFromMe)
        {
            IsAlive = isAlive;
            InCombat = inCombat;
            TargetGuid = targetGuid;
        }

        public bool IsAlive { get; }

        public bool InCombat { get; }

        public ulong TargetGuid { get; }
    }
}
