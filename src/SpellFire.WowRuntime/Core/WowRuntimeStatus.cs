namespace SpellFire.WowRuntime.Core
{
    public enum WowRuntimeStatus
    {
        Ready = 0,
        NotStarted = 1,
        ProcessUnavailable = 2,
        AddressTableMissing = 3,
        FeatureUnavailable = 4,
        ReadFailed = 5,
        InvalidArgument = 6,
        ObjectNotFound = 7,
        ObjectManagerUnavailable = 8
    }
}
