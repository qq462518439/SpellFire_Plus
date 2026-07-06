using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.ObjectManager
{
    public interface IObjectManager
    {
        WowRuntimeResult<WowObjectSnapshot> GetMe();

        WowRuntimeResult<WowObjectSnapshot> GetTarget();

        WowRuntimeResult<ObjectManagerSnapshot> GetObjects(int limit);

        WowRuntimeResult<WowObjectSnapshot> GetObjectByGuid(ulong guid);

        WowRuntimeResult<ObjectManagerSnapshot> GetObjectsByEntry(int entry, int limit);

        WowRuntimeResult<ObjectManagerSnapshot> GetNearbyObjects(Vector3 center, float radius, int limit);
    }
}
