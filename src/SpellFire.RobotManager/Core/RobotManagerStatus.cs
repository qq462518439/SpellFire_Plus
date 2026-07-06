namespace SpellFire.RobotManager.Core
{
    public enum RobotManagerStatus
    {
        Ready = 0,
        InvalidProduct = 1,
        InvalidRuntime = 2,
        ProductAlreadyRunning = 3,
        NoProduct = 4,
        Paused = 5,
        Stopped = 6,
        ProductError = 7,
        Faulted = 8
    }
}
