namespace SpellFire.WowRuntime.World
{
    public sealed class EmptyWorldAddressProvider : IWorldAddressProvider
    {
        public WorldAddressTable GetAddressTable(int processId)
        {
            return WorldAddressTable.Empty;
        }
    }
}
