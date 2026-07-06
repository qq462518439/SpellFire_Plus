namespace SpellFire.RobotManager.Core
{
    public sealed class RobotManagerResult
    {
        public RobotManagerResult(bool success, RobotManagerStatus status, string productName, string detail)
        {
            Success = success;
            Status = status;
            ProductName = productName ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public bool Success { get; }

        public RobotManagerStatus Status { get; }

        public string ProductName { get; }

        public string Detail { get; }

        public static RobotManagerResult Ok(RobotManagerStatus status, string productName, string detail)
        {
            return new RobotManagerResult(true, status, productName, detail);
        }

        public static RobotManagerResult Fail(RobotManagerStatus status, string productName, string detail)
        {
            return new RobotManagerResult(false, status, productName, detail);
        }
    }
}
