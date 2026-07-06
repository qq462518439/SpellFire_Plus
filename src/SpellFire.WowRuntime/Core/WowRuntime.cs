using SpellFire.WowRuntime.Bot;
using SpellFire.WowRuntime.Movement;
using SpellFire.WowRuntime.Navigation;
using SpellFire.WowRuntime.ObjectManager;
using SpellFire.WowRuntime.Scripting;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Core
{
    public sealed class WowRuntime : IWowRuntime
    {
        public WowRuntime(
            int processId,
            IWorldState world,
            IObjectManager objectManager,
            IMovementService movement,
            INavigationService navigation,
            IScriptService scripts,
            IBotController bot)
        {
            ProcessId = processId;
            World = world;
            ObjectManager = objectManager;
            Movement = movement;
            Navigation = navigation;
            Scripts = scripts;
            Bot = bot;
        }

        public int ProcessId { get; }

        public IWorldState World { get; }

        public IObjectManager ObjectManager { get; }

        public IMovementService Movement { get; }

        public INavigationService Navigation { get; }

        public IScriptService Scripts { get; }

        public IBotController Bot { get; }

        public WowRuntimeSnapshot Snapshot()
        {
            WowRuntimeResult<PlayerSnapshot> player = World.GetPlayer();
            if (!player.Success)
            {
                return new WowRuntimeSnapshot(ProcessId, false, player.Status, player.Detail, null);
            }

            return new WowRuntimeSnapshot(ProcessId, true, WowRuntimeStatus.Ready, string.Empty, player.Value);
        }
    }
}
