namespace SpellFire.WowRuntime.ObjectManager
{
    public sealed class ObjectManagerDiagnosticSnapshot
    {
        public ObjectManagerDiagnosticSnapshot(
            bool processExists,
            bool sessionOpen,
            bool addressTableReady,
            string stage,
            string detail,
            uint clientConnection,
            uint objectManager,
            ulong localGuid,
            ulong targetGuid,
            uint firstObject,
            int scanned,
            int readableObjects,
            int failedObjects,
            uint firstFailedObject,
            string firstFailedStage,
            string firstFailedDetail)
        {
            ProcessExists = processExists;
            SessionOpen = sessionOpen;
            AddressTableReady = addressTableReady;
            Stage = stage ?? string.Empty;
            Detail = detail ?? string.Empty;
            ClientConnection = clientConnection;
            ObjectManager = objectManager;
            LocalGuid = localGuid;
            TargetGuid = targetGuid;
            FirstObject = firstObject;
            Scanned = scanned;
            ReadableObjects = readableObjects;
            FailedObjects = failedObjects;
            FirstFailedObject = firstFailedObject;
            FirstFailedStage = firstFailedStage ?? string.Empty;
            FirstFailedDetail = firstFailedDetail ?? string.Empty;
        }

        public bool ProcessExists { get; }

        public bool SessionOpen { get; }

        public bool AddressTableReady { get; }

        public string Stage { get; }

        public string Detail { get; }

        public uint ClientConnection { get; }

        public uint ObjectManager { get; }

        public ulong LocalGuid { get; }

        public ulong TargetGuid { get; }

        public uint FirstObject { get; }

        public int Scanned { get; }

        public int ReadableObjects { get; }

        public int FailedObjects { get; }

        public uint FirstFailedObject { get; }

        public string FirstFailedStage { get; }

        public string FirstFailedDetail { get; }
    }
}
