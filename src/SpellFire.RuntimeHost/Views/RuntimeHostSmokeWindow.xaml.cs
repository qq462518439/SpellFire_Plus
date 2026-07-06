using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using SpellFire.RuntimeHost.Services;

namespace SpellFire.RuntimeHost.Views
{
    public partial class RuntimeHostSmokeWindow : Window
    {
        private RuntimeHostOperationService operations;
        private int activeProcessId;

        public RuntimeHostSmokeWindow()
        {
            InitializeComponent();
            operations = new RuntimeHostOperationService();

            if (RuntimeHostProcessLocator.TryFindLatestWow(out int processId, out string label))
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
            ResetOperations();
            base.OnClosed(e);
        }

        private void BtnFindWowProcess_Click(object sender, RoutedEventArgs e)
        {
            if (!RuntimeHostProcessLocator.TryFindLatestWow(out int processId, out string label))
            {
                txtStatus.Text = "No running Wow.exe process was found.";
                AppendLog("No running Wow.exe process was found.");
                return;
            }

            ResetOperations();
            txtProcessId.Text = processId.ToString(CultureInfo.InvariantCulture);
            txtStatus.Text = "已填入 " + label;
            AppendLog("Using found process: " + label);
        }

        private void BtnClearPid_Click(object sender, RoutedEventArgs e)
        {
            ResetOperations();
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
            RunSmoke("attach", current => operations.AttachHook(current), "Attach");
        }

        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            RunSmoke("status", current => operations.GetHookStatus(current), "状态");
        }

        private void BtnLuaSmoke_Click(object sender, RoutedEventArgs e)
        {
            RunSmoke("lua-smoke", current => operations.LuaSmoke(current), "Lua冒烟");
        }

        private void BtnLuaExec_Click(object sender, RoutedEventArgs e)
        {
            RunSmoke("lua-exec", current => operations.ExecuteLua(current, txtLuaScript.Text ?? string.Empty), "执行Lua");
        }

        private void RunSmoke(string name, Func<int, RuntimeHostOperationResult> action, string actionLabel)
        {
            if (!TryGetProcessId(out int processId))
            {
                return;
            }

            EnsureProcessScope(processId);
            AppendLog("START " + name + " pid=" + processId.ToString(CultureInfo.InvariantCulture));
            try
            {
                RuntimeHostOperationResult result = action(processId);
                txtStatus.Text = DescribeStatus(actionLabel, result);
                if (string.IsNullOrWhiteSpace(txtStatus.Text) || txtStatus.Text.StartsWith("OK ", StringComparison.Ordinal) || txtStatus.Text.StartsWith("FAIL ", StringComparison.Ordinal))
                {
                    txtStatus.Text = "OK " + name;
                }
                AppendLog("OK " + name + " " + RuntimeHostOutputFormatter.FormatOperation(result));
            }
            catch (Exception ex)
            {
                txtStatus.Text = "FAIL " + name;
                AppendLog("FAIL " + name + " " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void EnsureProcessScope(int processId)
        {
            if (activeProcessId == processId && operations != null)
            {
                return;
            }

            ResetOperations();
            operations = new RuntimeHostOperationService();
            activeProcessId = processId;
        }

        private void ResetOperations()
        {
            if (operations == null)
            {
                return;
            }

            operations.Dispose();
            operations = null;
            activeProcessId = 0;
            AppendLog("Cleanup SessionDisposed=True");
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
        private static string DescribeStatus(string actionName, RuntimeHostOperationResult status)
        {
            if (status == null)
            {
                return actionName + "：结果为空。";
            }

            string reason = status.Reason ?? string.Empty;
            switch (reason)
            {
                case "HookReady":
                    return actionName + "：已完成 Hook 恢复并进入可用状态。";
                case "HookAlreadyReady":
                    return actionName + "：当前进程已处于可用状态。";
                case "LuaSmokeExecuted":
                    return actionName + "：已执行，主线程桥与 Lua 桥可用。";
                case "LuaSmokeHookUnavailable":
                    return actionName + "：当前没有可用 Hook，需先 Attach。";
                case "LuaExecuteSucceeded":
                    return actionName + "：" + DescribeLuaExecuteSucceeded(status.Detail);
                case "LuaExecuteHookUnavailable":
                    return actionName + "：当前没有可用 Hook，无法执行脚本。";
                case "LuaExecuteFailed":
                    return actionName + "：脚本执行失败。";
                case "SafeBoundary_DirtyRecoverable_ReadyMissing":
                    return actionName + "：检测到可恢复脏进程，Ready 信号缺失。";
                case "SafeBoundary_DirtyRefused_HeartbeatMissing":
                    return actionName + "：拒测，Hook 心跳缺失，进程不安全。";
                case "HookLoadedButReadySignalMissing_UnloadFailed":
                    return actionName + "：检测到旧 Hook，但卸载恢复失败。";
                case "TargetNot32Bit":
                    return actionName + "：目标不是 32 位 Wow 进程。";
                case "NoWowProcess":
                case "ProcessUnavailable":
                    return actionName + "：未找到目标进程。";
                default:
                    return actionName + "：" + reason;
            }
        }

        private static string DescribeLuaExecuteSucceeded(string detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
            {
                return "脚本已执行，Lua 通道可用。";
            }

            Match match = Regex.Match(detail, "TextPayload=([^\\s\\\"]+)");
            if (!match.Success)
            {
                return "脚本已执行，Lua 通道可用。";
            }

            return "脚本已执行，返回 " + match.Groups[1].Value + "。";
        }

        private void AppendLog(string line)
        {
            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + "] " + line + Environment.NewLine);
            txtLog.ScrollToEnd();
        }
    }
}
