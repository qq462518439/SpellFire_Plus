using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.ObjectManager;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Infrastructure
{
    public sealed class ObjectManagerWorldSnapshotService : IWorldSnapshotService
    {
        private readonly int processId;
        private readonly IObjectManager objectManager;
        private readonly IWorldState world;

        public ObjectManagerWorldSnapshotService(int processId, IObjectManager objectManager, IWorldState world)
        {
            this.processId = processId;
            this.objectManager = objectManager;
            this.world = world;
        }

        public WowRuntimeResult<RuntimeWorldSnapshot> Capture(int objectLimit)
        {
            WowRuntimeResult<ObjectManagerSnapshot> objects = objectManager.GetObjects(objectLimit);
            if (!objects.Success)
            {
                return WowRuntimeResult<RuntimeWorldSnapshot>.Fail(objects.Status, objects.Detail);
            }

            WowRuntimeResult<PlayerSnapshot> player = world.GetPlayer();
            WowRuntimeResult<WorldPhaseSnapshot> phase = world.GetPhase();
            return WowRuntimeResult<RuntimeWorldSnapshot>.Ok(new RuntimeWorldSnapshot(
                processId,
                objects.Value,
                player.Success ? player.Value : null,
                phase.Success ? phase.Value : null));
        }
    }
}
