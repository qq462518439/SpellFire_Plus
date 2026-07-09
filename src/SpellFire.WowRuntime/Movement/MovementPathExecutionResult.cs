using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementPathExecutionResult
    {
        public MovementPathExecutionResult(
            MovementProgressStatus status,
            string detail,
            int pathPointCount,
            int visitedPointCount,
            Vector3 end,
            MovementProgressSnapshot lastProgress,
            bool stopAttempted)
        {
            Status = status;
            Detail = detail ?? string.Empty;
            PathPointCount = pathPointCount;
            VisitedPointCount = visitedPointCount;
            End = end;
            LastProgress = lastProgress;
            StopAttempted = stopAttempted;
        }

        public MovementProgressStatus Status { get; }

        public string Detail { get; }

        public int PathPointCount { get; }

        public int VisitedPointCount { get; }

        public Vector3 End { get; }

        public MovementProgressSnapshot LastProgress { get; }

        public bool StopAttempted { get; }
    }
}
