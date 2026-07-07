using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public interface IMovementManager
    {
        MovementProgressSnapshot MoveToPoint(int pointIndex, Vector3 point, float arrivalDistance, int minimumTimeoutMs);
    }
}
