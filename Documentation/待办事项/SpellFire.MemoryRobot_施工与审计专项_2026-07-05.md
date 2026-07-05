# SpellFire.MemoryRobot 施工与审计专项

## 当前结论

`SpellFire.MemoryRobot` 的职责必须收窄为：**对一个明确目标进程提供可审计、可释放、可诊断的底层内存操作能力**。

它不是：

1. 业务运行时
2. 多进程调度器
3. 产品/插件容器
4. 对象层、Lua 层、移动层
5. HookReady 或注入生命周期宿主
6. `Well` 或 `wManager` 的替代总线

它应该提供：

1. 打开/关闭目标进程
2. 维护进程句柄生命周期
3. 读写内存
4. 查询模块
5. 查询页面
6. 远程分配/释放/改保护
7. 明确的失败原因和诊断结果

一句话：`MemoryRobot` 只回答“这个进程能不能被安全读写、具体读写结果是什么”，不回答“当前应该操作哪个进程、挂什么产品、跑什么业务”。

## 当前代码事实

当前已经成立：

1. 独立项目：`src/SpellFire.MemoryRobot`
2. 目标框架：`net48`
3. 平台：`x86`
4. 已有基础目录：
   - `Abstractions`
   - `Native`
   - `Process`
   - `Reading`
   - `Writing`
   - `MemoryMap`
   - `Diagnostics`
   - `Assembly`
5. 已有 `MemorySession`
6. 已有 `MemorySessionFactory`
7. 已有 `MemoryRobotSessionManager`
8. 已有 `MemorySessionSnapshot`
9. 已有 `MemoryReader / MemoryWriter`
10. 已有 `MemoryReadResult / MemoryReadResult<T>`
11. 已有 `MemoryWriteResult`
12. 已有 `RemoteAllocator`
13. 已有 `RemoteThreadRunner`
14. 已有 `RemoteLibraryLoader`
15. 已有 `ProcessModuleSnapshotProvider`
16. 已有 `MemoryRegionQueryService`
17. 已有 `MemoryRobotFacade`
18. 已有 `MemoryRobotException`
19. 已有 `MemorySessionDiagnostics / MemorySessionProbeResult`
20. 已有 `ISystemLibraryResolver / SystemLibraryResolver`

当前能被验证的能力：

1. 打开真实 32 位 Wow 进程
2. 拒绝不存在的 PID
3. 识别权限拒绝
4. 识别 64 位目标不适配
5. 完成远程分配/释放前置验证
6. 支撑 `SpellFire.Hook` 的 `LoadLibraryW` 注入 smoke
7. 支撑 Hook command-ping / hook-info / read-self-module smoke

## 当前定级

`SpellFire.MemoryRobot` 当前定级为：**已有专属验收入口的底层内存组件**。

不是“业务成品”，原因：

1. 还没有目标进程退出中的竞态样本。
2. 远程线程和 LoadLibrary 已有专属验收，成功路径限制在 CLI 自进程，避免污染 Wow。
3. 还没有把 `MemoryRobot.Cli` 包装成正式发布工具。

不再使用的旧口径：

1. 不再说“多进程兼容要求”。
2. 不再说“扩展可能性要求”。
3. 不再把产品/插件未来扩展写进 MemoryRobot 第一阶段目标。
4. 不再把 RuntimeHost 的 smoke 结果当作 MemoryRobot 的全部验收。

## 正确边界

允许存在：

1. `MemorySessionManager`：只做底层 session 复用和释放。
2. `GetSessions()`：只做底层诊断快照。
3. `CloseSession(processId)`：只关闭指定进程的底层句柄。
4. `ReleaseAll()`：只作为进程退出或全局清理兜底。

不允许存在：

1. 当前目标进程选择逻辑
2. 进程列表 UI
3. 产品实例管理
4. 插件加载
5. Wow 对象模型
6. Lua 执行语义
7. 移动/导航语义

关键区别：

1. `MemoryRobot` 可以同时持有多个 session 的底层能力。
2. 但“是否要同时持有多个目标、哪个是当前目标、每个目标挂什么业务”不属于 `MemoryRobot`。

## 已完成两刀

### 生命周期第一刀

已完成：

1. `MemorySessionSnapshot`
2. `MemoryRobotSessionManager.GetSessions()`
3. `MemoryRobotSessionManager.TryGetSession(processId, out snapshot)`
4. `MemoryRobotSessionManager.CloseSession(processId)`
5. `MemoryRobotSessionManager.ReleaseAll()`
6. `IMemorySessionFactory` 暴露上述能力
7. `MemoryRobotRuntimeComponent.Cleanup(processId)` 关闭指定 PID 的 MemoryRobot session

边界：

1. `CloseSession(processId)` 不影响其它 PID。
2. `ReleaseAll()` 不作为普通业务路径使用。

### 读写结果模型第一刀

已完成：

