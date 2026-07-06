using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Movement;

namespace SpellFire.WowRuntime.World
{
    public interface IWorldState
    {
        WowRuntimeResult<PlayerSnapshot> GetPlayer();

        WowRuntimeResult<WorldPhaseSnapshot> GetPhase();

        WowRuntimeResult<MovementStateSnapshot> GetMovementState();
    }
}
