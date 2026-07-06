using System;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Infrastructure;
using SpellFire.WowRuntime.Movement;
using SpellFire.WowRuntime.ObjectManager;
using SpellFire.WowRuntime.Scripting;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string command = GetArg(args, "--command", "object-snapshot");
            int processId = ParseInt(GetArg(args, "--pid", "0"), 0);
            int limit = ParseInt(GetArg(args, "--limit", "64"), 64);
            int scanLimit = ParseInt(GetArg(args, "--scan-limit", "512"), 512);
            ulong guid = ParseUlong(GetArg(args, "--guid", "0"), 0);
            int entry = ParseInt(GetArg(args, "--entry", "0"), 0);
            float radius = ParseFloat(GetArg(args, "--radius", "40"), 40);
            ObjectKind? kind = ParseKind(GetArg(args, "--kind", string.Empty));

            IWowRuntime runtime = new WowRuntimeFactory().Create(processId);

            switch (command)
            {
                case "world-phase":
                    return PrintWorldPhaseResult(command, processId, runtime.World.GetPhase());
                case "world-player":
                    return PrintPlayerResult(command, processId, runtime.World.GetPlayer());
                case "object-me":
                    return PrintObjectResult(command, processId, runtime.ObjectManager.GetMe());
                case "object-target":
                    return PrintObjectResult(command, processId, runtime.ObjectManager.GetTarget());
                case "object-by-guid":
                    return PrintObjectResult(command, processId, runtime.ObjectManager.GetObjectByGuid(guid));
                case "object-nearest":
                    return PrintObjectResult(command, processId, kind.HasValue
                        ? runtime.ObjectManager.GetNearestObjectByKind(kind.Value, new Vector3(0, 0, 0), radius)
                        : runtime.ObjectManager.GetNearestObject(new Vector3(0, 0, 0), radius));
                case "object-by-entry":
                    return PrintSnapshotResult(command, processId, runtime.ObjectManager.GetObjectsByEntry(entry, limit, scanLimit));
                case "object-nearby":
                    return PrintSnapshotResult(command, processId, kind.HasValue
                        ? runtime.ObjectManager.GetNearbyObjectsByKind(kind.Value, new Vector3(0, 0, 0), radius, limit, scanLimit)
                        : runtime.ObjectManager.GetNearbyObjects(new Vector3(0, 0, 0), radius, limit, scanLimit));
                case "object-nearby-list":
                    return PrintSnapshotResult(command, processId, kind.HasValue
                        ? runtime.ObjectManager.GetNearbyObjectsByKind(kind.Value, new Vector3(0, 0, 0), radius, limit, scanLimit)
                        : runtime.ObjectManager.GetNearbyObjects(new Vector3(0, 0, 0), radius, limit, scanLimit), true);
                case "object-list":
                    return PrintSnapshotResult(command, processId, kind.HasValue
                        ? runtime.ObjectManager.GetObjectsByKind(kind.Value, limit, scanLimit)
                        : runtime.ObjectManager.GetObjects(limit, scanLimit), true);
                case "object-snapshot":
                    return PrintSnapshotResult(command, processId, kind.HasValue
                        ? runtime.ObjectManager.GetObjectsByKind(kind.Value, limit, scanLimit)
                        : runtime.ObjectManager.GetObjects(limit, scanLimit));
                case "world-snapshot":
                    return PrintWorldSnapshotResult(command, processId, runtime.WorldSnapshots.Capture(limit));
                case "script-smoke":
                    return PrintScriptResult(command, processId, runtime.Scripts.LuaSmoke());
                case "script-exec":
                    return PrintScriptResult(command, processId, runtime.Scripts.Execute(GetArg(args, "--script", "DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_WOWRUNTIME_SCRIPT_OK\");")));
                case "movement-jump":
                    return PrintMovementResult(command, processId, runtime.Movement.Jump());
                case "movement-stop":
                    return PrintMovementResult(command, processId, runtime.Movement.StopMove());
                case "movement-stop-to":
                    return PrintMovementResult(command, processId, runtime.Movement.StopMoveTo());
                case "movement-go":
                    return PrintMovementResult(command, processId, runtime.Movement.Go(Array.Empty<Vector3>()));
                case "movement-state":
                    return PrintMovementStateResult(command, processId, runtime.Movement.GetMovementState());
                default:
                    Console.WriteLine("Result=Fail Command=\"{0}\" Reason=\"UnknownCommand\" Detail=\"Unsupported command.\" ProcessId={1}", Escape(command), processId);
                    return 2;
            }
        }

        private static int PrintObjectResult(string command, int processId, WowRuntimeResult<WowObjectSnapshot> result)
        {
            if (!result.Success)
            {
                Console.WriteLine(
                    "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"{2}\" Detail=\"{3}\"",
                    Escape(command),
                    processId,
                    result.Status,
                    Escape(result.Detail));
                return 1;
            }

            Console.WriteLine(
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" Object={3}",
                Escape(command),
                processId,
                result.Status,
                FormatObject(result.Value));
            return 0;
        }

        private static int PrintPlayerResult(string command, int processId, WowRuntimeResult<PlayerSnapshot> result)
        {
            if (!result.Success)
            {
                Console.WriteLine(
                    "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"{2}\" Detail=\"{3}\" Player=Unavailable",
                    Escape(command),
                    processId,
                    result.Status,
                    Escape(result.Detail));
                return 1;
            }

            Console.WriteLine(
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" MapId={3} MapIdKnown={4} ContinentId={5} ContinentName=\"{6}\" Pos=({7:0.###},{8:0.###},{9:0.###}) Rotation={10:0.###} Movement={11} ClickToMoveTypeRaw={12} ClickToMoveState={13}",
                Escape(command),
                processId,
                result.Status,
                result.Value.MapId,
                result.Value.MapIdKnown,
                result.Value.ContinentId,
                Escape(result.Value.ContinentName),
                result.Value.Position.X,
                result.Value.Position.Y,
                result.Value.Position.Z,
                result.Value.Position.Rotation,
                result.Value.Movement,
                result.Value.ClickToMoveTypeRaw,
                result.Value.ClickToMoveState);
            return 0;
        }

        private static int PrintSnapshotResult(string command, int processId, WowRuntimeResult<ObjectManagerSnapshot> result)
        {
            return PrintSnapshotResult(command, processId, result, false);
        }

        private static int PrintSnapshotResult(string command, int processId, WowRuntimeResult<ObjectManagerSnapshot> result, bool includeItems)
        {
            if (!result.Success)
            {
                Console.WriteLine(
                    "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"{2}\" Detail=\"{3}\" ObjectCount=0 Limit=0 Me=Unavailable Target=Unavailable",
                    Escape(command),
                    processId,
                    result.Status,
                    Escape(result.Detail));
                return 1;
            }

            Console.WriteLine(
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" SnapshotUtc=\"{3:O}\" AgeMs={4} ObjectCount={5} PlayerCount={6} UnitCount={7} GameObjectCount={8} ItemCount={9} CorpseCount={10} Limit={11} Scanned={12} LocalGuid=0x{13:X} TargetGuid=0x{14:X} Me={15} Target={16}",
                Escape(command),
                processId,
                result.Status,
                result.Value.SnapshotUtc,
                result.Value.AgeMs,
                result.Value.Count,
                result.Value.PlayerCount,
                result.Value.UnitCount,
                result.Value.GameObjectCount,
                result.Value.ItemCount,
                result.Value.CorpseCount,
                result.Value.Limit,
                result.Value.Scanned,
                result.Value.LocalGuid,
                result.Value.TargetGuid,
                FormatObject(result.Value.Me),
                FormatObject(result.Value.Target));

            if (includeItems)
            {
                for (int i = 0; i < result.Value.Objects.Count; i++)
                {
                    Console.WriteLine(
                        "ItemIndex={0} Object={1}",
                        i,
                        FormatObject(result.Value.Objects[i]));
                }
            }

            return 0;
        }

        private static int PrintWorldSnapshotResult(string command, int processId, WowRuntimeResult<RuntimeWorldSnapshot> result)
        {
            if (!result.Success)
            {
                Console.WriteLine(
                "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"{2}\" Detail=\"{3}\" Phase=Unavailable ObjectCount=0 PlayerCount=0 UnitCount=0 GameObjectCount=0 ItemCount=0 CorpseCount=0 Limit=0 Scanned=0 LocalGuid=0x0 TargetGuid=0x0 Me=Unavailable Target=Unavailable",
                    Escape(command),
                    processId,
                    result.Status,
                Escape(result.Detail));
                return 1;
            }

            RuntimeWorldSnapshot snapshot = result.Value;
            ObjectManagerSnapshot objects = snapshot.Objects;
            Console.WriteLine(
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" SnapshotUtc=\"{3:O}\" AgeMs={4} Phase={5} ObjectCount={6} PlayerCount={7} UnitCount={8} GameObjectCount={9} ItemCount={10} CorpseCount={11} Limit={12} Scanned={13} LocalGuid=0x{14:X} TargetGuid=0x{15:X} Player={16} Me={17} Target={18}",
                Escape(command),
                processId,
                result.Status,
                snapshot.SnapshotUtc,
                snapshot.AgeMs,
                FormatWorldPhase(snapshot.Phase),
                snapshot.ObjectCount,
                objects == null ? 0 : objects.PlayerCount,
                objects == null ? 0 : objects.UnitCount,
                objects == null ? 0 : objects.GameObjectCount,
                objects == null ? 0 : objects.ItemCount,
                objects == null ? 0 : objects.CorpseCount,
                objects == null ? 0 : objects.Limit,
                objects == null ? 0 : objects.Scanned,
                objects == null ? 0 : objects.LocalGuid,
                objects == null ? 0 : objects.TargetGuid,
                FormatPlayer(snapshot.Player),
                FormatObject(snapshot.Me),
                FormatObject(snapshot.Target));
            return 0;
        }

        private static int PrintWorldPhaseResult(string command, int processId, WowRuntimeResult<WorldPhaseSnapshot> result)
        {
            if (!result.Success)
            {
                Console.WriteLine(
                    "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"{2}\" Detail=\"{3}\" Phase=Unknown InGame=Unknown LoadingOrConnecting=Unknown Source=\"\"",
                    Escape(command),
                    processId,
                    result.Status,
                    Escape(result.Detail));
                return 1;
            }

            Console.WriteLine(
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" Phase={3} InGame={4} LoadingOrConnecting={5} Source=\"{6}\" Detail=\"{7}\"",
                Escape(command),
                processId,
                result.Status,
                result.Value.Phase,
                result.Value.InGame,
                result.Value.LoadingOrConnecting,
                Escape(result.Value.Source),
                Escape(result.Value.Detail));
            return 0;
        }

        private static int PrintScriptResult(string command, int processId, WowRuntimeResult<ScriptExecutionSnapshot> result)
        {
            if (!result.Success)
            {
                Console.WriteLine(
                    "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"{2}\" Detail=\"{3}\" Operation=\"\" RuntimeReason=\"\"",
                    Escape(command),
                    processId,
                    result.Status,
                    Escape(result.Detail));
                return 1;
            }

            Console.WriteLine(
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" Operation=\"{3}\" RuntimeReason=\"{4}\" Detail=\"{5}\"",
                Escape(command),
                processId,
                result.Status,
                Escape(result.Value.Operation),
                Escape(result.Value.Reason),
                Escape(result.Value.Detail));
            return 0;
        }

        private static int PrintMovementResult(string command, int processId, WowRuntimeResult<MovementActionSnapshot> result)
        {
            if (!result.Success)
            {
                Console.WriteLine(
                    "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"{2}\" Detail=\"{3}\" Action=\"\" RuntimeReason=\"\"",
                    Escape(command),
                    processId,
                    result.Status,
                    Escape(result.Detail));
                return 1;
            }

            Console.WriteLine(
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" Action=\"{3}\" RuntimeReason=\"{4}\" Detail=\"{5}\"",
                Escape(command),
                processId,
                result.Status,
                Escape(result.Value.Action),
                Escape(result.Value.RuntimeReason),
                Escape(result.Value.Detail));
            return 0;
        }

        private static int PrintMovementStateResult(string command, int processId, WowRuntimeResult<MovementStateSnapshot> result)
        {
            if (!result.Success)
            {
                Console.WriteLine(
                    "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"{2}\" Detail=\"{3}\" InMovement=Unknown Flags=Unknown ClickToMoveTypeRaw=Unknown ClickToMoveState=Unknown SpeedKnown=False Speed=0 Source=\"\"",
                    Escape(command),
                    processId,
                    result.Status,
                    Escape(result.Detail));
                return 1;
            }

            Console.WriteLine(
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" InMovement={3} Flags={4} ClickToMoveTypeRaw={5} ClickToMoveState={6} SpeedKnown={7} Speed={8:0.###} Source=\"{9}\" Detail=\"{10}\"",
                Escape(command),
                processId,
                result.Status,
                result.Value.InMovement,
                result.Value.Flags,
                result.Value.ClickToMoveTypeRaw,
                result.Value.ClickToMoveState,
                result.Value.SpeedKnown,
                result.Value.Speed,
                Escape(result.Value.Source),
                Escape(result.Value.Detail));
            return 0;
        }

        private static string FormatObject(WowObjectSnapshot item)
        {
            if (item == null)
            {
                return "Unavailable";
            }

            return string.Format(
                "Guid=0x{0:X} Base=0x{1:X} Entry={2} Name=\"{3}\" Kind={4} Pos=({5:0.###},{6:0.###},{7:0.###}) Dist={8:0.###} Valid={9}",
                item.Guid,
                item.BaseAddress,
                item.Entry,
                Escape(item.Name),
                item.Kind,
                item.Position.X,
                item.Position.Y,
                item.Position.Z,
                item.DistanceFromMe,
                item.IsValid);
        }

        private static string FormatPlayer(PlayerSnapshot player)
        {
            if (player == null)
            {
                return "Unavailable";
            }

            return string.Format(
                "MapId={0} MapIdKnown={1} ContinentId={2} ContinentName=\"{3}\" Pos=({4:0.###},{5:0.###},{6:0.###}) Rotation={7:0.###} Movement={8} ClickToMoveTypeRaw={9} ClickToMoveState={10}",
                player.MapId,
                player.MapIdKnown,
                player.ContinentId,
                Escape(player.ContinentName),
                player.Position.X,
                player.Position.Y,
                player.Position.Z,
                player.Position.Rotation,
                player.Movement,
                player.ClickToMoveTypeRaw,
                player.ClickToMoveState);
        }

        private static string FormatWorldPhase(WorldPhaseSnapshot phase)
        {
            if (phase == null)
            {
                return "Unavailable";
            }

            return string.Format(
                "{0} InGame={1} LoadingOrConnecting={2} Source=\"{3}\" Detail=\"{4}\"",
                phase.Phase,
                phase.InGame,
                phase.LoadingOrConnecting,
                Escape(phase.Source),
                Escape(phase.Detail));
        }

        private static string GetArg(string[] args, string name, string fallback)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static ulong ParseUlong(string value, ulong fallback)
        {
            ulong parsed;
            return ulong.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static float ParseFloat(string value, float fallback)
        {
            float parsed;
            return float.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static ObjectKind? ParseKind(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            ObjectKind parsed;
            return Enum.TryParse(value, true, out parsed) ? parsed : (ObjectKind?)null;
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
