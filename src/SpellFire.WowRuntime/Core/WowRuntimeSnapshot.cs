using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Core
{
    public sealed class WowRuntimeSnapshot
    {
        public WowRuntimeSnapshot(int processId, bool ready, WowRuntimeStatus status, string detail, PlayerSnapshot player)
        {
            ProcessId = processId;
            Ready = ready;
            Status = status;
            Detail = detail ?? string.Empty;
            Player = player;
        }

        public int ProcessId { get; }

        public bool Ready { get; }

        public WowRuntimeStatus Status { get; }

        public string Detail { get; }

        public PlayerSnapshot Player { get; }
    }
}
