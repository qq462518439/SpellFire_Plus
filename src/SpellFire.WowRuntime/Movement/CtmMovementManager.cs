using System;
using System.Globalization;
using System.Threading;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class CtmMovementManager : IMovementManager
    {
        private readonly IMovementService movement;
        private readonly IWorldState world;

        public CtmMovementManager(IMovementService movement, IWorldState world)
        {
            this.movement = movement;
            this.world = world;
        }

        public MovementProgressSnapshot MoveToPoint(int pointIndex, Vector3 point, float arrivalDistance, int minimumTimeoutMs)
        {
            if (arrivalDistance <= 0 || minimumTimeoutMs <= 0)
            {
                return Snapshot(MovementProgressStatus.InvalidArgument, "arrivalDistance and minimumTimeoutMs must be positive.", pointIndex, point, default(Vector3), 0, 0, minimumTimeoutMs, false);
            }

            WowRuntimeResult<PlayerSnapshot> before = world.GetPlayer();
            if (!before.Success)
            {
                return Snapshot(MovementProgressStatus.PlayerUnavailable, before.Detail, pointIndex, point, default(Vector3), 0, 0, minimumTimeoutMs, false);
            }

            int timeoutMs = GetPointTimeoutMs(before.Value.Position, point, minimumTimeoutMs);
            WowRuntimeResult<MovementActionSnapshot> go = movement.Go(new[] { point });
            if (!go.Success)
            {
                movement.StopMove();
                return Snapshot(MovementProgressStatus.MovementFailed, go.Detail, pointIndex, point, before.Value.Position, Distance(before.Value.Position, point), Distance(before.Value.Position, point), timeoutMs, true);
            }

            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            DateTime lastProgressUtc = DateTime.UtcNow;
            double bestDistance = double.MaxValue;
            double lastDistance = double.MaxValue;
            Vector3 current = before.Value.Position;

            while (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(150);
                WowRuntimeResult<PlayerSnapshot> player = world.GetPlayer();
                if (!player.Success)
                {
                    movement.StopMove();
                    return Snapshot(MovementProgressStatus.PlayerUnavailable, player.Detail, pointIndex, point, current, lastDistance, bestDistance, timeoutMs, true);
                }

                current = player.Value.Position;
                lastDistance = Distance(current, point);
                if (lastDistance <= arrivalDistance)
                {
                    return Snapshot(MovementProgressStatus.Arrived, "Point reached.", pointIndex, point, current, lastDistance, Math.Min(bestDistance, lastDistance), timeoutMs, false);
                }

                if (lastDistance + 0.2 < bestDistance)
                {
                    bestDistance = lastDistance;
                    lastProgressUtc = DateTime.UtcNow;
                }

                if ((DateTime.UtcNow - lastProgressUtc).TotalMilliseconds > 2600 && bestDistance > arrivalDistance + 0.8)
                {
                    movement.StopMove();
                    return Snapshot(MovementProgressStatus.Stuck, "No measurable progress toward current path point.", pointIndex, point, current, lastDistance, bestDistance, timeoutMs, true);
                }
            }

            movement.StopMove();
            return Snapshot(MovementProgressStatus.Timeout, "Timed out waiting for current path point.", pointIndex, point, current, lastDistance, bestDistance, timeoutMs, true);
        }

        private static MovementProgressSnapshot Snapshot(MovementProgressStatus status, string detail, int pointIndex, Vector3 point, Vector3 current, double distance, double bestDistance, int timeoutMs, bool stopAttempted)
        {
            return new MovementProgressSnapshot(
                status,
                detail + " PointIndex=" + pointIndex + " Point=" + FormatVector(point) + " Current=" + FormatVector(current),
                pointIndex,
                point,
                current,
                distance,
                bestDistance,
                timeoutMs,
                stopAttempted);
        }

        private static int GetPointTimeoutMs(Vector3 from, Vector3 to, int minimumTimeoutMs)
        {
            double distance = Distance(from, to);
            int travelBudget = (int)((distance / 5.0) * 1000.0) + 3500;
            return Math.Max(minimumTimeoutMs, Math.Min(travelBudget, 22000));
        }

        private static double Distance(Vector3 a, Vector3 b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }

        private static string FormatVector(Vector3 value)
        {
            return "(" +
                   value.X.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.Y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.Z.ToString("0.###", CultureInfo.InvariantCulture) + ")";
        }
    }
}
