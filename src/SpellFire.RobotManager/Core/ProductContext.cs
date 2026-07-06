using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Movement;
using SpellFire.WowRuntime.ObjectManager;
using SpellFire.WowRuntime.Scripting;
using SpellFire.WowRuntime.World;

namespace SpellFire.RobotManager.Core
{
    // Product-facing contract. Keep this surface narrow: Product code must consume
    // WoW semantics from WowRuntime, not lower-level hook or memory primitives.
    public sealed class ProductContext
    {
        public ProductContext(IWowRuntime runtime)
        {
            Runtime = runtime;
        }

        private IWowRuntime Runtime { get; }

        public int ProcessId
        {
            get { return Runtime.ProcessId; }
        }

        public IWorldState World
        {
            get { return Runtime.World; }
        }

        public IWorldSnapshotService WorldSnapshots
        {
            get { return Runtime.WorldSnapshots; }
        }

        public IObjectManager ObjectManager
        {
            get { return Runtime.ObjectManager; }
        }

        public IMovementService Movement
        {
            get { return Runtime.Movement; }
        }

        public IScriptService Scripts
        {
            get { return Runtime.Scripts; }
        }

        public WowRuntimeSnapshot Snapshot()
        {
            return Runtime.Snapshot();
        }
    }
}
