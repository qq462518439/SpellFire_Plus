namespace SpellFire.WowRuntime.Navigation
{
    public enum NavigationExecutionStatus
    {
        Success = 0,
        PathUnavailable = 1,
        MovementFailed = 2,
        PlayerUnavailable = 3,
        Timeout = 4,
        Stuck = 5,
        InvalidArgument = 6
    }
}
