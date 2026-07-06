using SpellFire.RobotManager.Core;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Scripting;
using SpellFire.WowRuntime.World;

namespace SpellFire.RobotManager.Testing
{
    public sealed class ContextProbeProduct : IProduct, IProductFaultSink
    {
        public ContextProbeProduct()
        {
            Name = "ContextProbeProduct";
            State = ProductState.Created;
        }

        public string Name { get; }

        public ProductState State { get; private set; }

        public int PulseCount { get; private set; }

        public bool ScriptExecuted { get; private set; }

        public string ScriptReason { get; private set; }

        public bool SnapshotAttempted { get; private set; }

        public bool SnapshotSucceeded { get; private set; }

        public string SnapshotStatus { get; private set; }

        public bool InWorld { get; private set; }

        public bool HasPlayer { get; private set; }

        public int ObjectCount { get; private set; }

        public void Start(ProductContext context)
        {
            State = ProductState.Running;
        }

        public void Pulse(ProductContext context)
        {
            PulseCount++;

            WowRuntimeResult<ScriptExecutionSnapshot> script = context.Scripts.Execute("DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_ROBOT_CONTEXT_OK\");");
            ScriptExecuted = script.Success;
            ScriptReason = script.Success && script.Value != null ? script.Value.Reason : script.Status.ToString();

            SnapshotAttempted = true;
            WowRuntimeResult<RuntimeWorldSnapshot> snapshot = context.WorldSnapshots.Capture(64);
            SnapshotSucceeded = snapshot.Success;
            SnapshotStatus = snapshot.Status.ToString();
            if (snapshot.Success && snapshot.Value != null)
            {
                InWorld = snapshot.Value.InWorld;
                HasPlayer = snapshot.Value.HasPlayer;
                ObjectCount = snapshot.Value.ObjectCount;
            }
        }

        public void Pause(ProductContext context)
        {
            State = ProductState.Paused;
        }

        public void Resume(ProductContext context)
        {
            State = ProductState.Running;
        }

        public void Stop(ProductContext context)
        {
            State = ProductState.Stopped;
        }

        public void MarkFaulted()
        {
            State = ProductState.Faulted;
        }
    }
}
