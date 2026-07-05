using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using SpellFire.RuntimeHost.Abstractions;
using SpellFire.RuntimeHost.Components;

namespace SpellFire.RuntimeHost.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string command = args.Length > 0 ? args[0] : "attach";
            if (string.Equals(command, "cleanup", StringComparison.OrdinalIgnoreCase))
            {
                return RunCleanup();
            }

            int processId = 0;
            if (args.Length > 1)
            {
                int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out processId);
            }

            if (processId <= 0 && !TryFindWowProcessId(out processId))
            {
                Console.WriteLine("FAIL " + command + " Reason=\"NoWowProcess\"");
                return 2;
            }

            IRuntimeHost host = new RuntimeHostFactory().CreateHost();
            try
            {
                if (string.Equals(command, "preflight", StringComparison.OrdinalIgnoreCase))
                {
                    return RunPreflight(host, processId);
                }

                if (string.Equals(command, "memory-probe", StringComparison.OrdinalIgnoreCase))
                {
                    return RunMemoryProbe(host, processId);
                }

                if (string.Equals(command, "attach", StringComparison.OrdinalIgnoreCase))
                {
                    return RunAttach(host, processId);
                }

                if (string.Equals(command, "status", StringComparison.OrdinalIgnoreCase))
                {
                    return RunStatus(host, processId);
                }

                if (string.Equals(command, "shutdown", StringComparison.OrdinalIgnoreCase))
                {
                    return RunShutdown(host, processId);
                }

                if (string.Equals(command, "command-ping", StringComparison.OrdinalIgnoreCase))
                {
                    return RunCommandPing(host, processId);
                }

                if (string.Equals(command, "hook-info", StringComparison.OrdinalIgnoreCase))
                {
                    return RunHookInfo(host, processId);
                }

                if (string.Equals(command, "read-self-module", StringComparison.OrdinalIgnoreCase))
                {
                    return RunReadSelfModule(host, processId);
                }

                if (string.Equals(command, "lua-smoke", StringComparison.OrdinalIgnoreCase))
                {
                    return RunLuaSmoke(host, processId);
                }

                Console.WriteLine("FAIL unknown-command Command=\"" + command + "\"");
                return 2;
            }
            finally
            {
                foreach (IRuntimeHostSession session in host.GetSessions())
                {
                    session.Dispose();
                }
            }
        }

        private static int RunCleanup()
        {
            HookPayloadCleanupResult result = HookPayloadTempCleaner.CleanupInactivePayloads();
            Console.WriteLine("OK cleanup " + result);
            return 0;
        }

        private static int RunPreflight(IRuntimeHost host, int processId)
        {
            IRuntimeHostSession session = host.Attach(processId);
            SpellFireHookRuntimeComponent hook = GetHookComponent(host);
            RuntimeComponentStatus boundary = hook == null ? null : hook.EvaluateSafetyBoundary(processId);
            Console.WriteLine("OK preflight " + FormatSession(session) + " | " + FormatComponent(boundary));
            return boundary != null && boundary.Ready ? 0 : 1;
        }

        private static int RunMemoryProbe(IRuntimeHost host, int processId)
        {
            IRuntimeHostSession session = host.Attach(processId);
            RuntimeComponentStatus memory = session.Components.FirstOrDefault(item => string.Equals(item.Name, "MemoryRobot", StringComparison.Ordinal));
            Console.WriteLine("OK memory-probe " + FormatSession(session) + " | " + FormatComponent(memory));
            return memory != null && memory.Ready ? 0 : 1;
        }

        private static int RunAttach(IRuntimeHost host, int processId)
        {
            host.Attach(processId);
            SpellFireHookRuntimeComponent hook = GetHookComponent(host);
            if (hook == null)
            {
                Console.WriteLine("FAIL attach Reason=\"SpellFireHookUnavailable\"");
                return 1;
            }

            RuntimeComponentStatus result = hook.AttemptAttach(processId);
            Console.WriteLine("OK attach " + FormatComponent(result));
            return result.Ready ? 0 : 1;
        }

        private static int RunStatus(IRuntimeHost host, int processId)
        {
            SpellFireHookRuntimeComponent hook = GetHookComponent(host);
            if (hook == null)
            {
                Console.WriteLine("FAIL status Reason=\"SpellFireHookUnavailable\"");
                return 1;
            }

            RuntimeComponentStatus result = hook.GetStatus(processId);
            Console.WriteLine("OK status " + FormatComponent(result));
            return result.Ready ? 0 : 1;
        }

        private static int RunShutdown(IRuntimeHost host, int processId)
        {
            SpellFireHookRuntimeComponent hook = GetHookComponent(host);
            if (hook == null)
            {
                Console.WriteLine("FAIL shutdown Reason=\"SpellFireHookUnavailable\"");
                return 1;
            }

            RuntimeComponentStatus result = hook.RequestShutdown(processId);
            Console.WriteLine("OK shutdown " + FormatComponent(result));
            return result.Ready ? 0 : 1;
        }

        private static int RunCommandPing(IRuntimeHost host, int processId)
        {
            SpellFireHookRuntimeComponent hook = GetHookComponent(host);
            if (hook == null)
            {
                Console.WriteLine("FAIL command-ping Reason=\"SpellFireHookUnavailable\"");
                return 1;
            }

            RuntimeComponentStatus result = hook.CommandPing(processId);
            Console.WriteLine("OK command-ping " + FormatComponent(result));
            return result.Ready ? 0 : 1;
        }

        private static int RunHookInfo(IRuntimeHost host, int processId)
        {
            SpellFireHookRuntimeComponent hook = GetHookComponent(host);
            if (hook == null)
            {
                Console.WriteLine("FAIL hook-info Reason=\"SpellFireHookUnavailable\"");
                return 1;
            }

            RuntimeComponentStatus result = hook.GetHookInfo(processId);
            Console.WriteLine("OK hook-info " + FormatComponent(result));
            return result.Ready ? 0 : 1;
        }

        private static int RunReadSelfModule(IRuntimeHost host, int processId)
        {
            SpellFireHookRuntimeComponent hook = GetHookComponent(host);
            if (hook == null)
            {
                Console.WriteLine("FAIL read-self-module Reason=\"SpellFireHookUnavailable\"");
                return 1;
            }

            RuntimeComponentStatus result = hook.ReadSelfModule(processId);
            Console.WriteLine("OK read-self-module " + FormatComponent(result));
            return result.Ready ? 0 : 1;
        }

        private static int RunLuaSmoke(IRuntimeHost host, int processId)
        {
            SpellFireHookRuntimeComponent hook = GetHookComponent(host);
            if (hook == null)
            {
                Console.WriteLine("FAIL lua-smoke Reason=\"SpellFireHookUnavailable\"");
                return 1;
            }

            RuntimeComponentStatus result = hook.LuaSmoke(processId);
            Console.WriteLine("OK lua-smoke " + FormatComponent(result));
            return result.Ready ? 0 : 1;
        }

        private static SpellFireHookRuntimeComponent GetHookComponent(IRuntimeHost host)
        {
            RuntimeHost concreteHost = host as RuntimeHost;
            if (concreteHost == null)
            {
                return null;
            }

            foreach (IRuntimeComponent component in concreteHost.Components)
            {
                SpellFireHookRuntimeComponent hook = component as SpellFireHookRuntimeComponent;
                if (hook != null)
                {
                    return hook;
                }
            }

            return null;
        }

        private static bool TryFindWowProcessId(out int processId)
        {
            Process process = Process.GetProcessesByName("Wow")
                .OrderByDescending(item => item.StartTime)
                .FirstOrDefault();
            processId = process == null ? 0 : process.Id;
            return process != null;
        }

        private static string FormatSession(IRuntimeHostSession session)
        {
            return "ProcessId=" + session.ProcessId.ToString(CultureInfo.InvariantCulture) +
                   " State=" + session.State +
                   " Components=" + session.Components.Count.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatComponent(RuntimeComponentStatus component)
        {
            if (component == null)
            {
                return "Component=null";
            }

            return "Name=\"" + (component.Name ?? string.Empty) +
                   "\" Ready=" + component.Ready +
                   " Reason=\"" + (component.Reason ?? string.Empty) +
                   "\" Detail=\"" + (component.Detail ?? string.Empty) + "\"";
        }
    }
}
