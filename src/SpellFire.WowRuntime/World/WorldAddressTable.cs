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
                  IntPtr.Zero)
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
            IntPtr targetGuid)
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
        }

        public static WorldAddressTable Empty
        {
            get { return new WorldAddressTable(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero); }
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
                       TargetGuid != IntPtr.Zero;
            }
        }
    }
}
