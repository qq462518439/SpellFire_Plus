using System.Collections.Generic;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public interface IMovementService
    {
        WowRuntimeResult<MovementStateSnapshot> GetMovementState();

        WowRuntimeResult<ClickToMoveDiagnosticSnapshot> GetClickToMoveDiagnostic();

        WowRuntimeResult<MovementActionSnapshot> Jump();

        WowRuntimeResult<MovementActionSnapshot> StartMoveForward();

        WowRuntimeResult<MovementActionSnapshot> StartMoveBackward();

        WowRuntimeResult<MovementActionSnapshot> StartStrafeLeft();

        WowRuntimeResult<MovementActionSnapshot> StartStrafeRight();

        WowRuntimeResult<MovementActionSnapshot> StartTurnLeft();

        WowRuntimeResult<MovementActionSnapshot> StartTurnRight();

        WowRuntimeResult<MovementActionSnapshot> StopTurn();

        WowRuntimeResult<MovementActionSnapshot> FaceTo(Vector3 point);

        WowRuntimeResult<MovementActionSnapshot> FaceObject(ulong guid);

        WowRuntimeResult<MovementActionSnapshot> Go(IReadOnlyList<Vector3> points);

        WowRuntimeResult<MovementActionSnapshot> StopMove();

        WowRuntimeResult<MovementActionSnapshot> StopMoveTo();
    }
}
