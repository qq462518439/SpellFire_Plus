using SpellFire.WowRuntime.Bot;
using SpellFire.WowRuntime.Movement;
using SpellFire.WowRuntime.Navigation;
using SpellFire.WowRuntime.ObjectManager;
using SpellFire.WowRuntime.Scripting;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Core
{
    public interface IWowRuntime
    {
        int ProcessId { get; }

        IWorldState World { get; }

        IWorldSnapshotService WorldSnapshots { get; }

        IObjectManager ObjectManager { get; }

        IMovementService Movement { get; }

        INavigationService Navigation { get; }

        IScriptService Scripts { get; }

        IBotController Bot { get; }

        WowRuntimeSnapshot Snapshot();
    }
}
