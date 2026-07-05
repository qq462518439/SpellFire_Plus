namespace SpellFire.RuntimeHost
{
    public enum RuntimeHostState
    {
        Unknown = 0,
        Attached = 1,
        PartiallyReady = 2,
        Ready = 3,
        Failed = 4,
        Detached = 5
    }
}
