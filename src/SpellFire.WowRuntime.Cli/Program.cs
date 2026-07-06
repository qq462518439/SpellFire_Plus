using System;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Infrastructure;
using SpellFire.WowRuntime.ObjectManager;
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
            ulong guid = ParseUlong(GetArg(args, "--guid", "0"), 0);
            int entry = ParseInt(GetArg(args, "--entry", "0"), 0);
            float radius = ParseFloat(GetArg(args, "--radius", "40"), 40);
            ObjectKind? kind = ParseKind(GetArg(args, "--kind", string.Empty));

            IWowRuntime runtime = new WowRuntimeFactory().Create(processId);

            switch (command)
            {
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
                    return PrintSnapshotResult(command, processId, runtime.ObjectManager.GetObjectsByEntry(entry, limit));
                case "object-nearby":
                    return PrintSnapshotResult(command, processId, kind.HasValue
                        ? runtime.ObjectManager.GetNearbyObjectsByKind(kind.Value, new Vector3(0, 0, 0), radius, limit)
                        : runtime.ObjectManager.GetNearbyObjects(new Vector3(0, 0, 0), radius, limit));
                case "object-nearby-list":
                    return PrintSnapshotResult(command, processId, kind.HasValue
                        ? runtime.ObjectManager.GetNearbyObjectsByKind(kind.Value, new Vector3(0, 0, 0), radius, limit)
                        : runtime.ObjectManager.GetNearbyObjects(new Vector3(0, 0, 0), radius, limit), true);
                case "object-list":
                    return PrintSnapshotResult(command, processId, kind.HasValue
                        ? runtime.ObjectManager.GetObjectsByKind(kind.Value, limit)
                        : runtime.ObjectManager.GetObjects(limit), true);
                case "object-snapshot":
                    return PrintSnapshotResult(command, processId, kind.HasValue
                        ? runtime.ObjectManager.GetObjectsByKind(kind.Value, limit)
                        : runtime.ObjectManager.GetObjects(limit));
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
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" ObjectCount={3} Limit={4} Scanned={5} LocalGuid=0x{6:X} TargetGuid=0x{7:X} Me={8} Target={9}",
                Escape(command),
                processId,
                result.Status,
                result.Value.Count,
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
