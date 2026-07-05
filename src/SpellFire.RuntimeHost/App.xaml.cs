using System;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Windows;
using SpellFire.RuntimeHost.Abstractions;
using SpellFire.RuntimeHost.Components;
using SpellFire.RuntimeHost.Views;

namespace SpellFire.RuntimeHost
{
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args != null && e.Args.Length > 0)
            {
                int exitCode = RuntimeHostCommandLine.Run(e.Args);
                Shutdown(exitCode);
                return;
            }

            RuntimeHostSmokeWindow window = new RuntimeHostSmokeWindow();
            window.Show();
        }
    }

    internal static class RuntimeHostCommandLine
    {
        private static readonly string CommandLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtimehost-command.log");

        public static int Run(string[] args)
        {
            TryClearLog();
            string command = args[0] ?? string.Empty;
            int processId = 0;
            if (args.Length > 1)
            {
                int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out processId);
            }

            if (processId <= 0 && !TryFindWowProcessId(out processId))
            {
                WriteLine("FAIL " + command + " Reason=\"NoWowProcess\"");
                return 2;
            }

            IRuntimeHost host = new RuntimeHostFactory().CreateHost();
            try
            {
                if (string.Equals(command, "preflight", StringComparison.OrdinalIgnoreCase))
                {
                    return RunPreflight(host, processId);
                }

                if (string.Equals(command, "attach", StringComparison.OrdinalIgnoreCase))
                {
                    return RunAttach(host, processId);
                }

                WriteLine("FAIL unknown-command Command=\"" + command + "\"");
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

        private static int RunPreflight(IRuntimeHost host, int processId)
        {
            IRuntimeHostSession session = host.Attach(processId);
            SpellFireHookRuntimeComponent hook = GetHookComponent(host);
            RuntimeComponentStatus boundary = hook == null ? null : hook.EvaluateSafetyBoundary(processId);
            WriteLine("OK preflight " + FormatSession(session) + " | " + FormatComponent(boundary));
            return boundary != null && boundary.Ready ? 0 : 1;
        }

        private static int RunAttach(IRuntimeHost host, int processId)
        {
            host.Attach(processId);
            SpellFireHookRuntimeComponent hook = GetHookComponent(host);
            if (hook == null)
            {
                WriteLine("FAIL attach Reason=\"SpellFireHookUnavailable\"");
                return 1;
            }

            RuntimeComponentStatus result = hook.AttemptAttach(processId);
            WriteLine("OK attach " + FormatComponent(result));
            return result.Ready ? 0 : 1;
        }

        private static void WriteLine(string line)
        {
            Console.WriteLine(line);
            File.AppendAllText(CommandLogPath, line + Environment.NewLine);
        }

        private static void TryClearLog()
        {
            try
            {
                if (File.Exists(CommandLogPath))
                {
                    File.Delete(CommandLogPath);
                }
            }
            catch
            {
            }
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
