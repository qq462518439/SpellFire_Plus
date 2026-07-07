namespace SpellFire.WowRuntime.Movement
{
    public enum MovementProgressStatus
    {
        NotStarted = 0,
        Moving = 1,
        Arrived = 2,
        Timeout = 3,
        Stuck = 4,
        MovementFailed = 5,
        PlayerUnavailable = 6,
        InvalidArgument = 7
    }
}
