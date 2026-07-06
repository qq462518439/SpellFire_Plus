using System.Collections.Generic;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public interface IMovementService
    {
        WowRuntimeResult<MovementStateSnapshot> GetMovementState();

        WowRuntimeResult<MovementActionSnapshot> Jump();

        WowRuntimeResult<MovementActionSnapshot> Go(IReadOnlyList<Vector3> points);

        WowRuntimeResult<MovementActionSnapshot> StopMove();

        WowRuntimeResult<MovementActionSnapshot> StopMoveTo();
    }
}
