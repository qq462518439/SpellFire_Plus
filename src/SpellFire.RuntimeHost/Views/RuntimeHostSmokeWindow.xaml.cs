using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using SpellFire.RuntimeHost.Abstractions;
using SpellFire.RuntimeHost.Components;

namespace SpellFire.RuntimeHost.Views
{
    public partial class RuntimeHostSmokeWindow : Window
    {
        private IRuntimeHost host;
        private IRuntimeHostSession session;

        public RuntimeHostSmokeWindow()
        {
            InitializeComponent();
            host = new RuntimeHostFactory().CreateHost();

            if (TryFindWowProcessId(out int processId, out string label))
            {
                txtProcessId.Text = processId.ToString(CultureInfo.InvariantCulture);
                txtStatus.Text = "已自动填入 " + label;
                AppendLog("Auto-selected process: " + label);
            }
            else
            {
                txtStatus.Text = "未找到游戏进程。可点击“查找游戏进程”或手动输入 PID。";
            }

            AppendLog("Ready. RuntimeHost smoke window opened.");
        }

        protected override void OnClosed(EventArgs e)
        {
            CleanupSession();
            base.OnClosed(e);
        }

        private void BtnFindWowProcess_Click(object sender, RoutedEventArgs e)
        {
            if (!TryFindWowProcessId(out int processId, out string label))
            {
                txtStatus.Text = "No running Wow.exe process was found.";
                AppendLog("No running Wow.exe process was found.");
                return;
            }

            CleanupSession();
            txtProcessId.Text = processId.ToString(CultureInfo.InvariantCulture);
            txtStatus.Text = "已填入 " + label;
            AppendLog("Using found process: " + label);
        }

        private void BtnClearPid_Click(object sender, RoutedEventArgs e)
        {
            CleanupSession();
            txtProcessId.Clear();
            txtStatus.Text = "PID 已清空。";
            AppendLog("PID cleared.");
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }

        private void BtnCopyLog_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtLog.Text ?? string.Empty);
            txtStatus.Text = "日志已复制。";
        }

        private void BtnAttach_Click(object sender, RoutedEventArgs e)
        {
            RunSmoke("attach", current =>
            {
                EnsureSession(current);
                var concreteHook = GetHookComponent();
                if (concreteHook == null)
                {
                    return "SpellFireHook unavailable";
                }

                RuntimeComponentStatus attachResult = concreteHook.AttemptAttach(current);
                return FormatSession(session) + " | " + FormatComponent(attachResult);
            });
        }

        private void BtnHostSnapshot_Click(object sender, RoutedEventArgs e)
        {
            RunSmoke("host-snapshot", current =>
            {
                EnsureSession(current);
                return FormatSession(session);
            });
        }

        private void BtnMemoryRobot_Click(object sender, RoutedEventArgs e)
        {
            RunSmoke("memoryrobot", current =>
            {
                EnsureSession(current);
                RuntimeComponentStatus component = session.Components.FirstOrDefault(item => string.Equals(item.Name, "MemoryRobot", StringComparison.Ordinal));
                return component == null ? "MemoryRobot unavailable" : FormatComponent(component);
            });
        }

        private void BtnHook_Click(object sender, RoutedEventArgs e)
        {
            RunSmoke("spellfire-hook", current =>
            {
                EnsureSession(current);
                RuntimeComponentStatus component = session.Components.FirstOrDefault(item => string.Equals(item.Name, "SpellFireHook", StringComparison.Ordinal));
                return component == null ? "SpellFireHook unavailable" : FormatComponent(component);
            });
        }

        private void BtnLuaSmoke_Click(object sender, RoutedEventArgs e)
        {
            RunSmoke("lua-smoke", current =>
            {
                EnsureSession(current);
                var concreteHook = GetHookComponent();
                if (concreteHook == null)
                {
                    return "SpellFireHook unavailable";
                }

                RuntimeComponentStatus component = concreteHook.LuaSmoke(current);
                return FormatComponent(component);
            });
        }

        private void BtnRunAll_Click(object sender, RoutedEventArgs e)
        {
            RunSmoke("run-all", current =>
            {
                EnsureSession(current);
                StringBuilder builder = new StringBuilder();
                builder.Append(FormatSession(session));
                foreach (RuntimeComponentStatus component in session.Components)
                {
                    builder.Append(" | ").Append(FormatComponent(component));
                }

                return builder.ToString();
            });
        }

        private void RunSmoke(string name, Func<int, string> action)
        {
            if (!TryGetProcessId(out int processId))
            {
                return;
            }

            AppendLog("START " + name + " pid=" + processId.ToString(CultureInfo.InvariantCulture));
            try
            {
                string result = action(processId) ?? string.Empty;
                txtStatus.Text = "OK " + name;
                AppendLog("OK " + name + " " + result);
            }
            catch (Exception ex)
            {
                txtStatus.Text = "FAIL " + name;
                AppendLog("FAIL " + name + " " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void EnsureSession(int processId)
        {
            if (session != null && session.ProcessId == processId && session.State != RuntimeHostState.Detached)
            {
                return;
            }

            CleanupSession();
            session = host.Attach(processId);
        }

        private void CleanupSession()
        {
            if (session == null)
            {
                return;
            }

            session.Dispose();
            AppendLog("Cleanup SessionDisposed=True");
            session = null;
        }

        private bool TryGetProcessId(out int processId)
        {
            processId = 0;
            if (!int.TryParse(txtProcessId.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out processId) || processId <= 0)
            {
                txtStatus.Text = "请输入有效 PID。";
                AppendLog("Invalid PID.");
                return false;
            }

            return true;
        }

        private static bool TryFindWowProcessId(out int processId, out string label)
        {
            processId = 0;
            label = string.Empty;
            Process process = Process.GetProcessesByName("Wow")
                .OrderByDescending(item => item.StartTime)
                .FirstOrDefault();
            if (process == null)
            {
                return false;
            }

            processId = process.Id;
            label = process.ProcessName + " [PID:" + process.Id.ToString(CultureInfo.InvariantCulture) + "]";
            return true;
        }

        private static string FormatSession(IRuntimeHostSession currentSession)
        {
            return "ProcessId=" + currentSession.ProcessId.ToString(CultureInfo.InvariantCulture) +
                   " State=" + currentSession.State +
                   " Components=" + currentSession.Components.Count.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatComponent(RuntimeComponentStatus component)
        {
            return "Name=\"" + (component.Name ?? string.Empty) +
                   "\" Ready=" + component.Ready +
                   " Reason=\"" + (component.Reason ?? string.Empty) +
                   "\" Detail=\"" + (component.Detail ?? string.Empty) + "\"";
        }

        private SpellFireHookRuntimeComponent GetHookComponent()
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

        private void AppendLog(string line)
        {
            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + "] " + line + Environment.NewLine);
            txtLog.ScrollToEnd();
        }
    }
}
