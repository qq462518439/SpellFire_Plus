using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        private async void BtnNavigateTarget_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetProcessId(out int processId))
            {
                return;
            }

            btnNavigateTarget.IsEnabled = false;
            AppendLog("START navigation-target-smoke pid=" + processId.ToString(CultureInfo.InvariantCulture));
            try
            {
                EnsureProcessScope(processId);
                RuntimeHostOperationResult attach = operations.AttachHook(processId);
                AppendLog("DIAG navigation-target-smoke attach " + RuntimeHostOutputFormatter.FormatOperation(attach));
                if (attach == null || !attach.Ready)
                {
                    txtStatus.Text = "导航冒烟：Attach 不可用，未执行移动。";
                    AppendLog("FAIL navigation-target-smoke Reason=\"AttachNotReady\"");
                    return;
                }

                string cliPath = ResolveWowRuntimeCliPath();
                if (string.IsNullOrWhiteSpace(cliPath) || !File.Exists(cliPath))
                {
                    txtStatus.Text = "导航冒烟：WowRuntime CLI 不存在。";
                    AppendLog("FAIL navigation-target-smoke Reason=\"WowRuntimeCliMissing\" Path=\"" + cliPath + "\"");
                    return;
                }

                CliResult target = await RunWowRuntimeCliAsync(cliPath, "--command", "object-target", "--pid", processId.ToString(CultureInfo.InvariantCulture));
                AppendLog("DIAG navigation-target-smoke object-target Exit=" + target.ExitCode.ToString(CultureInfo.InvariantCulture) + " " + OneLine(target.Output));
                if (target.ExitCode != 0 || target.Output.IndexOf("Object=Guid=", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    txtStatus.Text = "导航冒烟：请先在游戏中选中目标。";
                    AppendLog("FAIL navigation-target-smoke Reason=\"TargetUnavailable\"");
                    return;
                }

                if (!TryParsePosition(target.Output, out float x, out float y, out float z))
                {
                    txtStatus.Text = "导航冒烟：目标坐标解析失败。";
                    AppendLog("FAIL navigation-target-smoke Reason=\"TargetPositionParseFailed\"");
                    return;
                }

                CliResult player = await RunWowRuntimeCliAsync(cliPath, "--command", "world-player", "--pid", processId.ToString(CultureInfo.InvariantCulture));
                AppendLog("DIAG navigation-target-smoke world-player Exit=" + player.ExitCode.ToString(CultureInfo.InvariantCulture) + " " + OneLine(player.Output));
                if (player.ExitCode != 0 || !TryParsePosition(player.Output, out float fromX, out float fromY, out float fromZ) || !TryParseIntField(player.Output, "MapId", out int mapId))
                {
                    txtStatus.Text = "导航冒烟：玩家位置或地图解析失败。";
                    AppendLog("FAIL navigation-target-smoke Reason=\"PlayerPositionOrMapParseFailed\"");
                    return;
                }

                CliResult path = await RunWowRuntimeCliAsync(
                    cliPath,
                    "--command", "navigation-find-path",
                    "--pid", processId.ToString(CultureInfo.InvariantCulture),
                    "--map", mapId.ToString(CultureInfo.InvariantCulture),
                    "--from-x", fromX.ToString("0.###", CultureInfo.InvariantCulture),
                    "--from-y", fromY.ToString("0.###", CultureInfo.InvariantCulture),
                    "--from-z", fromZ.ToString("0.###", CultureInfo.InvariantCulture),
                    "--to-x", x.ToString("0.###", CultureInfo.InvariantCulture),
                    "--to-y", y.ToString("0.###", CultureInfo.InvariantCulture),
                    "--to-z", z.ToString("0.###", CultureInfo.InvariantCulture));
                AppendLog("DIAG navigation-target-smoke path-probe Exit=" + path.ExitCode.ToString(CultureInfo.InvariantCulture) + " " + OneLine(path.Output));

                CliResult execute = await RunWowRuntimeCliAsync(
                    cliPath,
                    "--command", "navigation-execute-to",
                    "--pid", processId.ToString(CultureInfo.InvariantCulture),
                    "--x", x.ToString("0.###", CultureInfo.InvariantCulture),
                    "--y", y.ToString("0.###", CultureInfo.InvariantCulture),
                    "--z", z.ToString("0.###", CultureInfo.InvariantCulture),
                    "--arrival", "2.25",
                    "--timeout-ms", "6500",
                    "--max-points", "16");
                AppendLog((execute.ExitCode == 0 ? "OK " : "FAIL ") + "navigation-target-smoke execute Exit=" + execute.ExitCode.ToString(CultureInfo.InvariantCulture) + " " + OneLine(execute.Output));
                txtStatus.Text = execute.ExitCode == 0 ? "导航冒烟：已到达目标容差。" : "导航冒烟：执行未通过，查看日志诊断。";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "导航冒烟：异常。";
                AppendLog("FAIL navigation-target-smoke " + ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                btnNavigateTarget.IsEnabled = true;
            }
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

        private static bool TryParsePosition(string text, out float x, out float y, out float z)
        {
            x = 0;
            y = 0;
            z = 0;

            Match match = Regex.Match(text ?? string.Empty, @"Pos=\(([-0-9.]+),([-0-9.]+),([-0-9.]+)\)");
            if (!match.Success)
            {
                return false;
            }

            return float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                   float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                   float.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out z);
        }

        private static bool TryParseIntField(string text, string fieldName, out int value)
        {
            value = 0;
            Match match = Regex.Match(text ?? string.Empty, Regex.Escape(fieldName) + @"=([-0-9]+)");
            return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static string ResolveWowRuntimeCliPath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;
            string root = FindRepositoryRoot(baseDirectory);
            if (string.IsNullOrWhiteSpace(root))
            {
                return string.Empty;
            }

            return Path.Combine(root, "src", "SpellFire.WowRuntime.Cli", "bin", "Debug", "net48", "SpellFire.WowRuntime.Cli.exe");
        }

        private static string FindRepositoryRoot(string start)
        {
            DirectoryInfo current = string.IsNullOrWhiteSpace(start) ? null : new DirectoryInfo(start);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")) && Directory.Exists(Path.Combine(current.FullName, "src")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return string.Empty;
        }

        private static Task<CliResult> RunWowRuntimeCliAsync(string cliPath, params string[] args)
        {
            return Task.Run(() =>
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = cliPath,
                    WorkingDirectory = FindRepositoryRoot(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Arguments = JoinArguments(args)
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return new CliResult(process.ExitCode, (output + error).Trim());
                }
            });
        }

        private static string JoinArguments(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return string.Empty;
            }

            string[] escaped = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                escaped[i] = QuoteArgument(args[i]);
            }

            return string.Join(" ", escaped);
        }

        private static string QuoteArgument(string value)
        {
            value = value ?? string.Empty;
            if (value.Length == 0 || value.IndexOfAny(new[] { ' ', '\t', '"' }) >= 0)
            {
                return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            }

            return value;
        }

        private static string OneLine(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
        }

        private sealed class CliResult
        {
            public CliResult(int exitCode, string output)
            {
                ExitCode = exitCode;
                Output = output ?? string.Empty;
            }

            public int ExitCode { get; }

            public string Output { get; }
        }

        private void AppendLog(string line)
        {
            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + "] " + line + Environment.NewLine);
            txtLog.ScrollToEnd();
        }
    }
}
