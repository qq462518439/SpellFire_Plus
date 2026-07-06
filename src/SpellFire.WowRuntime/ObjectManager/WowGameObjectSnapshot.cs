using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.ObjectManager
{
    public sealed class WowGameObjectSnapshot : WowObjectSnapshot
    {
        public WowGameObjectSnapshot(ulong guid, int entry, string name, Vector3 position, bool isValid)
            : this(guid, entry, name, position, isValid, 0, 0)
        {
        }

        public WowGameObjectSnapshot(ulong guid, int entry, string name, Vector3 position, bool isValid, uint baseAddress, float distanceFromMe)
            : base(guid, entry, name, ObjectKind.GameObject, position, isValid, baseAddress, distanceFromMe)
        {
        }
    }
}
