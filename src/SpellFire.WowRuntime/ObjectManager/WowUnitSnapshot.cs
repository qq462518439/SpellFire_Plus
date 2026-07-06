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
            : base(guid, entry, name, kind, position, isValid)
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
