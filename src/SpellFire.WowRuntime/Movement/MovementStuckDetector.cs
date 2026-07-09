using System;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementStuckDetector
    {
        public bool IsStuck(MovementProgressSample sample, MovementExecutionOptions options, DateTime nowUtc)
        {
            if (sample == null || options == null)
            {
                return false;
            }

            return (nowUtc - sample.LastProgressUtc).TotalMilliseconds > 2600 &&
                   sample.BestDistance > options.ArrivalDistance + 0.8;
        }
    }
}
