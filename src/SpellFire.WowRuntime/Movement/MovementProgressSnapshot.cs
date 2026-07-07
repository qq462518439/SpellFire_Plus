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
            int timeoutMs,
            bool stopAttempted)
        {
            Status = status;
            Detail = detail ?? string.Empty;
            PointIndex = pointIndex;
            Point = point;
            Current = current;
            Distance = distance;
            BestDistance = bestDistance;
            TimeoutMs = timeoutMs;
            StopAttempted = stopAttempted;
        }

        public MovementProgressStatus Status { get; }

        public string Detail { get; }

        public int PointIndex { get; }

        public Vector3 Point { get; }

        public Vector3 Current { get; }

        public double Distance { get; }

        public double BestDistance { get; }

        public int TimeoutMs { get; }

        public bool StopAttempted { get; }
    }
}
