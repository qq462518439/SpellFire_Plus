using System;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Infrastructure
{
    public sealed class MemoryWorldState : IWorldState
    {
        private readonly int processId;
        private readonly IMemorySessionFactory memorySessions;
        private readonly IWorldAddressProvider addresses;

        public MemoryWorldState(int processId, IMemorySessionFactory memorySessions, IWorldAddressProvider addresses)
        {
            this.processId = processId;
            this.memorySessions = memorySessions;
            this.addresses = addresses;
        }

        public WowRuntimeResult<PlayerSnapshot> GetPlayer()
        {
            WorldAddressTable table = addresses.GetAddressTable(processId);
            if (table == null || !table.HasPlayer)
            {
                return WowRuntimeResult<PlayerSnapshot>.Fail(WowRuntimeStatus.AddressTableMissing, "World address table is not available.");
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
    }
}
