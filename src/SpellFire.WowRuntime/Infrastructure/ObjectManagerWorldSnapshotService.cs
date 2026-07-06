using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.ObjectManager;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Infrastructure
{
    public sealed class ObjectManagerWorldSnapshotService : IWorldSnapshotService
    {
        private readonly int processId;
        private readonly IObjectManager objectManager;

        public ObjectManagerWorldSnapshotService(int processId, IObjectManager objectManager)
        {
            this.processId = processId;
            this.objectManager = objectManager;
        }

        public WowRuntimeResult<RuntimeWorldSnapshot> Capture(int objectLimit)
        {
            WowRuntimeResult<ObjectManagerSnapshot> objects = objectManager.GetObjects(objectLimit);
            if (!objects.Success)
            {
                return WowRuntimeResult<RuntimeWorldSnapshot>.Fail(objects.Status, objects.Detail);
            }

            return WowRuntimeResult<RuntimeWorldSnapshot>.Ok(new RuntimeWorldSnapshot(processId, objects.Value));
        }
    }
}
