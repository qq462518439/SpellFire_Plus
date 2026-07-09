using System;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementProgressSample
    {
        public MovementProgressSample(
            Vector3 position,
            double distance,
            double bestDistance,
            int sampleCount,
            DateTime lastProgressUtc,
            MovementStateSnapshot movementState)
        {
            Position = position;
            Distance = distance;
            BestDistance = bestDistance;
            SampleCount = sampleCount;
            LastProgressUtc = lastProgressUtc;
            MovementState = movementState;
        }

        public Vector3 Position { get; }

        public double Distance { get; }

        public double BestDistance { get; }

        public int SampleCount { get; }

        public DateTime LastProgressUtc { get; }

        public MovementStateSnapshot MovementState { get; }
    }
}
