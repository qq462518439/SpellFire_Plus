using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Process;
using SpellFire.Runtime.Bootstrap;
using SpellFire.Runtime.Contracts;
using SpellFire.WowRuntime.Bot;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Movement;
using SpellFire.WowRuntime.Navigation;
using SpellFire.WowRuntime.ObjectManager;
using SpellFire.WowRuntime.Scripting;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Infrastructure
{
    public sealed class WowRuntimeFactory : IWowRuntimeFactory
    {
        private readonly IMemorySessionFactory memorySessions;
        private readonly IWorldAddressProvider addresses;

        public WowRuntimeFactory()
            : this(new MemorySessionFactory(), new StaticWorldAddressProvider())
        {
        }

        public WowRuntimeFactory(IMemorySessionFactory memorySessions, IWorldAddressProvider addresses)
        {
            this.memorySessions = memorySessions;
            this.addresses = addresses;
        }

        public IWowRuntime Create(int processId)
        {
            BotController bot = new BotController();
            bot.AddState(new IdleBotState());
            MemoryObjectManager objectManager = new MemoryObjectManager(processId, memorySessions, addresses);
            IRuntimeFacade runtimeFacade = RuntimeCompositionRoot.CreateDefaultFacade();
            RuntimeFacadeScriptService scripts = new RuntimeFacadeScriptService(processId, runtimeFacade);

            IWorldState world = new MemoryWorldState(processId, memorySessions, addresses, objectManager);

            return new Core.WowRuntime(
                processId,
                world,
                new ObjectManagerWorldSnapshotService(processId, objectManager, world),
                objectManager,
                new ScriptMovementService(scripts, world, processId, runtimeFacade),
                new UnavailableNavigationService(),
                scripts,
                bot);
        }
    }
}
