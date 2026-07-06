using System;
using System.Diagnostics;
using System.Linq;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Process;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Movement;
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
            WorldAddressTable table = addresses.GetAddressTable(processId);
            WowRuntimeResult<PlayerSnapshot> objectPlayer = TryGetPlayerFromObjectManager(table);
            if (table == null)
            {
                return !objectPlayer.Success && objectPlayer.Status == WowRuntimeStatus.NotStarted
                    ? WowRuntimeResult<PlayerSnapshot>.Fail(WowRuntimeStatus.AddressTableMissing, "World address table is not available.")
                    : objectPlayer;
            }

            int continentId = 0;
            bool continentKnown = TryReadContinentId(table, out continentId);

            if (objectPlayer.Success)
            {
                int clickToMoveType = ReadClickToMoveType(table);
                return WowRuntimeResult<PlayerSnapshot>.Ok(new PlayerSnapshot(
                    continentKnown ? continentId : 0,
                    continentKnown,
                    continentKnown ? continentId : 0,
                    continentKnown ? ContinentNames.GetName(continentId) : string.Empty,
                    objectPlayer.Value.Position,
                    ToMovementFlags(0, clickToMoveType),
                    clickToMoveType,
                    ToClickToMoveState(clickToMoveType)));
            }

            if (!table.HasPlayer)
            {
                return !objectPlayer.Success && objectPlayer.Status == WowRuntimeStatus.NotStarted
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
                    int clickToMoveType = ReadClickToMoveType(robot, table);

                    return WowRuntimeResult<PlayerSnapshot>.Ok(new PlayerSnapshot(
                        mapId,
                        mapId != 0,
                        mapId,
                        mapId == 0 ? string.Empty : ContinentNames.GetName(mapId),
                        new Vector3(x, y, z, rotation),
                        ToMovementFlags(flags, clickToMoveType),
                        clickToMoveType,
                        ToClickToMoveState(clickToMoveType)));
                }
            }
            catch (Exception ex)
            {
                return WowRuntimeResult<PlayerSnapshot>.Fail(WowRuntimeStatus.ReadFailed, ex.Message);
            }
        }

        public WowRuntimeResult<WorldPhaseSnapshot> GetPhase()
        {
            if (!IsProcessAvailable())
            {
                return WowRuntimeResult<WorldPhaseSnapshot>.Fail(WowRuntimeStatus.ProcessUnavailable, "Target process is not available.");
            }

            WorldAddressTable table = addresses.GetAddressTable(processId);
            if (table == null || table.InGameFlag == IntPtr.Zero || table.LoadingOrConnectingFlag == IntPtr.Zero)
            {
                return WowRuntimeResult<WorldPhaseSnapshot>.Fail(WowRuntimeStatus.AddressTableMissing, "World phase addresses are not available.");
            }

            try
            {
                using (IMemoryRobot robot = memorySessions.Open(processId))
                {
                    ProcessModuleInfo mainModule = robot.Modules.GetModules().FirstOrDefault();
                    if (mainModule == null || mainModule.BaseAddress == IntPtr.Zero)
                    {
                        return WowRuntimeResult<WorldPhaseSnapshot>.Fail(WowRuntimeStatus.ReadFailed, "Main module base address is not available.");
                    }

                    byte inGameRaw = robot.Reader.Read<byte>(IntPtr.Add(mainModule.BaseAddress, table.InGameFlag.ToInt32()));
                    int loadingRaw = robot.Reader.Read<int>(IntPtr.Add(mainModule.BaseAddress, table.LoadingOrConnectingFlag.ToInt32()));
                    bool inGame = inGameRaw > 0;
                    bool loading = loadingRaw != 0;
                    WorldPhaseKind phase = loading
                        ? WorldPhaseKind.LoadingOrConnecting
                        : inGame ? WorldPhaseKind.InWorld : WorldPhaseKind.LoginOrCharacterList;

                    string detail = string.Format(
                        "InGameRaw=0x{0:X2} LoadingRaw=0x{1:X8} InGameAddress=0x{2:X} LoadingAddress=0x{3:X}",
                        inGameRaw,
                        loadingRaw,
                        unchecked((long)IntPtr.Add(mainModule.BaseAddress, table.InGameFlag.ToInt32())),
                        unchecked((long)IntPtr.Add(mainModule.BaseAddress, table.LoadingOrConnectingFlag.ToInt32())));

                    return WowRuntimeResult<WorldPhaseSnapshot>.Ok(new WorldPhaseSnapshot(
                        phase,
                        inGame,
                        loading,
                        "Usefuls",
                        detail));
                }
            }
            catch (Exception ex)
            {
                return WowRuntimeResult<WorldPhaseSnapshot>.Fail(WowRuntimeStatus.ReadFailed, ex.Message);
            }
        }

        public WowRuntimeResult<MovementStateSnapshot> GetMovementState()
        {
            if (!IsProcessAvailable())
            {
                return WowRuntimeResult<MovementStateSnapshot>.Fail(WowRuntimeStatus.ProcessUnavailable, "Target process is not available.");
            }

            WorldAddressTable table = addresses.GetAddressTable(processId);
            if (table == null || table.ClickToMoveType == IntPtr.Zero)
            {
                return WowRuntimeResult<MovementStateSnapshot>.Fail(WowRuntimeStatus.AddressTableMissing, "Movement state addresses are not available.");
            }

            try
            {
                int clickToMoveType = ReadClickToMoveType(table);
                ClickToMoveState clickToMoveState = ToClickToMoveState(clickToMoveType);
                float speed = 0;
                bool speedKnown = TryReadLocalPlayerSpeed(out speed);
                MovementFlags flags = ToMovementFlags(speedKnown && speed > 0 ? 1 : 0, clickToMoveType);
                bool inMovement = flags != MovementFlags.None || clickToMoveState == ClickToMoveState.Move || (speedKnown && speed > 0);
                WowRuntimeResult<WorldPhaseSnapshot> phase = GetPhase();
                string phaseText = phase.Success ? phase.Value.Phase.ToString() : "Unknown";
                string detail = string.Format(
                    "Phase={0} ClickToMoveTypeRaw={1} ClickToMoveState={2} SpeedKnown={3} Speed={4:0.###}",
                    phaseText,
                    clickToMoveType,
                    clickToMoveState,
                    speedKnown,
                    speed);

                return WowRuntimeResult<MovementStateSnapshot>.Ok(new MovementStateSnapshot(
                    inMovement,
                    flags,
                    clickToMoveType,
                    clickToMoveState,
                    speed,
                    speedKnown,
                    "WorldState",
                    detail));
            }
            catch (Exception ex)
            {
                return WowRuntimeResult<MovementStateSnapshot>.Fail(WowRuntimeStatus.ReadFailed, ex.Message);
            }
        }

        private bool TryReadLocalPlayerSpeed(out float speed)
        {
            speed = 0;
            if (objectManager == null)
            {
                return false;
            }

            WowRuntimeResult<WowObjectSnapshot> me = objectManager.GetMe();
            if (!me.Success || me.Value == null || me.Value.BaseAddress == 0)
            {
                return false;
            }

            try
            {
                using (IMemoryRobot robot = memorySessions.Open(processId))
                {
                    uint movementInfo = robot.Reader.Read<uint>(new IntPtr(unchecked((int)(me.Value.BaseAddress + 216))));
                    if (movementInfo == 0)
                    {
                        return false;
                    }

                    speed = robot.Reader.Read<float>(new IntPtr(unchecked((int)(movementInfo + 140))));
                    if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0)
                    {
                        speed = 0;
                        return false;
                    }

                    return true;
                }
            }
            catch
            {
                speed = 0;
                return false;
            }
        }

        private bool IsProcessAvailable()
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private int ReadClickToMoveType(WorldAddressTable table)
        {
            try
            {
                using (IMemoryRobot robot = memorySessions.Open(processId))
                {
                    return ReadClickToMoveType(robot, table);
                }
            }
            catch
            {
                return 0;
            }
        }

        private static int ReadClickToMoveType(IMemoryRobot robot, WorldAddressTable table)
        {
            if (table == null || table.ClickToMoveType == IntPtr.Zero)
            {
                return 0;
            }

            ProcessModuleInfo mainModule = robot.Modules.GetModules().FirstOrDefault();
            if (mainModule == null || mainModule.BaseAddress == IntPtr.Zero)
            {
                return 0;
            }

            return robot.Reader.Read<int>(IntPtr.Add(mainModule.BaseAddress, table.ClickToMoveType.ToInt32()));
        }

        private static MovementFlags ToMovementFlags(int raw, int clickToMoveType)
        {
            MovementFlags flags = raw == 0 ? MovementFlags.None : MovementFlags.Moving;
            if (clickToMoveType == (int)ClickToMoveState.Move)
            {
                flags |= MovementFlags.Moving;
            }

            return flags;
        }

        private static ClickToMoveState ToClickToMoveState(int raw)
        {
            switch (raw)
            {
                case 1:
                    return ClickToMoveState.FaceTarget;
                case 2:
                    return ClickToMoveState.Face;
                case 3:
                    return ClickToMoveState.StopThrowsException;
                case 4:
                    return ClickToMoveState.Move;
                case 5:
                    return ClickToMoveState.NpcInteract;
                case 6:
                    return ClickToMoveState.Loot;
                case 7:
                    return ClickToMoveState.ObjInteract;
                case 8:
                    return ClickToMoveState.FaceOther;
                case 9:
                    return ClickToMoveState.Skin;
                case 10:
                    return ClickToMoveState.AttackPosition;
                case 11:
                    return ClickToMoveState.AttackGuid;
                case 12:
                    return ClickToMoveState.ConstantFace;
                case 13:
                    return ClickToMoveState.None;
                case 16:
                    return ClickToMoveState.Attack;
                case 19:
                    return ClickToMoveState.Idle;
                default:
                    return ClickToMoveState.Unknown;
            }
        }

        private bool TryReadContinentId(WorldAddressTable table, out int continentId)
        {
            continentId = 0;
            if (table == null || table.ObjectManager == IntPtr.Zero || table.ContinentIdOffset == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                using (IMemoryRobot robot = memorySessions.Open(processId))
                {
                    uint clientConnection = robot.Reader.Read<uint>(table.ObjectManager);
                    if (clientConnection == 0)
                    {
                        return false;
                    }

                    uint objectManagerAddress = robot.Reader.Read<uint>(IntPtr.Add(new IntPtr((int)clientConnection), 0x2ED0));
                    if (objectManagerAddress == 0)
                    {
                        return false;
                    }

                    continentId = robot.Reader.Read<int>(IntPtr.Add(new IntPtr((int)objectManagerAddress), table.ContinentIdOffset.ToInt32()));
                    return continentId > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private WowRuntimeResult<PlayerSnapshot> TryGetPlayerFromObjectManager(WorldAddressTable table)
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

            int clickToMoveType = ReadClickToMoveType(table);
            return WowRuntimeResult<PlayerSnapshot>.Ok(new PlayerSnapshot(
                0,
                false,
                0,
                string.Empty,
                me.Value.Position,
                ToMovementFlags(0, clickToMoveType),
                clickToMoveType,
                ToClickToMoveState(clickToMoveType)));
        }
    }
}