1. `MemoryReadResult`
2. `MemoryReadResult<T>`
3. `MemoryWriteResult`
4. `IMemoryReader.TryReadBytes(address, size)`
5. `IMemoryReader.TryRead<T>(address)`
6. `IMemoryWriter.TryWriteBytes(address, buffer)`
7. `IMemoryWriter.TryWrite<T>(address, value)`

保留兼容：

1. `ReadBytes`
2. `Read<T>`
3. `ReadString`
4. `WriteBytes`
5. `Write<T>`

旧 API 继续保持异常语义；新 API 返回结果对象。

## 当前缺口

优先级从高到低：

1. 缺少 MemoryRobot 专属 CLI 或脚本，不应该长期借 `RuntimeHost` 验收。
2. 缺少 `TryRead/TryWrite` 的可重复样本。
3. 缺少 session 生命周期样本：
   - 打开后关闭
   - 重复打开同一 PID
   - 关闭后再次打开
   - 目标进程退出后快照状态
4. 缺少模块快照的专属验收。
5. 缺少页面查询的专属验收。
6. 缺少远程分配/释放/改保护的专属验收。

当前不补：

1. 对象层
2. Lua
3. Movement
4. HookReady 策略
5. 产品/插件容器
6. 业务 runtime facade

## 验收现状

已跑过：

```text
dotnet build SpellFire.MemoryRobot: OK
dotnet build SpellFire.RuntimeHost.Cli: OK
memory-probe-matrix: OK
runtimehost-smoke pid=11892 -Shutdown: OK
```

这只能证明：

1. MemoryRobot 当前构建通过。
2. MemoryRobot 的基础进程打开诊断没有破坏。
3. Hook smoke 仍能消费 MemoryRobot 的远程执行能力。

这还不能证明：

1. `TryRead/TryWrite` 已被专属样本覆盖。
2. session manager 在退出竞态下足够稳定。
3. 模块/页面/远程分配 API 已形成 MemoryRobot 自身验收闭环。

## 2026-07-05 专属 smoke 第一刀

本轮已新增 `SpellFire.MemoryRobot.Cli`，不再只借 `RuntimeHost` 验收 MemoryRobot。

新增项目：

1. `src/SpellFire.MemoryRobot.Cli/SpellFire.MemoryRobot.Cli.csproj`
2. `src/SpellFire.MemoryRobot.Cli/Program.cs`

新增脚本：

1. `tools/memoryrobot-smoke.ps1`

当前命令：

1. `probe <pid>`
2. `session-open-close <pid>`
3. `module-snapshot <pid>`
4. `memory-region <pid>`
5. `remote-alloc-free <pid>`
6. `try-read-invalid <pid>`

已验证：

```text
dotnet build SpellFire.MemoryRobot.Cli: OK
memoryrobot-smoke pid=11892: OK
```

覆盖证据：

1. `probe`：`SessionOpened`
2. `session-open-close`：`CloseResult=True` 且关闭后快照不存在
3. `module-snapshot`：能枚举模块，首模块为 `Wow.exe`
4. `memory-region`：能查询页面区域
5. `remote-alloc-free`：能远程分配并释放
6. `try-read-invalid`：失败结果能返回 `Success=False`、`BytesRead=0`、`Win32Error=299`

当前状态：

1. MemoryRobot 已有自己的最小验收入口。
2. RuntimeHost smoke 仍可作为消费方回归，但不再是 MemoryRobot 唯一证据。

## 2026-07-05 失败矩阵与安全写入第一刀

本轮继续扩展 `SpellFire.MemoryRobot.Cli` 和专属脚本。

新增 CLI 命令：

1. `probe-expect <pid> <expectedReason>`
2. `close-then-reopen <pid>`
3. `write-remote-allocation <pid>`

新增脚本：

1. `tools/memoryrobot-failure-matrix.ps1`

`memoryrobot-smoke.ps1` 已追加：

1. `close-then-reopen`
2. `write-remote-allocation`

安全写入边界：

1. 只写入 `MemoryRobot` 自己通过 `VirtualAllocEx` 分配出来的远程临时页。
2. 不写游戏真实业务地址。
3. 写入后立即读回验证，再释放临时页。

已验证：

```text
memoryrobot-smoke pid=11632: OK
memoryrobot-failure-matrix: OK
```

覆盖证据：

1. `close-then-reopen`：关闭指定 PID session 后可重新打开。
2. `write-remote-allocation`：`WriteSuccess=True`、`ReadSuccess=True`、`PayloadMatches=True`、`Freed=True`。
3. `missing-process`：`ProcessUnavailable`
4. `system-access`：`AccessDenied`
5. `explorer-bitness`：`TargetNot32Bit`

## 2026-07-05 远程执行错误路径第一刀

本轮把远程线程 / LoadLibrary 纳入 `MemoryRobot` 专属 smoke，但只验错误路径，不触碰 HookReady 业务链。

新增 CLI 命令：

1. `remote-thread-invalid-start <pid>`
2. `load-library-missing-file <pid>`

已追加到：

1. `tools/memoryrobot-smoke.ps1`

安全边界：

