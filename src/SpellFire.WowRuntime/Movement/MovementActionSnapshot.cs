namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementActionSnapshot
    {
        public MovementActionSnapshot(string action, string script, string runtimeReason, string detail)
        {
            Action = action ?? string.Empty;
            Script = script ?? string.Empty;
            RuntimeReason = runtimeReason ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Action { get; }

        public string Script { get; }

        public string RuntimeReason { get; }

        public string Detail { get; }
    }
}
