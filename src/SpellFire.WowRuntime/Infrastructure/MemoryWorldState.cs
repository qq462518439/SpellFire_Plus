using System;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.ObjectManager;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Infrastructure
{
    public sealed class MemoryWorldState : IWorldState
    {
        private readonly int processId;
        private readonly IMemorySessionFactory memorySessions;
        private readonly IWorldAddressProvider addresses;
        private readonly IObjectManager objectManager;

        public MemoryWorldState(int processId, IMemorySessionFactory memorySessions, IWorldAddressProvider addresses)
            : this(processId, memorySessions, addresses, null)
        {
        }

        public MemoryWorldState(int processId, IMemorySessionFactory memorySessions, IWorldAddressProvider addresses, IObjectManager objectManager)
        {
            this.processId = processId;
            this.memorySessions = memorySessions;
            this.addresses = addresses;
            this.objectManager = objectManager;
        }

        public WowRuntimeResult<PlayerSnapshot> GetPlayer()
        {
            WowRuntimeResult<PlayerSnapshot> objectPlayer = TryGetPlayerFromObjectManager();
            if (objectPlayer.Success)
            {
                return objectPlayer;
            }

            WorldAddressTable table = addresses.GetAddressTable(processId);
            if (table == null || !table.HasPlayer)
            {
                return objectPlayer.Status == WowRuntimeStatus.NotStarted
                    ? WowRuntimeResult<PlayerSnapshot>.Fail(WowRuntimeStatus.AddressTableMissing, "World address table is not available.")
                    : objectPlayer;
            }

            try
            {
                using (IMemoryRobot robot = memorySessions.Open(processId))
                {
                    int mapId = robot.Reader.Read<int>(table.MapId);
                    float x = robot.Reader.Read<float>(table.PlayerX);
                    float y = robot.Reader.Read<float>(table.PlayerY);
                    float z = robot.Reader.Read<float>(table.PlayerZ);
                    float rotation = table.PlayerRotation == IntPtr.Zero ? 0 : robot.Reader.Read<float>(table.PlayerRotation);
                    int flags = robot.Reader.Read<int>(table.MovementFlags);

                    return WowRuntimeResult<PlayerSnapshot>.Ok(
                        new PlayerSnapshot(mapId, new Vector3(x, y, z, rotation), ToMovementFlags(flags)));
                }
            }
            catch (Exception ex)
            {
                return WowRuntimeResult<PlayerSnapshot>.Fail(WowRuntimeStatus.ReadFailed, ex.Message);
            }
        }

        private static MovementFlags ToMovementFlags(int raw)
        {
            return raw == 0 ? MovementFlags.None : MovementFlags.Moving;
        }

        private WowRuntimeResult<PlayerSnapshot> TryGetPlayerFromObjectManager()
        {
            if (objectManager == null)
            {
                return WowRuntimeResult<PlayerSnapshot>.Fail(WowRuntimeStatus.NotStarted, "Object manager fallback is not configured.");
            }

            WowRuntimeResult<WowObjectSnapshot> me = objectManager.GetMe();
            if (!me.Success)
            {
                return WowRuntimeResult<PlayerSnapshot>.Fail(me.Status, me.Detail);
            }

            return WowRuntimeResult<PlayerSnapshot>.Ok(new PlayerSnapshot(0, me.Value.Position, MovementFlags.None));
        }
    }
}
