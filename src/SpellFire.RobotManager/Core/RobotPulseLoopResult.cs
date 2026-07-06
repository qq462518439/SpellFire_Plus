namespace SpellFire.RobotManager.Core
{
    public sealed class RobotPulseLoopResult
    {
        public RobotPulseLoopResult(bool success, RobotManagerStatus status, string detail)
        {
            Success = success;
            Status = status;
            Detail = detail ?? string.Empty;
        }

        public bool Success { get; }

        public RobotManagerStatus Status { get; }

        public string Detail { get; }

        public static RobotPulseLoopResult Ok(string detail)
        {
            return new RobotPulseLoopResult(true, RobotManagerStatus.Ready, detail);
        }

        public static RobotPulseLoopResult Fail(RobotManagerStatus status, string detail)
        {
            return new RobotPulseLoopResult(false, status, detail);
        }
    }
}
