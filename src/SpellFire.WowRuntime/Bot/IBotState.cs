namespace SpellFire.WowRuntime.Bot
{
    public interface IBotState
    {
        string Name { get; }

        int Priority { get; }

        bool CanRun(IWowRuntimeContext context);

        void Run(IWowRuntimeContext context);
    }
}
