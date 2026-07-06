using SpellFire.WowRuntime.Core;

namespace SpellFire.WowRuntime.World
{
    public interface IWorldSnapshotService
    {
        WowRuntimeResult<RuntimeWorldSnapshot> Capture(int objectLimit);
    }
}