1. `remote-thread-invalid-start` 使用 `IntPtr.Zero`，在托管侧直接归因为 `ArgumentException`，不会创建有效远程线程。
2. `load-library-missing-file` 使用不存在的临时 DLL 路径，在本地文件检查阶段归因为 `FileNotFoundException`，不会调用远程 `LoadLibraryW`。
3. 本轮不加载任何 DLL 到 Wow。
4. 本轮不复用 `SpellFire.Hook.dll`，不碰 HookReady。

已验证：

```text
memoryrobot-smoke pid=11632: OK
memoryrobot-failure-matrix: OK
```

覆盖证据：

1. `remote-thread-invalid-start`：`Exception="ArgumentException"`
2. `load-library-missing-file`：`Exception="FileNotFoundException"`

## 2026-07-05 远程执行安全成功路径第一刀

本轮补齐远程线程 / LoadLibrary 的安全成功路径样本，但严格限制在 `SpellFire.MemoryRobot.Cli` 自身进程，不对 Wow 执行成功路径注入。

新增 MemoryRobot 接口：

1. `ISystemLibraryResolver`
2. `SystemLibraryResolver`

接口边界：

1. 只暴露 `GetModuleHandle`
2. 只暴露 `GetProcAddress`
3. 不公开 `Kernel32Native`
4. 不把 native 层变成上层可随意调用的总线

新增 CLI 命令：

1. `self-remote-thread-get-current-process-id`
2. `self-load-library-known-system-dll`

已追加到：

1. `tools/memoryrobot-smoke.ps1`

安全边界：

1. `self-remote-thread-get-current-process-id` 在 CLI 自身进程中创建远程线程，调用 `GetCurrentProcessId`，返回值必须等于 CLI 自身 PID。
2. `self-load-library-known-system-dll` 在 CLI 自身进程中加载系统 DLL：`version.dll`。
3. 不向 Wow 加载任何 DLL。
4. 不复用 `SpellFire.Hook.dll`。
5. 不触碰 HookReady payload。

已验证：

```text
memoryrobot-smoke pid=11632: OK
memoryrobot-failure-matrix: OK
```

覆盖证据：

1. `self-remote-thread-get-current-process-id`：`ExitCode` 等于 CLI 自身 `ProcessId`
2. `self-load-library-known-system-dll`：`ModuleHandle != 0`

## 2026-07-05 session 退出竞态与清理第一刀

本轮补齐 session 生命周期的专属样本，不依赖 RuntimeHost。

新增 CLI 命令：

1. `snapshot-after-close <pid>`
2. `session-close-all <pid>`
3. `process-exit-after-open`

已追加到：

1. `tools/memoryrobot-smoke.ps1`

样本含义：

1. `snapshot-after-close`：打开指定 PID，关闭指定 session 后，快照必须不存在。
2. `session-close-all`：打开指定 PID，执行全局释放后，快照必须不存在。
3. `process-exit-after-open`：启动短生命周期子进程，打开 session，等待子进程退出，再次 acquire 必须失败，并且旧 session 必须被移除。

已验证：

```text
memoryrobot-smoke pid=11632: OK
```

覆盖证据：

1. `snapshot-after-close`：`CloseResult=True`、`HasSnapshot=False`
2. `session-close-all`：`HasBefore=True`、`HasAfter=False`
3. `process-exit-after-open`：`ChildExited=True`、`HasBeforeAcquire=True`、`AcquireFailed=True`、`HasAfterAcquire=False`

## 2026-07-05 CLI 输出协议收口

本轮不新增能力，只整理 `SpellFire.MemoryRobot.Cli` 输出字段，避免审计日志混淆不同进程身份。

字段规则：

1. `TargetProcessId`：外部传入的目标进程，通常是 Wow。
2. `SelfProcessId`：`SpellFire.MemoryRobot.Cli` 自身进程。
3. `ChildProcessId`：CLI 为样本启动的短生命周期子进程。
4. `SnapshotProcessId`：`MemorySessionSnapshot` 内部记录的 PID。

保留：

1. `tools/memoryrobot-smoke.ps1` 的 `START memoryrobot-* pid=<pid>` 仍表示脚本选择的默认目标 PID。
2. CLI 正式结果行不再使用模糊的 `ProcessId` 表示所有场景。

已验证：

```text
dotnet build SpellFire.MemoryRobot.Cli: OK
memoryrobot-smoke pid=11632: OK
memoryrobot-failure-matrix: OK
```

## 下一刀

下一刀应该做：**提交前收口**。

最小命令建议：

1. 核对未跟踪文件全部纳入提交范围
2. 跑 `memoryrobot-smoke`
3. 跑 `memoryrobot-failure-matrix`
4. 检查是否仍有 RuntimeHost 文档误改
5. 分批提交 MemoryRobot 专项

## 停工线

如果某个能力不能用独立 MemoryRobot smoke 证明，就不要把它写成已完成。

如果某个能力需要知道 Wow 偏移、对象结构、Lua、移动或产品生命周期，它就不属于 `SpellFire.MemoryRobot`。
