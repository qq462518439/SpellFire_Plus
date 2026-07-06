using System.Threading;

namespace SpellFire.WowRuntime.Bot
{
    public sealed class IdleBotState : IBotState
    {
        public string Name
        {
            get { return "Idle"; }
        }

        public int Priority
        {
            get { return int.MinValue; }
        }

        public bool CanRun(IWowRuntimeContext context)
        {
            return true;
        }

        public void Run(IWowRuntimeContext context)
        {
            Thread.Sleep(60);
        }
    }
}
