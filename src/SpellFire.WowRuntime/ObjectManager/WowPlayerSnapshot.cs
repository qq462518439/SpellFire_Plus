using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.ObjectManager
{
    public sealed class WowPlayerSnapshot : WowUnitSnapshot
    {
        public WowPlayerSnapshot(
            ulong guid,
            int entry,
            string name,
            Vector3 position,
            bool isValid,
            bool isAlive,
            bool inCombat,
            ulong targetGuid)
            : base(guid, entry, name, ObjectKind.Player, position, isValid, isAlive, inCombat, targetGuid)
        {
        }
    }
}
