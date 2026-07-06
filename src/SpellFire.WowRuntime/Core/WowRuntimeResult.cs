namespace SpellFire.WowRuntime.Core
{
    public sealed class WowRuntimeResult<T>
    {
        private WowRuntimeResult(bool success, WowRuntimeStatus status, string detail, T value)
        {
            Success = success;
            Status = status;
            Detail = detail ?? string.Empty;
            Value = value;
        }

        public bool Success { get; }

        public WowRuntimeStatus Status { get; }

        public string Detail { get; }

        public T Value { get; }

        public static WowRuntimeResult<T> Ok(T value)
        {
            return new WowRuntimeResult<T>(true, WowRuntimeStatus.Ready, string.Empty, value);
        }

        public static WowRuntimeResult<T> Fail(WowRuntimeStatus status, string detail)
        {
            return new WowRuntimeResult<T>(false, status, detail, default(T));
        }
    }
}
