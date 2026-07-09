using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public interface IMovementManager
    {
        MovementProgressSnapshot MoveToPoint(int pointIndex, Vector3 point, float arrivalDistance, int minimumTimeoutMs);

        MovementPathExecutionResult MovePath(
            System.Collections.Generic.IReadOnlyList<Vector3> points,
            Vector3 start,
            float arrivalDistance,
            int minimumTimeoutMs,
            int maxPoints);
    }
}
