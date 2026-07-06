using System;
using System.IO;
using System.Windows;
using SpellFire.RuntimeHost.Services;
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
            return RuntimeHostCommandLineRunner.Run(args ?? Array.Empty<string>(), WriteLine);
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
    }
}
