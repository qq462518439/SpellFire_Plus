using SpellFire.WowRuntime.Core;

namespace SpellFire.WowRuntime.World
{
    public interface IWorldState
    {
        WowRuntimeResult<PlayerSnapshot> GetPlayer();
    }
}
