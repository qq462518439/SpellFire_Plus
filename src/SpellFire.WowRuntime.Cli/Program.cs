using System;
using System.Globalization;
using System.Threading;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Infrastructure;
using SpellFire.WowRuntime.Movement;
using SpellFire.WowRuntime.Navigation;
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
            float x = ParseFloat(GetArg(args, "--x", "0"), 0);
            float y = ParseFloat(GetArg(args, "--y", "0"), 0);
            float z = ParseFloat(GetArg(args, "--z", "0"), 0);
            float fromX = ParseFloat(GetArg(args, "--from-x", "0"), 0);
            float fromY = ParseFloat(GetArg(args, "--from-y", "0"), 0);
            float fromZ = ParseFloat(GetArg(args, "--from-z", "0"), 0);
            float toX = ParseFloat(GetArg(args, "--to-x", "0"), 0);
            float toY = ParseFloat(GetArg(args, "--to-y", "0"), 0);
            float toZ = ParseFloat(GetArg(args, "--to-z", "0"), 0);
            int mapId = ParseInt(GetArg(args, "--map", "0"), 0);
            int timeoutMs = ParseInt(GetArg(args, "--timeout-ms", "4500"), 4500);
            int maxPoints = ParseInt(GetArg(args, "--max-points", "8"), 8);
            float arrival = ParseFloat(GetArg(args, "--arrival", "1.75"), 1.75f);
            bool hasX = HasArg(args, "--x");
            bool hasY = HasArg(args, "--y");
            bool hasZ = HasArg(args, "--z");
            string action = GetArg(args, "--action", "forward");
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
                case "object-diagnostic":
                    return PrintObjectDiagnosticResult(command, processId, runtime.ObjectManager.GetDiagnostic(scanLimit));
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
                case "movement-forward-start":
                    return PrintMovementResult(command, processId, runtime.Movement.StartMoveForward());
                case "movement-backward-start":
                    return PrintMovementResult(command, processId, runtime.Movement.StartMoveBackward());
                case "movement-strafe-left-start":
                    return PrintMovementResult(command, processId, runtime.Movement.StartStrafeLeft());
                case "movement-strafe-right-start":
                    return PrintMovementResult(command, processId, runtime.Movement.StartStrafeRight());
                case "movement-turn-left-start":
                    return PrintMovementResult(command, processId, runtime.Movement.StartTurnLeft());
                case "movement-turn-right-start":
                    return PrintMovementResult(command, processId, runtime.Movement.StartTurnRight());
                case "movement-turn-stop":
                    return PrintMovementResult(command, processId, runtime.Movement.StopTurn());
                case "movement-face-to":
                    return PrintMovementResult(command, processId, runtime.Movement.FaceTo(new Vector3(x, y, z)));
                case "movement-face-object":
                    return PrintMovementResult(command, processId, runtime.Movement.FaceObject(guid));
                case "movement-stop":
                    return PrintMovementResult(command, processId, runtime.Movement.StopMove());
                case "movement-stop-to":
                    return PrintMovementResult(command, processId, runtime.Movement.StopMoveTo());
                case "movement-go":
                    if (!hasX || !hasY || !hasZ)
                    {
                        return PrintMovementResult(command, processId, WowRuntimeResult<MovementActionSnapshot>.Fail(
                            WowRuntimeStatus.InvalidArgument,
                            "movement-go requires explicit --x --y --z. Refusing implicit 0,0,0 target."));
                    }

                    return PrintMovementResult(command, processId, runtime.Movement.Go(new[] { new Vector3(x, y, z) }));
                case "movement-ctm-diagnostic":
                    return PrintClickToMoveDiagnosticResult(command, processId, runtime.Movement.GetClickToMoveDiagnostic());
                case "movement-state":
                    return PrintMovementStateResult(command, processId, runtime.Movement.GetMovementState());
                case "movement-speed-sample":
                    return PrintMovementSpeedSampleResult(command, processId, runtime, action);
                case "navigation-capability":
                    return PrintNavigationCapabilityResult(command, processId, runtime.Navigation.GetCapability());
                case "navigation-find-path":
                    return PrintNavigationPathResult(command, processId, runtime.Navigation.FindPath(new PathQuery(
                        mapId,
                        new Vector3(fromX, fromY, fromZ),
                        new Vector3(toX, toY, toZ))));
                case "navigation-find-z":
                    return PrintNavigationFindZResult(command, processId, runtime.Navigation, mapId, x, y, z);
                case "navigation-execute-to":
                    if (!hasX || !hasY || !hasZ)
                    {
                        Console.WriteLine(
                            "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"InvalidArgument\" Detail=\"navigation-execute-to requires explicit --x --y --z.\"",
                            Escape(command),
                            processId);
                        return 1;
                    }

                    return PrintNavigationExecutionResult(command, processId, new NavigationExecutionService(runtime.Navigation, runtime.World, runtime.Movement).ExecuteTo(
                        new Vector3(x, y, z),
                        arrival,
                        timeoutMs,
                        maxPoints));
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

        private static int PrintObjectDiagnosticResult(string command, int processId, WowRuntimeResult<ObjectManagerDiagnosticSnapshot> result)
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

            ObjectManagerDiagnosticSnapshot item = result.Value;
            bool ready = item.ProcessExists && item.SessionOpen && item.AddressTableReady && item.ClientConnection != 0 && item.ObjectManager != 0 && item.FirstObject != 0;
            Console.WriteLine(
                "Result=OK Command=\"{0}\" ProcessId={1} Ready={2} Reason=\"{3}\" Stage=\"{4}\" Detail=\"{5}\" ProcessExists={6} SessionOpen={7} AddressTableReady={8} ClientConnection=0x{9:X} ObjectManager=0x{10:X} LocalGuid=0x{11:X} TargetGuid=0x{12:X} FirstObject=0x{13:X} Scanned={14} ReadableObjects={15} FailedObjects={16} FirstFailedObject=0x{17:X} FirstFailedStage=\"{18}\" FirstFailedDetail=\"{19}\"",
                Escape(command),
                processId,
                ready,
                result.Status,
                Escape(item.Stage),
                Escape(item.Detail),
                item.ProcessExists,
                item.SessionOpen,
                item.AddressTableReady,
                item.ClientConnection,
                item.ObjectManager,
                item.LocalGuid,
                item.TargetGuid,
                item.FirstObject,
                item.Scanned,
                item.ReadableObjects,
                item.FailedObjects,
                item.FirstFailedObject,
                Escape(item.FirstFailedStage),
                Escape(item.FirstFailedDetail));
            return ready ? 0 : 1;
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
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" SnapshotUtc=\"{3:O}\" AgeMs={4} Phase={5} InWorld={6} HasPlayer={7} HasTarget={8} ObjectCount={9} PlayerCount={10} UnitCount={11} GameObjectCount={12} ItemCount={13} CorpseCount={14} Limit={15} Scanned={16} LocalGuid=0x{17:X} TargetGuid=0x{18:X} Player={19} Me={20} Target={21} NearestUnit={22} NearestGameObject={23}",
                Escape(command),
                processId,
                result.Status,
                snapshot.SnapshotUtc,
                snapshot.AgeMs,
                FormatWorldPhase(snapshot.Phase),
                snapshot.InWorld,
                snapshot.HasPlayer,
                snapshot.HasTarget,
                snapshot.ObjectCount,
                snapshot.PlayerCount,
                snapshot.UnitCount,
                snapshot.GameObjectCount,
                snapshot.ItemCount,
                snapshot.CorpseCount,
                objects == null ? 0 : objects.Limit,
                objects == null ? 0 : objects.Scanned,
                objects == null ? 0 : objects.LocalGuid,
                objects == null ? 0 : objects.TargetGuid,
                FormatPlayer(snapshot.Player),
                FormatObject(snapshot.Me),
                FormatObject(snapshot.Target),
                FormatObject(snapshot.NearestUnit),
                FormatObject(snapshot.NearestGameObject));
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

        private static int PrintNavigationCapabilityResult(string command, int processId, NavigationCapabilitySnapshot result)
        {
            if (result == null)
            {
                Console.WriteLine(
                "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"NavigationCapabilityUnavailable\" Detail=\"Navigation capability snapshot is unavailable.\" CanFindPath=False CanExecutePath=False CanFindZ=False SupportsPathQueue=False SupportsArrivalCheck=False SupportsStuckDetection=False StopResponsibility=\"\"",
                    Escape(command),
                    processId);
                return 1;
            }

            Console.WriteLine(
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"Ready\" CanFindPath={2} CanExecutePath={3} CanFindZ={4} SupportsPathQueue={5} SupportsArrivalCheck={6} SupportsStuckDetection={7} RdManagedAssemblyPresent={8} RdManagedSessionReady={9} TileProviderReady={10} StopResponsibility=\"{11}\" Detail=\"{12}\"",
                Escape(command),
                processId,
                result.CanFindPath,
                result.CanExecutePath,
                result.CanFindZ,
                result.SupportsPathQueue,
                result.SupportsArrivalCheck,
                result.SupportsStuckDetection,
                result.RdManagedAssemblyPresent,
                result.RdManagedSessionReady,
                result.TileProviderReady,
                Escape(result.StopResponsibility),
                Escape(result.Detail));
            return 0;
        }

        private static int PrintNavigationPathResult(string command, int processId, PathResult result)
        {
            if (result == null)
            {
                Console.WriteLine(
                    "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"PathResultUnavailable\" Detail=\"Navigation path result is unavailable.\" PathStatus=Unavailable PointCount=0",
                    Escape(command),
                    processId);
                return 1;
            }

            bool ready = result.Success;
            Console.WriteLine(
                "Result={0} Command=\"{1}\" ProcessId={2} Ready={3} Reason=\"{4}\" Detail=\"{5}\" PathStatus={6} PointCount={7} FirstPoint={8} LastPoint={9}",
                ready ? "OK" : "Fail",
                Escape(command),
                processId,
                ready,
                result.Status,
                Escape(result.Detail),
                result.Status,
                result.Points == null ? 0 : result.Points.Count,
                FormatPathPoint(result, true),
                FormatPathPoint(result, false));
            return ready ? 0 : 1;
        }

        private static string FormatPathPoint(PathResult result, bool first)
        {
            if (result == null || result.Points == null || result.Points.Count == 0)
            {
                return "Unavailable";
            }

            Vector3 value = first ? result.Points[0] : result.Points[result.Points.Count - 1];
            return FormatPosition(value);
        }

        private static int PrintNavigationFindZResult(string command, int processId, INavigationService navigation, int mapId, float x, float y, float hintZ)
        {
            float z;
            bool found = navigation.TryFindZ(mapId, x, y, hintZ, out z);
            Console.WriteLine(
                "Result={0} Command=\"{1}\" ProcessId={2} Ready={3} Reason=\"{4}\" MapId={5} X={6:0.###} Y={7:0.###} HintZ={8:0.###} Z={9:0.###}",
                found ? "OK" : "Fail",
                Escape(command),
                processId,
                found,
                found ? "Success" : "FindZFailed",
                mapId,
                x,
                y,
                hintZ,
                z);
            return found ? 0 : 1;
        }

        private static int PrintNavigationExecutionResult(string command, int processId, NavigationExecutionSnapshot result)
        {
            if (result == null)
            {
                Console.WriteLine(
                    "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"NavigationExecutionUnavailable\" Detail=\"Navigation execution snapshot is unavailable.\"",
                    Escape(command),
                    processId);
                return 1;
            }

            Console.WriteLine(
                "Result={0} Command=\"{1}\" ProcessId={2} Ready={3} Reason=\"{4}\" Detail=\"{5}\" PathPointCount={6} VisitedPointCount={7} Start={8} End={9} Target={10} DistanceToTarget={11:0.###} StopAttempted={12}",
                result.Success ? "OK" : "Fail",
                Escape(command),
                processId,
                result.Success,
                result.Status,
                Escape(result.Detail),
                result.PathPointCount,
                result.VisitedPointCount,
                FormatPosition(result.Start),
                FormatPosition(result.End),
                FormatPosition(result.Target),
                result.DistanceToTarget,
                result.StopAttempted);
            return result.Success ? 0 : 1;
        }

        private static int PrintClickToMoveDiagnosticResult(string command, int processId, WowRuntimeResult<ClickToMoveDiagnosticSnapshot> result)
        {
            if (!result.Success)
            {
                Console.WriteLine(
                    "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"{2}\" Detail=\"{3}\" Phase=Unknown Pos=Unavailable ClickToMoveTypeRaw=Unknown ClickToMoveState=Unknown SpeedKnown=False Speed=0",
                    Escape(command),
                    processId,
                    result.Status,
                    Escape(result.Detail));
                return 1;
            }

            Console.WriteLine(
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" Phase={3} InGame={4} LoadingOrConnecting={5} Pos=({6:0.###},{7:0.###},{8:0.###}) Rotation={9:0.###} InMovement={10} Flags={11} ClickToMoveTypeRaw={12} ClickToMoveState={13} SpeedKnown={14} Speed={15:0.###} Detail=\"{16}\"",
                Escape(command),
                processId,
                result.Status,
                result.Value.Phase.Phase,
                result.Value.Phase.InGame,
                result.Value.Phase.LoadingOrConnecting,
                result.Value.Player.Position.X,
                result.Value.Player.Position.Y,
                result.Value.Player.Position.Z,
                result.Value.Player.Position.Rotation,
                result.Value.Movement.InMovement,
                result.Value.Movement.Flags,
                result.Value.Movement.ClickToMoveTypeRaw,
                result.Value.Movement.ClickToMoveState,
                result.Value.Movement.SpeedKnown,
                result.Value.Movement.Speed,
                Escape(result.Value.Detail));
            return 0;
        }

        private static int PrintMovementSpeedSampleResult(string command, int processId, IWowRuntime runtime, string action)
        {
            WowRuntimeResult<PlayerSnapshot> before = runtime.World.GetPlayer();
            bool hasPositionSamples = before.Success;

            DateTime startedUtc = DateTime.UtcNow;
            WowRuntimeResult<MovementActionSnapshot> started = StartMovementAction(runtime, action);
            if (!started.Success)
            {
                Console.WriteLine(
                    "Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"{2}\" Detail=\"{3}\" StartPos={4} EndPos=Unavailable Distance=0 DurationMs=0 ComputedSpeed=0 StateSpeedKnown=False StateSpeed=0 Moved=False PositionSample={5}",
                    Escape(command),
                    processId,
                    started.Status,
                    Escape(started.Detail),
                    hasPositionSamples ? FormatPosition(before.Value.Position) : "Unavailable",
                    hasPositionSamples ? "Ready" : Escape(before.Detail));
                return 1;
            }

            WowRuntimeResult<PlayerSnapshot> after = null;
            WowRuntimeResult<MovementStateSnapshot> state = null;
            int durationMs = 0;
            try
            {
                Thread.Sleep(1000);
                durationMs = Math.Max(1, (int)(DateTime.UtcNow - startedUtc).TotalMilliseconds);
                after = runtime.World.GetPlayer();
                state = runtime.Movement.GetMovementState();
            }
            finally
            {
                runtime.Movement.StopMove();
            }

            hasPositionSamples = before.Success && after != null && after.Success;
            double distance = 0;
            double computedSpeed = 0;
            if (hasPositionSamples)
            {
                float dx = after.Value.Position.X - before.Value.Position.X;
                float dy = after.Value.Position.Y - before.Value.Position.Y;
                float dz = after.Value.Position.Z - before.Value.Position.Z;
                distance = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
                computedSpeed = distance / (durationMs / 1000.0);
            }

            bool stateSpeedKnown = state != null && state.Success && state.Value.SpeedKnown;
            float stateSpeed = stateSpeedKnown ? state.Value.Speed : 0;
            bool moved = hasPositionSamples && distance > 0.3 && computedSpeed > 0.3;
            string actionLabel = GetMovementActionLabel(started.Value.Action);
            string detail = moved
                ? actionLabel + " movement produced measurable displacement speed evidence."
                : !hasPositionSamples && !stateSpeedKnown
                    ? actionLabel + " movement ran, but neither position displacement nor SpeedMoving was readable."
                    : actionLabel + " movement did not produce enough displacement speed evidence.";

            Console.WriteLine(
                "Result={0} Command=\"{1}\" ProcessId={2} Ready={3} Reason=\"{4}\" Detail=\"{5}\" Action=\"{6}\" StartPos={7} EndPos={8} Distance={9:0.###} DurationMs={10} ComputedSpeed={11:0.###} StateSpeedKnown={12} StateSpeed={13:0.###} Moved={14} StartActionReason=\"{15}\" PositionSample=\"{16}\" StateSample=\"{17}\"",
                moved ? "OK" : "Fail",
                Escape(command),
                processId,
                moved,
                moved ? WowRuntimeStatus.Ready : WowRuntimeStatus.ReadFailed,
                detail,
                Escape(started.Value.Action),
                before.Success ? FormatPosition(before.Value.Position) : "Unavailable",
                after != null && after.Success ? FormatPosition(after.Value.Position) : "Unavailable",
                distance,
                durationMs,
                computedSpeed,
                stateSpeedKnown,
                stateSpeed,
                moved,
                Escape(started.Value.RuntimeReason),
                Escape(FormatSampleStatus(before, after)),
                Escape(state == null ? "Unavailable" : state.Success ? state.Value.Detail : state.Detail));
            return moved ? 0 : 1;
        }

        private static string GetMovementActionLabel(string action)
        {
            switch ((action ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "forward-start":
                    return "Forward";
                case "backward-start":
                    return "Backward";
                case "strafe-left-start":
                    return "Strafe-left";
                case "strafe-right-start":
                    return "Strafe-right";
                default:
                    return "Selected";
            }
        }

        private static WowRuntimeResult<MovementActionSnapshot> StartMovementAction(IWowRuntime runtime, string action)
        {
            string normalized = (action ?? string.Empty).Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "":
                case "forward":
                    return runtime.Movement.StartMoveForward();
                case "backward":
                    return runtime.Movement.StartMoveBackward();
                case "strafe-left":
                    return runtime.Movement.StartStrafeLeft();
                case "strafe-right":
                    return runtime.Movement.StartStrafeRight();
                default:
                    return WowRuntimeResult<MovementActionSnapshot>.Fail(
                        WowRuntimeStatus.InvalidArgument,
                        "Unsupported movement speed sample action. Use forward, backward, strafe-left, or strafe-right.");
            }
        }

        private static string FormatPosition(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.###},{1:0.###},{2:0.###})",
                value.X,
                value.Y,
                value.Z);
        }

        private static string FormatSampleStatus(WowRuntimeResult<PlayerSnapshot> before, WowRuntimeResult<PlayerSnapshot> after)
        {
            string beforeStatus = before.Success ? "Before=Ready" : "Before=" + before.Status + ":" + before.Detail;
            string afterStatus = after == null
                ? "After=Unavailable"
                : after.Success ? "After=Ready" : "After=" + after.Status + ":" + after.Detail;
            return beforeStatus + " " + afterStatus;
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

        private static bool HasArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static ulong ParseUlong(string value, ulong fallback)
        {
            if (!string.IsNullOrWhiteSpace(value) && value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                ulong parsedHex;
                return ulong.TryParse(value.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out parsedHex)
                    ? parsedHex
                    : fallback;
            }

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
