namespace SpellFire.WowRuntime.Core
{
    public interface IWowRuntimeFactory
    {
        IWowRuntime Create(int processId);
    }
}
