using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Navigation
{
    public sealed class NavigationExecutionSnapshot
    {
        public NavigationExecutionSnapshot(
            NavigationExecutionStatus status,
            string detail,
            int pathPointCount,
            int visitedPointCount,
            Vector3 start,
            Vector3 end,
            Vector3 target,
            double distanceToTarget,
            bool stopAttempted)
        {
            Status = status;
            Detail = detail ?? string.Empty;
            PathPointCount = pathPointCount;
            VisitedPointCount = visitedPointCount;
            Start = start;
            End = end;
            Target = target;
            DistanceToTarget = distanceToTarget;
            StopAttempted = stopAttempted;
        }

        public bool Success
        {
            get { return Status == NavigationExecutionStatus.Success; }
        }

        public NavigationExecutionStatus Status { get; }

        public string Detail { get; }

        public int PathPointCount { get; }

        public int VisitedPointCount { get; }

        public Vector3 Start { get; }

        public Vector3 End { get; }

        public Vector3 Target { get; }

        public double DistanceToTarget { get; }

        public bool StopAttempted { get; }
    }
}
