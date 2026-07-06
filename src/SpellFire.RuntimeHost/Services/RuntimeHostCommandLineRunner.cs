using System;
using System.Globalization;
using SpellFire.RuntimeHost.Components;

namespace SpellFire.RuntimeHost.Services
{
    public static class RuntimeHostCommandLineRunner
    {
        public static int Run(string[] args, Action<string> writeLine)
        {
            if (args == null || args.Length == 0)
            {
                writeLine("FAIL unknown-command Command=\"\"");
                return 2;
            }

            string command = args[0] ?? string.Empty;
            if (string.Equals(command, "cleanup", StringComparison.OrdinalIgnoreCase))
            {
                HookPayloadCleanupResult cleanup = HookPayloadTempCleaner.CleanupInactivePayloads();
                writeLine("OK cleanup " + cleanup);
                return 0;
            }

            int processId = 0;
            if (args.Length > 1)
            {
                int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out processId);
            }

            if (processId <= 0 && !RuntimeHostProcessLocator.TryFindLatestWow(out processId, out _))
            {
                writeLine("FAIL " + command + " Reason=\"NoWowProcess\"");
                return 2;
            }

            using (RuntimeHostOperationService service = new RuntimeHostOperationService())
            {
                if (string.Equals(command, "preflight", StringComparison.OrdinalIgnoreCase))
                {
                    return WriteOperation("preflight", service.Preflight(processId), writeLine);
                }

                if (string.Equals(command, "memory-probe", StringComparison.OrdinalIgnoreCase))
                {
                    return WriteOperation("memory-probe", service.ProbeMemoryRobot(processId), writeLine);
                }

                if (string.Equals(command, "attach", StringComparison.OrdinalIgnoreCase))
                {
                    return WriteOperation("attach", service.AttachHook(processId), writeLine);
                }

                if (string.Equals(command, "status", StringComparison.OrdinalIgnoreCase))
                {
                    return WriteOperation("status", service.GetHookStatus(processId), writeLine);
                }

                if (string.Equals(command, "shutdown", StringComparison.OrdinalIgnoreCase))
                {
                    return WriteOperation("shutdown", service.ShutdownHook(processId), writeLine);
                }

                if (string.Equals(command, "command-ping", StringComparison.OrdinalIgnoreCase))
                {
                    return WriteOperation("command-ping", service.PingHook(processId), writeLine);
                }

                if (string.Equals(command, "hook-info", StringComparison.OrdinalIgnoreCase))
                {
                    return WriteOperation("hook-info", service.GetHookInfo(processId), writeLine);
                }

                if (string.Equals(command, "read-self-module", StringComparison.OrdinalIgnoreCase))
                {
                    return WriteOperation("read-self-module", service.ReadHookSelfModule(processId), writeLine);
                }

                if (string.Equals(command, "lua-smoke", StringComparison.OrdinalIgnoreCase))
                {
                    return WriteOperation("lua-smoke", service.LuaSmoke(processId), writeLine);
                }

                if (string.Equals(command, "lua-exec", StringComparison.OrdinalIgnoreCase))
                {
                    string script = args.Length > 2 ? string.Join(" ", args, 2, args.Length - 2) : string.Empty;
                    return WriteOperation("lua-exec", service.ExecuteLua(processId, script), writeLine);
                }

                if (string.Equals(command, "ctm-move", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length < 5 ||
                        !float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                        !float.TryParse(args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
                        !float.TryParse(args[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                    {
                        writeLine("FAIL ctm-move Reason=\"InvalidArguments\" Usage=\"ctm-move <pid> <x> <y> <z> [guid] [action] [precision]\"");
                        return 2;
                    }

                    ulong guid = 0;
                    int action = 4;
                    float precision = 0.5f;
                    if (args.Length > 5)
                    {
                        string guidText = args[5];
                        if (guidText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        {
                            ulong.TryParse(guidText.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out guid);
                        }
                        else
                        {
                            ulong.TryParse(guidText, NumberStyles.Integer, CultureInfo.InvariantCulture, out guid);
                        }
                    }

                    if (args.Length > 6)
                    {
                        int.TryParse(args[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out action);
                    }

                    if (args.Length > 7)
                    {
                        float.TryParse(args[7], NumberStyles.Float, CultureInfo.InvariantCulture, out precision);
                    }

                    return WriteOperation("ctm-move", service.ClickToMoveMove(processId, x, y, z, guid, action, precision), writeLine);
                }
            }

            writeLine("FAIL unknown-command Command=\"" + command + "\"");
            return 2;
        }

        private static int WriteOperation(string command, RuntimeHostOperationResult operation, Action<string> writeLine)
        {
            writeLine("OK " + command + " " + RuntimeHostOutputFormatter.FormatOperation(operation));
            return operation != null && operation.Ready ? 0 : 1;
        }
    }
}
