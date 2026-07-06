namespace SpellFire.WowRuntime.Scripting
{
    public sealed class ScriptExecutionSnapshot
    {
        public ScriptExecutionSnapshot(int processId, string operation, string reason, string detail)
        {
            ProcessId = processId;
            Operation = operation ?? string.Empty;
            Reason = reason ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public int ProcessId { get; }

        public string Operation { get; }

        public string Reason { get; }

        public string Detail { get; }
    }
}
