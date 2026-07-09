namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementExecutionOptions
    {
        public MovementExecutionOptions(float arrivalDistance, int minimumTimeoutMs)
        {
            ArrivalDistance = arrivalDistance;
            MinimumTimeoutMs = minimumTimeoutMs;
        }

        public float ArrivalDistance { get; }

        public int MinimumTimeoutMs { get; }
    }
}
