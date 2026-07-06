using SpellFire.WowRuntime.Bot;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Movement;
using SpellFire.WowRuntime.Navigation;
using SpellFire.WowRuntime.ObjectManager;
using SpellFire.WowRuntime.Scripting;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Infrastructure
{
    public sealed class RuntimeContext : IWowRuntimeContext
    {
        private readonly IWowRuntime runtime;

        public RuntimeContext(IWowRuntime runtime)
        {
            this.runtime = runtime;
        }

        public IWorldState World
        {
            get { return runtime.World; }
        }

        public IObjectManager ObjectManager
        {
            get { return runtime.ObjectManager; }
        }

        public IMovementService Movement
        {
            get { return runtime.Movement; }
        }

        public INavigationService Navigation
        {
            get { return runtime.Navigation; }
        }

        public IScriptService Scripts
        {
            get { return runtime.Scripts; }
        }

        public WowRuntimeSnapshot Snapshot()
        {
            return runtime.Snapshot();
        }
    }
}
