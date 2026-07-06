using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.ObjectManager
{
    public sealed class WowGameObjectSnapshot : WowObjectSnapshot
    {
        public WowGameObjectSnapshot(ulong guid, int entry, string name, Vector3 position, bool isValid)
            : base(guid, entry, name, ObjectKind.GameObject, position, isValid)
        {
        }
    }
}
