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

            IWowRuntime runtime = new WowRuntimeFactory().Create(processId);

            switch (command)
            {
                case "object-me":
                    return PrintObjectResult(command, processId, runtime.ObjectManager.GetMe());
                case "object-target":
                    return PrintObjectResult(command, processId, runtime.ObjectManager.GetTarget());
                case "object-by-guid":
                    return PrintObjectResult(command, processId, runtime.ObjectManager.GetObjectByGuid(guid));
                case "object-by-entry":
                    return PrintSnapshotResult(command, processId, runtime.ObjectManager.GetObjectsByEntry(entry, limit));
                case "object-nearby":
                    return PrintSnapshotResult(command, processId, runtime.ObjectManager.GetNearbyObjects(new Vector3(0, 0, 0), radius, limit));
                case "object-snapshot":
                    return PrintSnapshotResult(command, processId, runtime.ObjectManager.GetObjects(limit));
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
                "Result=OK Command=\"{0}\" ProcessId={1} Ready=True Reason=\"{2}\" ObjectCount={3} Limit={4} Me={5} Target={6}",
                Escape(command),
                processId,
                result.Status,
                result.Value.Count,
                result.Value.Limit,
                FormatObject(result.Value.Me),
                FormatObject(result.Value.Target));
            return 0;
        }

        private static string FormatObject(WowObjectSnapshot item)
        {
            if (item == null)
            {
                return "Unavailable";
            }

            return string.Format(
                "Guid=0x{0:X} Entry={1} Name=\"{2}\" Kind={3} Pos=({4:0.###},{5:0.###},{6:0.###}) Valid={7}",
                item.Guid,
                item.Entry,
                Escape(item.Name),
                item.Kind,
                item.Position.X,
                item.Position.Y,
                item.Position.Z,
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

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
