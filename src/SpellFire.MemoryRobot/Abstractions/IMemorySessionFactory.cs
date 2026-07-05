namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IMemorySessionFactory
    {
        IMemoryRobot Open(int processId);
    }
}
