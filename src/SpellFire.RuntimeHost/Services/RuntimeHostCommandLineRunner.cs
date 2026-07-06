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
