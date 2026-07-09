using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementProgressSnapshot
    {
        public MovementProgressSnapshot(
            MovementProgressStatus status,
            string detail,
            int pointIndex,
            Vector3 point,
            Vector3 current,
            double distance,
            double bestDistance,
            int sampleCount,
            int timeoutMs,
            MovementStateSnapshot movementState,
            bool stopAttempted)
        {
            Status = status;
            Detail = detail ?? string.Empty;
            PointIndex = pointIndex;
            Point = point;
            Current = current;
            Distance = distance;
            BestDistance = bestDistance;
            SampleCount = sampleCount;
            TimeoutMs = timeoutMs;
            MovementState = movementState;
            StopAttempted = stopAttempted;
        }

        public MovementProgressStatus Status { get; }

        public string Detail { get; }

        public int PointIndex { get; }

        public Vector3 Point { get; }

        public Vector3 Current { get; }

        public double Distance { get; }

        public double BestDistance { get; }

        public int SampleCount { get; }

        public int TimeoutMs { get; }

        public MovementStateSnapshot MovementState { get; }

        public bool StopAttempted { get; }

        public static MovementProgressSnapshot Create(
            MovementProgressStatus status,
            string detail,
            int pointIndex,
            Vector3 point,
            Vector3 current,
            double distance,
            double bestDistance,
            int sampleCount,
            int timeoutMs,
            MovementStateSnapshot movementState,
            bool stopAttempted)
        {
            return new MovementProgressSnapshot(
                status,
                detail + " PointIndex=" + pointIndex + " Point=" + MovementText.FormatVector(point) + " Current=" + MovementText.FormatVector(current),
                pointIndex,
                point,
                current,
                distance,
                bestDistance,
                sampleCount,
                timeoutMs,
                movementState,
                stopAttempted);
        }
    }
}
