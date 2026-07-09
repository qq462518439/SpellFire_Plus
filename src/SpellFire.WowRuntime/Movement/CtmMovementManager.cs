using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class CtmMovementManager : IMovementManager
    {
        private readonly MovementPathExecutor executor;

        public CtmMovementManager(IMovementService movement, IWorldState world)
            : this(new MovementPathExecutor(
                movement,
                new MovementProgressTracker(world),
                new MovementStuckDetector(),
                new MovementStuckResolver(movement),
                new MovementStopPolicy(movement),
                new MovementPathPointFilter()))
        {
        }

        public CtmMovementManager(MovementPathExecutor executor)
        {
            this.executor = executor;
        }

        public MovementProgressSnapshot MoveToPoint(int pointIndex, Vector3 point, float arrivalDistance, int minimumTimeoutMs)
        {
            return executor.MoveToPoint(pointIndex, point, new MovementExecutionOptions(arrivalDistance, minimumTimeoutMs));
        }

        public MovementPathExecutionResult MovePath(
            System.Collections.Generic.IReadOnlyList<Vector3> points,
            Vector3 start,
            float arrivalDistance,
            int minimumTimeoutMs,
            int maxPoints)
        {
            return executor.MovePath(points, start, new MovementExecutionOptions(arrivalDistance, minimumTimeoutMs), maxPoints);
        }
    }
}
