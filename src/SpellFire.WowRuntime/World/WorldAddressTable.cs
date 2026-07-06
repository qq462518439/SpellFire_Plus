using System;

namespace SpellFire.WowRuntime.World
{
    public sealed class WorldAddressTable
    {
        public WorldAddressTable(
            IntPtr mapId,
            IntPtr playerX,
            IntPtr playerY,
            IntPtr playerZ,
            IntPtr playerRotation,
            IntPtr movementFlags)
            : this(
                  mapId,
                  playerX,
                  playerY,
                  playerZ,
                  playerRotation,
                  movementFlags,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  IntPtr.Zero,
                  0)
        {
        }

        public WorldAddressTable(
            IntPtr mapId,
            IntPtr playerX,
            IntPtr playerY,
            IntPtr playerZ,
            IntPtr playerRotation,
            IntPtr movementFlags,
            IntPtr objectManager,
            IntPtr firstObject,
            IntPtr nextObjectOffset,
            IntPtr localGuid,
            IntPtr targetGuid,
            IntPtr continentIdOffset,
            IntPtr objectGuidOffset,
            IntPtr objectTypeOffset,
            IntPtr objectEntryOffset,
            IntPtr unitPositionOffset,
            IntPtr gameObjectPositionOffset,
            IntPtr inGameFlag,
            IntPtr loadingOrConnectingFlag,
            IntPtr clickToMoveType,
            int scanLimit)
        {
            MapId = mapId;
            PlayerX = playerX;
            PlayerY = playerY;
            PlayerZ = playerZ;
            PlayerRotation = playerRotation;
            MovementFlags = movementFlags;
            ObjectManager = objectManager;
            FirstObject = firstObject;
            NextObjectOffset = nextObjectOffset;
            LocalGuid = localGuid;
            TargetGuid = targetGuid;
            ContinentIdOffset = continentIdOffset;
            ObjectGuidOffset = objectGuidOffset;
            ObjectTypeOffset = objectTypeOffset;
            ObjectEntryOffset = objectEntryOffset;
            UnitPositionOffset = unitPositionOffset;
            GameObjectPositionOffset = gameObjectPositionOffset;
            InGameFlag = inGameFlag;
            LoadingOrConnectingFlag = loadingOrConnectingFlag;
            ClickToMoveType = clickToMoveType;
            ScanLimit = scanLimit;
        }

        public static WorldAddressTable Empty
        {
            get
            {
                return new WorldAddressTable(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0);
            }
        }

        public IntPtr MapId { get; }

        public IntPtr PlayerX { get; }

        public IntPtr PlayerY { get; }

        public IntPtr PlayerZ { get; }

        public IntPtr PlayerRotation { get; }

        public IntPtr MovementFlags { get; }

        public IntPtr ObjectManager { get; }

        public IntPtr FirstObject { get; }

        public IntPtr NextObjectOffset { get; }

        public IntPtr LocalGuid { get; }

        public IntPtr TargetGuid { get; }

        public IntPtr ContinentIdOffset { get; }

        public IntPtr ObjectGuidOffset { get; }

        public IntPtr ObjectTypeOffset { get; }

        public IntPtr ObjectEntryOffset { get; }

        public IntPtr UnitPositionOffset { get; }

        public IntPtr GameObjectPositionOffset { get; }

        public IntPtr InGameFlag { get; }

        public IntPtr LoadingOrConnectingFlag { get; }

        public IntPtr ClickToMoveType { get; }

        public int ScanLimit { get; }

        public bool HasPlayer
        {
            get
            {
                return MapId != IntPtr.Zero &&
                       PlayerX != IntPtr.Zero &&
                       PlayerY != IntPtr.Zero &&
                       PlayerZ != IntPtr.Zero &&
                       MovementFlags != IntPtr.Zero;
            }
        }

        public bool HasObjectManager
        {
            get
            {
                return ObjectManager != IntPtr.Zero &&
                       FirstObject != IntPtr.Zero &&
                       NextObjectOffset != IntPtr.Zero &&
                       LocalGuid != IntPtr.Zero &&
                       TargetGuid != IntPtr.Zero &&
                       ContinentIdOffset != IntPtr.Zero &&
                       ObjectGuidOffset != IntPtr.Zero &&
                       ObjectTypeOffset != IntPtr.Zero &&
                       UnitPositionOffset != IntPtr.Zero &&
                       GameObjectPositionOffset != IntPtr.Zero &&
                       ScanLimit > 0;
            }
        }
    }
}
