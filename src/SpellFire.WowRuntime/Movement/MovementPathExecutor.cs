using System;
using System.Threading;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementPathExecutor
    {
        private readonly IMovementService movement;
        private readonly MovementProgressTracker tracker;
        private readonly MovementStuckDetector stuckDetector;
        private readonly MovementStuckResolver stuckResolver;
        private readonly MovementStopPolicy stopPolicy;
        private readonly MovementPathPointFilter pointFilter;

        public MovementPathExecutor(
            IMovementService movement,
            MovementProgressTracker tracker,
            MovementStuckDetector stuckDetector,
            MovementStuckResolver stuckResolver,
            MovementStopPolicy stopPolicy,
            MovementPathPointFilter pointFilter)
        {
            this.movement = movement;
            this.tracker = tracker;
            this.stuckDetector = stuckDetector;
            this.stuckResolver = stuckResolver;
            this.stopPolicy = stopPolicy;
            this.pointFilter = pointFilter;
        }

        public MovementPathExecutionResult MovePath(
            System.Collections.Generic.IReadOnlyList<Vector3> points,
            Vector3 start,
            MovementExecutionOptions options,
            int maxPoints)
        {
            if (points == null || options == null || maxPoints <= 0)
            {
                return new MovementPathExecutionResult(
                    MovementProgressStatus.InvalidArgument,
                    "points, options, and maxPoints must be valid.",
                    points == null ? 0 : points.Count,
                    0,
                    start,
                    null,
                    false);
            }

            System.Collections.Generic.IReadOnlyList<Vector3> prepared = pointFilter.Prepare(points, start, options.ArrivalDistance, maxPoints);
            Vector3 lastPosition = start;
            int visited = 0;
            MovementProgressSnapshot lastProgress = null;

            for (int i = 0; i < prepared.Count; i++)
            {
                MovementProgressSnapshot progress = MoveToPoint(i, prepared[i], options);
                lastProgress = progress;
                lastPosition = progress.Current;

                if (progress.Status != MovementProgressStatus.Arrived)
                {
                    return new MovementPathExecutionResult(
                        progress.Status,
                        progress.Detail,
                        points.Count,
                        visited,
                        lastPosition,
                        progress,
                        progress.StopAttempted);
                }

                visited++;
            }

            return new MovementPathExecutionResult(
                MovementProgressStatus.Arrived,
                "Path points reached.",
                points.Count,
                visited,
                lastPosition,
                lastProgress,
                false);
        }

        public MovementProgressSnapshot MoveToPoint(int pointIndex, Vector3 point, MovementExecutionOptions options)
        {
            if (options == null || options.ArrivalDistance <= 0 || options.MinimumTimeoutMs <= 0)
            {
                return MovementProgressSnapshot.Create(
                    MovementProgressStatus.InvalidArgument,
                    "arrivalDistance and minimumTimeoutMs must be positive.",
                    pointIndex,
                    point,
                    default(Vector3),
                    0,
                    0,
                    0,
                    0,
                    null,
                    false);
            }

            WowRuntimeResult<PlayerSnapshot> before = tracker.ReadPlayer();
            if (!before.Success)
            {
                return MovementProgressSnapshot.Create(
                    MovementProgressStatus.PlayerUnavailable,
                    before.Detail,
                    pointIndex,
                    point,
                    default(Vector3),
                    0,
                    0,
                    0,
                    options.MinimumTimeoutMs,
                    null,
                    false);
            }

            int timeoutMs = GetPointTimeoutMs(before.Value.Position, point, options.MinimumTimeoutMs);
            WowRuntimeResult<MovementActionSnapshot> go = movement.Go(new[] { point });
            if (!go.Success)
            {
                bool stopped = stopPolicy.Stop();
                double distance = MovementProgressTracker.Distance(before.Value.Position, point);
                return MovementProgressSnapshot.Create(
                    MovementProgressStatus.MovementFailed,
                    go.Detail,
                    pointIndex,
                    point,
                    before.Value.Position,
                    distance,
                    distance,
                    0,
                    timeoutMs,
                    null,
                    stopped);
            }

            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            MovementProgressSample sample = null;
            int sampleCount = 0;
            bool stuckResolveAttempted = false;
            while (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(150);
                sampleCount++;
                WowRuntimeResult<MovementProgressSample> sampled = tracker.Sample(point, sample, sampleCount);
                if (!sampled.Success)
                {
                    bool stopped = stopPolicy.Stop();
                    return MovementProgressSnapshot.Create(
                        MovementProgressStatus.PlayerUnavailable,
                        sampled.Detail,
                        pointIndex,
                        point,
                        sample == null ? before.Value.Position : sample.Position,
                        sample == null ? 0 : sample.Distance,
                        sample == null ? 0 : sample.BestDistance,
                        sampleCount,
                        timeoutMs,
                        sample == null ? null : sample.MovementState,
                        stopped);
                }

                sample = sampled.Value;
                if (sample.Distance <= options.ArrivalDistance)
                {
                    return MovementProgressSnapshot.Create(
                        MovementProgressStatus.Arrived,
                        "Point reached.",
                        pointIndex,
                        point,
                        sample.Position,
                        sample.Distance,
                        Math.Min(sample.BestDistance, sample.Distance),
                        sample.SampleCount,
                        timeoutMs,
                        sample.MovementState,
                        false);
                }

                if (stuckDetector.IsStuck(sample, options, DateTime.UtcNow))
                {
                    bool resolved = !stuckResolveAttempted && stuckResolver.TryResolve();
                    stuckResolveAttempted = true;
                    if (resolved)
                    {
                        WowRuntimeResult<MovementActionSnapshot> retry = movement.Go(new[] { point });
                        if (retry.Success)
                        {
                            sample = null;
                            sampleCount = 0;
                            deadline = DateTime.UtcNow.AddMilliseconds(Math.Min(timeoutMs, 7000));
                            continue;
                        }
                    }

                    bool stopped = stopPolicy.Stop();
                    return MovementProgressSnapshot.Create(
                        MovementProgressStatus.Stuck,
                        "No measurable progress toward current path point. StuckResolveAttempted=True StuckResolved=" + resolved,
                        pointIndex,
                        point,
                        sample.Position,
                        sample.Distance,
                        sample.BestDistance,
                        sample.SampleCount,
                        timeoutMs,
                        sample.MovementState,
                        stopped);
                }
            }

            bool stopAttempted = stopPolicy.Stop();
            Vector3 current = sample == null ? before.Value.Position : sample.Position;
            return MovementProgressSnapshot.Create(
                MovementProgressStatus.Timeout,
                "Timed out waiting for current path point.",
                pointIndex,
                point,
                current,
                sample == null ? MovementProgressTracker.Distance(current, point) : sample.Distance,
                sample == null ? MovementProgressTracker.Distance(current, point) : sample.BestDistance,
                sample == null ? 0 : sample.SampleCount,
                timeoutMs,
                sample == null ? null : sample.MovementState,
                stopAttempted);
        }

        private static int GetPointTimeoutMs(Vector3 from, Vector3 to, int minimumTimeoutMs)
        {
            double distance = MovementProgressTracker.Distance(from, to);
            int travelBudget = (int)((distance / 5.0) * 1000.0) + 3500;
            return Math.Max(minimumTimeoutMs, Math.Min(travelBudget, 22000));
        }
    }
}
