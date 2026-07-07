using System;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Movement;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Navigation
{
    public sealed class NavigationExecutionService
    {
        private readonly INavigationService navigation;
        private readonly IWorldState world;
        private readonly IMovementManager movementManager;

        public NavigationExecutionService(INavigationService navigation, IWorldState world, IMovementService movement)
            : this(navigation, world, new CtmMovementManager(movement, world))
        {
        }

        public NavigationExecutionService(INavigationService navigation, IWorldState world, IMovementManager movementManager)
        {
            this.navigation = navigation;
            this.world = world;
            this.movementManager = movementManager;
        }

        public NavigationExecutionSnapshot ExecuteTo(Vector3 target, float arrivalDistance, int perPointTimeoutMs, int maxPoints)
        {
            if (arrivalDistance <= 0 || perPointTimeoutMs <= 0 || maxPoints <= 0)
            {
                return new NavigationExecutionSnapshot(
                    NavigationExecutionStatus.InvalidArgument,
                    "arrivalDistance, perPointTimeoutMs, and maxPoints must be positive.",
                    0,
                    0,
                    default(Vector3),
                    default(Vector3),
                    target,
                    0,
                    false);
            }

            WowRuntimeResult<PlayerSnapshot> player = world.GetPlayer();
            if (!player.Success)
            {
                return new NavigationExecutionSnapshot(
                    NavigationExecutionStatus.PlayerUnavailable,
                    player.Detail,
                    0,
                    0,
                    default(Vector3),
                    default(Vector3),
                    target,
                    0,
                    false);
            }

            Vector3 start = player.Value.Position;
            PathResult path = navigation.FindPath(new PathQuery(player.Value.MapId, start, target));
            if (!path.Success)
            {
                return new NavigationExecutionSnapshot(
                    NavigationExecutionStatus.PathUnavailable,
                    path.Status + ": " + path.Detail,
                    path.Points.Count,
                    0,
                    start,
                    start,
                    target,
                    Distance(start, target),
                    false);
            }

            int limit = Math.Min(path.Points.Count, maxPoints);
            int visited = 0;
            Vector3 lastPosition = start;

            for (int i = 0; i < limit; i++)
            {
                Vector3 point = path.Points[i];
                if (Distance(lastPosition, point) <= arrivalDistance)
                {
                    visited++;
                    continue;
                }

                MovementProgressSnapshot progress = movementManager.MoveToPoint(i, point, arrivalDistance, perPointTimeoutMs);
                if (progress.Status != MovementProgressStatus.Arrived)
                {
                    return Finish(MapProgressStatus(progress.Status), FormatProgressDetail(progress), path.Points.Count, visited, start, progress.Current, target, progress.StopAttempted);
                }

                visited++;
                lastPosition = progress.Current;
            }

            WowRuntimeResult<PlayerSnapshot> final = world.GetPlayer();
            if (!final.Success)
            {
                return Finish(NavigationExecutionStatus.PlayerUnavailable, final.Detail, path.Points.Count, visited, start, lastPosition, target, false);
            }

            lastPosition = final.Value.Position;
            NavigationExecutionStatus status = Distance(lastPosition, target) <= Math.Max(arrivalDistance, 2.5f)
                ? NavigationExecutionStatus.Success
                : NavigationExecutionStatus.Timeout;
            string detail = status == NavigationExecutionStatus.Success
                ? "Navigation execution reached target tolerance."
                : "Navigation execution consumed allowed points but target tolerance was not reached.";
            return Finish(status, detail, path.Points.Count, visited, start, lastPosition, target, false);
        }

        private static NavigationExecutionStatus MapProgressStatus(MovementProgressStatus status)
        {
            switch (status)
            {
                case MovementProgressStatus.Timeout:
                    return NavigationExecutionStatus.Timeout;
                case MovementProgressStatus.Stuck:
                    return NavigationExecutionStatus.Stuck;
                case MovementProgressStatus.MovementFailed:
                    return NavigationExecutionStatus.MovementFailed;
                case MovementProgressStatus.PlayerUnavailable:
                    return NavigationExecutionStatus.PlayerUnavailable;
                case MovementProgressStatus.InvalidArgument:
                    return NavigationExecutionStatus.InvalidArgument;
                default:
                    return NavigationExecutionStatus.MovementFailed;
            }
        }

        private static string FormatProgressDetail(MovementProgressSnapshot progress)
        {
            if (progress == null)
            {
                return "Movement progress unavailable.";
            }

            return progress.Detail +
                   " MovementStatus=" + progress.Status +
                   " Distance=" + progress.Distance.ToString("0.###") +
                   " BestDistance=" + progress.BestDistance.ToString("0.###") +
                   " TimeoutMs=" + progress.TimeoutMs;
        }

        private static NavigationExecutionSnapshot Finish(
            NavigationExecutionStatus status,
            string detail,
            int pathPointCount,
            int visitedPointCount,
            Vector3 start,
            Vector3 end,
            Vector3 target,
            bool stopAttempted)
        {
            return new NavigationExecutionSnapshot(
                status,
                detail,
                pathPointCount,
                visitedPointCount,
                start,
                end,
                target,
                Distance(end, target),
                stopAttempted);
        }

        private static double Distance(Vector3 a, Vector3 b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }
    }
}
