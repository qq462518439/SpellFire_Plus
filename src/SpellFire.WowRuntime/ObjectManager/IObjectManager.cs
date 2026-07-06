using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.ObjectManager
{
    public interface IObjectManager
    {
        WowRuntimeResult<WowObjectSnapshot> GetMe();

        WowRuntimeResult<WowObjectSnapshot> GetTarget();

        WowRuntimeResult<ObjectManagerSnapshot> GetObjects(int limit);

        WowRuntimeResult<ObjectManagerSnapshot> GetObjects(int limit, int scanLimit);

        WowRuntimeResult<WowObjectSnapshot> GetObjectByGuid(ulong guid);

        WowRuntimeResult<ObjectManagerSnapshot> GetObjectsByEntry(int entry, int limit);

        WowRuntimeResult<ObjectManagerSnapshot> GetObjectsByEntry(int entry, int limit, int scanLimit);

        WowRuntimeResult<ObjectManagerSnapshot> GetObjectsByKind(ObjectKind kind, int limit);

        WowRuntimeResult<ObjectManagerSnapshot> GetObjectsByKind(ObjectKind kind, int limit, int scanLimit);

        WowRuntimeResult<ObjectManagerSnapshot> GetNearbyObjects(Vector3 center, float radius, int limit);

        WowRuntimeResult<ObjectManagerSnapshot> GetNearbyObjects(Vector3 center, float radius, int limit, int scanLimit);

        WowRuntimeResult<ObjectManagerSnapshot> GetNearbyObjectsByKind(ObjectKind kind, Vector3 center, float radius, int limit);

        WowRuntimeResult<ObjectManagerSnapshot> GetNearbyObjectsByKind(ObjectKind kind, Vector3 center, float radius, int limit, int scanLimit);

        WowRuntimeResult<WowObjectSnapshot> GetNearestObject(Vector3 center, float radius);

        WowRuntimeResult<WowObjectSnapshot> GetNearestObjectByKind(ObjectKind kind, Vector3 center, float radius);
    }
}
