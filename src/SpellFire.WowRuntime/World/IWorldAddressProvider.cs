namespace SpellFire.WowRuntime.World
{
    public interface IWorldAddressProvider
    {
        WorldAddressTable GetAddressTable(int processId);
    }
}
