using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Process;
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

            return new Core.WowRuntime(
                processId,
                new MemoryWorldState(processId, memorySessions, addresses),
                new MemoryObjectManager(processId, memorySessions, addresses),
                new UnavailableMovementService(),
                new UnavailableNavigationService(),
                new UnavailableScriptService(),
                bot);
        }
    }
}
