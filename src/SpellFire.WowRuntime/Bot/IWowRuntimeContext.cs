using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Movement;
using SpellFire.WowRuntime.Navigation;
using SpellFire.WowRuntime.ObjectManager;
using SpellFire.WowRuntime.Scripting;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Bot
{
    public interface IWowRuntimeContext
    {
        IWorldState World { get; }

        IObjectManager ObjectManager { get; }

        IMovementService Movement { get; }

        INavigationService Navigation { get; }

        IScriptService Scripts { get; }

        WowRuntimeSnapshot Snapshot();
    }
}
