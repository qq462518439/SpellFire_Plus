using System;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementProgressTracker
    {
        private readonly IWorldState world;

        public MovementProgressTracker(IWorldState world)
        {
            this.world = world;
        }

        public WowRuntimeResult<PlayerSnapshot> ReadPlayer()
        {
            return world.GetPlayer();
        }

        public WowRuntimeResult<MovementProgressSample> Sample(
            Vector3 point,
            MovementProgressSample previous,
            int sampleCount)
        {
            WowRuntimeResult<PlayerSnapshot> player = world.GetPlayer();
            if (!player.Success)
            {
                return WowRuntimeResult<MovementProgressSample>.Fail(player.Status, player.Detail);
            }

            WowRuntimeResult<MovementStateSnapshot> movement = world.GetMovementState();
            MovementStateSnapshot movementState = movement.Success ? movement.Value : null;
            double distance = Distance(player.Value.Position, point);
            double bestDistance = previous == null ? distance : previous.BestDistance;
            DateTime lastProgressUtc = previous == null ? DateTime.UtcNow : previous.LastProgressUtc;
            if (distance + 0.2 < bestDistance)
            {
                bestDistance = distance;
                lastProgressUtc = DateTime.UtcNow;
            }

            return WowRuntimeResult<MovementProgressSample>.Ok(new MovementProgressSample(
                player.Value.Position,
                distance,
                bestDistance,
                sampleCount,
                lastProgressUtc,
                movementState));
        }

        public static double Distance(Vector3 a, Vector3 b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }
    }
}
