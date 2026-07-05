# SpellFire.RuntimeHost 施工与审计专项

## 当前结论

`SpellFire.RuntimeHost` 当前不定义为正式宿主层，也不定义为业务运行时成品。

当前主线收缩为：

1. `RuntimeHost` 是测试壳 / smoke 壳。
2. 当前默认被测组件只有：
   - `MemoryRobotRuntimeComponent`
   - `SpellFireHookRuntimeComponent`
3. `RuntimeHost.Cli + tools/runtimehost-smoke.ps1` 是当前唯一可按“成品”使用的自动验收工具链。
4. `RuntimeHost` 退出时必须自动 cleanup。

当前不能再做的事：

1. 重新接回 `WRobotComponentAdapter`
2. 默认拼接主仓运行目录
3. 默认拼接官方 WRobot 目录
4. 用第三方资产把宿主状态伪装成“已就绪”

一句话：

`RuntimeHost` 当前专项不是“接第三方运行时”，也不是“正式多进程宿主”，而是“把 SpellFire 自己的运行时地基做成可重复验收的 smoke 工具链”。

## 阶段收口审计结论

截至本轮审计，当前状态定级如下：

1. `SpellFire.MemoryRobot`：**成型地基**
2. `SpellFire.Hook`：**成型地基**
3. `SpellFire.RuntimeHost`：**测试壳 / smoke 壳**
4. `RuntimeHost.Cli + tools/runtimehost-smoke.ps1`：**当前可信成品**

不能把 `MemoryRobot / Hook / RuntimeHost` 单独升级为“业务成品”的原因：

1. 还没有远程卸载，当前 payload 仍依赖目标进程生命周期自然释放
2. `SpellFire.Hook` 只证明 ready、heartbeat、shutdown、command-ping、hook-info、read-self-module
3. 还没有对象、Lua、移动、战斗、导航语义层消费
4. `RuntimeHost` 当前只承担 smoke 壳职责，不应向 `WowRuntime` 输出正式宿主上下文
5. 当前命令通道仍是固定共享内存槽，不是完整 RPC/消息框架

可以认定为“可信成品”的部分：

1. 自动验收脚本可稳定构建、注入、重复 attach、检查心跳、执行命令、请求 shutdown、清理临时 payload
2. CLI 验收不依赖 WPF 页面，不需要人工点按钮
3. 测试后 Wow 进程仍响应，已有真实进程证据

因此本专项当前的正确收口口径是：

`MemoryRobot/Hook 是可继续承载后续 runtime 能力的成型地基；RuntimeHost 只是测试壳；当前唯一成品是 RuntimeHost.Cli smoke 自动验收工具链。`

## 当前代码真实状态

当前已经成立的代码事实：

1. `SpellFire.RuntimeHost` 已经重写为独立可执行项目
2. `RuntimeHost`、`RuntimeHostSession`、`RuntimeHostFactory` 已重建
3. `IRuntimeComponent` 合同已重建
4. `MemoryRobotRuntimeComponent` 已接通
5. `SpellFireHookRuntimeComponent` 已从空占位升级到“前置条件探测器”
6. `RuntimeHost` 可直接执行最小冒烟测试
7. `RuntimeHost` 退出时会自动 `Dispose` 测试会话并调用组件 `Cleanup`
8. `SpellFireHookRuntimeComponent` 已不再依赖 `Well.dll / EasyHook` 旧资产检查，而改为基于 `SpellFire.MemoryRobot` 的 hook 地基检查
9. `SpellFire.MemoryRobot` 已补出最小远程执行地基：
   - `IRemoteThreadRunner`
   - `IRemoteLibraryLoader`
   - `CreateRemoteThread + WaitForSingleObject + GetExitCodeThread`
   - `LoadLibraryW` 注入闭环
10. `RuntimeHost` 的 smoke attach 已开始消费这套能力
11. `SpellFire.Hook.dll` 原生 Win32 payload 已建立
12. `RuntimeHost` 构建后会把 `SpellFire.Hook.dll` 投放到自身输出目录
13. payload 被加载时会设置 `Local\SpellFireHookReady_{pid}` ready 事件
14. `RuntimeHost` attach 会等待 ready 事件，而不是把 `LoadLibraryW` 返回值直接写成 hook ready
15. `SpellFire.RuntimeHost.Cli` 已建立，自动化脚本不再依赖 WPF 页面
16. `tools/runtimehost-smoke.ps1` 已能自动构建并对当前 Wow 进程执行：
   - preflight
   - attach
   - `HookReady` 判定
17. 已在真实 Wow 进程上得到 `HookReady`
18. payload 投放已改成 source/temp 双层：
   - 构建输出目录保存 `SpellFire.Hook.source.dll`
   - attach 前复制到 `%TEMP%\SpellFireHookPayloads\{pid}\{guid}\SpellFire.Hook.dll`
   - WoW 锁住的是临时副本，不再锁构建输出
19. 自动化脚本已覆盖 repeat attach，并已在新进程上验证：
   - 第一次 attach 返回 `HookReady`
   - 第二次 attach 返回 `HookAlreadyReady`
20. `HookPayloadTempCleaner` 已建立：
   - 自动清理已退出进程的临时 payload 目录
   - 跳过当前仍活跃的进程目录
   - 不做远程强卸载
21. `tools/runtimehost-smoke.ps1` 已接入前后 cleanup
22. `SpellFire.Hook` 已进入最小服务化：
   - worker thread
   - heartbeat event
   - shutdown event
23. `SpellFire.RuntimeHost.Cli` 已支持：
   - `status`
   - `shutdown`
24. 自动脚本已验证：
   - `HookServiceAlive`
   - `HookShutdownRequested`
25. `SpellFire.Hook` 已建立最小命令通道：
   - `Local\SpellFireHookCommand_{pid}`
   - `Local\SpellFireHookAck_{pid}`
   - `Local\SpellFireHookCommandBuffer_{pid}`
26. `RuntimeHost.Cli` 已支持 `command-ping`
27. 自动脚本已验证宿主到 Hook 的一次往返：
   - `HookCommandPingOk`
   - `Magic=0x53464850`
   - `Version=1`
   - `HeaderSize=72`
   - `Status=0x53464F4B`
   - `Result=0x50494E47`
28. `RuntimeHost.Cli` 已支持 `hook-info`
29. `GetHookInfo` 已作为第一条非 ping 安全命令通过真实 Wow 验收：
   - `HookInfoOk`
   - `HookProcessId=<pid>`
   - `HookProtocolVersion=1`
   - `HeartbeatCount>=1`
30. 宿主侧命令协议已抽离到 `HookProtocol`：
   - 协议常量集中
   - 命令号集中
   - 状态码/结果码集中
   - buffer offset 集中
31. `HookCommandChannel` 已收敛为通道职责：打开 mapping、发命令、读结果
32. 第一条最小内存读命令已完成：`read-self-module`
33. `read-self-module` 只读取 `SpellFire.Hook` 自己模块的 PE 头，不读取 Wow 对象/Lua/移动内存
34. 自动脚本已验证：
   - `HookSelfModuleReadOk`
   - `DosSignature=0x5A4D`
   - `PeSignature=0x4550`
   - `Machine=0x14C`
   - `SectionCount>0`
35. Hook 侧协议常量已抽离到 `HookProtocol.h`
36. `dllmain.cpp` 不再本地散落协议常量、命令号、状态码、结果码和 `CommandBuffer` 定义

当前还没有完成的事实：

1. 还没有远程卸载
2. payload 当前只做最小 ready/heartbeat/shutdown/command-ping/hook-info/read-self-module，不含对象/Lua/移动业务
3. 还没有向 `WowRuntime` 提供语义层服务
4. 尚未实现远程卸载/退出回收；当前策略是让 payload 随目标进程生命周期结束

## 2026-07-05 接力验收记录

本轮接力先不补 `cache` 命令，优先验证现有矩阵和 smoke 入口。

已确认：

1. `tools/memory-probe-matrix.ps1` 通过。
2. `missing-process` 返回 `ProcessUnavailable`。
3. `system-access` 返回 `AccessDenied`。
4. `explorer-bitness` 返回 `TargetNot32Bit`。
5. 托管项目可构建：
   - `SpellFire.MemoryRobot`
   - `SpellFire.RuntimeHost`
   - `SpellFire.Runtime`
   - `SpellFire.RuntimeHost.Cli`
6. `SpellFire.RuntimeHost.Cli cleanup` 可执行并返回 OK。
7. 无效 PID 下的 `preflight/status/command-ping` 均能返回可解释失败，没有卡死或异常崩溃。

完整 `runtimehost-smoke` 尚未进入 attach 验收，当前阻塞点是本机 native C++ 构建环境：

```text
FAIL runtimehost-smoke Reason="CppTargetsMissing"
```

原因：

1. 原脚本硬编码 `Visual Studio 2026 vcvars32.bat`，本机不存在。
2. 本机可发现 VS18 MSBuild。
3. 本机未发现 `Microsoft.Cpp.Default.props`。
4. 当前没有现成 `SpellFire.Hook.dll` 产物，不能使用 `-SkipBuild` 跳过 native 构建。

已修正：

1. `tools/runtimehost-smoke.ps1` 不再硬编码 VS2026。
2. 脚本会自动发现 MSBuild。
3. 缺失 C++ targets 时输出 `CppTargetsMissing` 明确诊断。

当前结论：

```text
session manager 没有破坏 memory probe 矩阵和非注入 CLI 路径；
完整 attach/repeat/status/command/hook-info/read-self-module 仍需 C++ workload 或现成 Hook DLL 后再验收。
```

## 2026-07-05 接力继续验收记录

已继续处理 native Hook 构建阻塞。

新增确认：

1. 本机存在 VS2019 C++ 工具链：
   - `VC\Tools\MSVC\14.29.30133`
   - `MSBuild\Microsoft\VC\v160\Microsoft.Cpp.Default.props`
2. `SpellFire.Hook.vcxproj` 原配置使用 `PlatformToolset=v145`，本机不存在该工具集。
3. 已将 `SpellFire.Hook.vcxproj` 的 Debug/Release `PlatformToolset` 调整为 `v142`。
4. `tools/runtimehost-smoke.ps1` 已改为优先选择带 C++ targets 的 MSBuild 实例。

完整 smoke 已通过：

```text
OK runtimehost-smoke pid=5192
```

覆盖阶段：

1. native `SpellFire.Hook.dll` 构建成功。
2. `SpellFire.RuntimeHost` 构建成功。
3. `SpellFire.RuntimeHost.Cli` 构建成功。
4. `memory-probe` 返回 `SessionOpened`。
5. `preflight` 返回 `MemoryReadyForHook`。
6. 首次 `attach` 返回 `HookReady`。
7. 重复 `attach` 返回 `HookAlreadyReady`。
8. `status` 返回 `HookServiceAlive`。
9. `command-ping` 返回 `HookCommandPingOk`。
10. `hook-info` 返回 `HookInfoOk`。
11. `read-self-module` 返回 `HookSelfModuleReadOk`。
12. `read-self-module` 验证：
    - `DosSignature=0x5A4D`
    - `PeSignature=0x4550`
    - `Machine=0x14C`
    - `SectionCount=9`

Shutdown smoke 已通过：

```text
OK runtimehost-smoke pid=5192 -Shutdown
```

Shutdown 验收点：

1. `shutdown` 返回 `HookShutdownRequested`。
2. shutdown 后 `PostStatusReason=HookServiceUnavailable`。
3. shutdown 后 `ReadySignal=False`。
4. shutdown 后 `HeartbeatSignal=False`。

本轮更新后的结论：

```text
session manager 未破坏现有矩阵和完整 smoke；
MemoryRobot/Hook 当前恢复到可构建、可 attach、可重复 attach、可命令往返、可 shutdown 的成型地基状态；
RuntimeHost 仍只按 smoke 测试壳使用。
```

## 当前边界

本专项当前只做：
1. 单目标 PID smoke 验证
2. 组件状态探测
3. 组件状态汇总
4. 测试会话退出清理
5. 冒烟命令行入口
本专项当前不做：

1. 第三方 runtime 适配
2. 运行目录混合搜索器
3. 对象/Lua/移动语义层
4. 主程序接线
5. 插件容器
6. 产品容器

## 宿主口径

`RuntimeHost` 只认 `IRuntimeComponent`。

一个组件在当前专项里只需要回答三件事：

1. 你叫什么
2. 你对某个进程是否 ready
3. 你退出时如何 cleanup

也就是说，后续任何新能力都要先做成组件，再挂到宿主：

1. `SpellFireHook`
2. `SpellFireLuaBridge`
3. `SpellFireObjectRuntime`
4. `SpellFireGateProbe`

禁止绕过组件合同，直接把逻辑塞进 `RuntimeHost` 本体。

## 当前默认组件

### `MemoryRobotRuntimeComponent`

职责：

1. 验证目标进程是否可打开
2. 给宿主提供最基础的进程可用性状态

当前输出口径：

1. `SessionOpened`
2. `SessionClosed`
3. `ProcessUnavailable`
4. `TargetBitnessUnknown`
5. `TargetNot32Bit`
6. `AccessDenied`
7. `InvalidParameter`
8. `OpenProcessFailed`
9. 诊断 detail 包含：
   - `ProcessFound`
   - `ProcessName`
   - `Responding`
   - `HostX64OS`
   - `HostX64Process`
   - `TargetWow64Known`
   - `TargetWow64`
   - `RequestedAccess`
   - `Win32Error`
   - `Win32Message`

它现在属于：

1. **成型地基**

定级依据：

1. 已能打开目标进程并建立 `MemorySession`
2. 已支撑远程分配、远程线程、`LoadLibraryW` 注入闭环
3. 已在真实 Wow 进程上支撑 Hook 注入验收

未升级为“可信成品”的原因：

1. 还没有 session 复用策略
2. 还没有把读写 API 扩成稳定公开面
3. 还没有长期运行压力证据

### `SpellFireHookRuntimeComponent`

职责：

1. 验证目标进程是否存在
2. 验证目标进程是否可响应
3. 探测目标位数环境信息
4. 验证 `SpellFire.MemoryRobot` 是否能打开进程
5. 验证后续 hook 所需的最小远程内存能力是否成立

当前地基检查：

1. 目标是否可识别为 32 位 WoW 进程
2. `MemorySession` 是否可打开
3. 进程句柄是否有效
4. `VirtualAllocEx` / `VirtualFreeEx` 是否可完成最小闭环

当前输出口径：

1. `ProcessUnavailable`
2. `TargetBitnessUnknown`
3. `TargetNot32Bit`
4. `MemorySessionOpenFailed`
5. `RemoteAllocationFailed`
6. `MemoryReadyForHook`
7. `HookPayloadMissing`
8. `AttachAttemptFailed`
9. `HookReady`
10. `AttachAttempted_LoadLibraryReturnedZero`
11. `AttachAttempted_ReadySignalTimeout`
12. `HookLoadedButReadySignalMissing`
13. `HookAlreadyReady`
14. `HookServiceAlive`
15. `HookServiceUnavailable`
16. `HookShutdownRequested`
17. `HookCommandPingOk`
18. `HookCommandPingFailed`
19. `HookCommandUnavailable`
20. `HookCommandFailed`
21. `HookInfoOk`
22. `HookInfoFailed`
23. `HookInfoUnavailable`
24. `HookSelfModuleReadOk`
25. `HookSelfModuleReadFailed`
26. `HookSelfModuleUnavailable`

它现在属于：

1. **成型地基**

定级依据：

1. 已能通过 `LoadLibraryW` 注入自家原生 payload
2. 已能用 ready event 区分“注入调用返回”和“Hook 真 ready”
3. 已有 heartbeat/shutdown
4. 已有命令通道和协议头
5. 已完成 `command-ping`、`hook-info`、`read-self-module`
6. 已在真实 Wow 进程上完成自动 smoke

未升级为“可信成品”的原因：

1. 还没有远程卸载
2. 还没有完整命令调度框架
3. 还没有业务语义层命令
4. 还没有长期驻留与异常恢复策略
5. 还没有并发/重复 attach 压力验收

注意：

1. 它还不是注入成品
2. 它当前只做 attach 前置条件探测
3. 只有收到 ready 事件才能写成 `HookReady`
4. 不允许再把 `Well/EasyHook` 旧资产检查冒充成 hook ready
5. 当前 payload 只证明“自家 DLL 可被注入并执行 DllMain”，还不证明对象/Lua/移动能力

## 分阶段施工计划

下面是这个专项的完整阶段，不再想到哪做到哪。

### 阶段 0：宿主骨架归零重建

目标：

1. 删掉旧的第三方 runtime 伪入口
2. 重建最小宿主骨架

已完成：

1. `RuntimeHost` 重写
2. `RuntimeHostSession` 重写
3. `RuntimeHostFactory` 重写
4. 冒烟入口并回 `SpellFire.RuntimeHost`

验收：

1. `RuntimeHost` 可独立构建
2. 能执行 `SpellFire.RuntimeHost <pid>`
3. 退出时会打印 cleanup 结果

停工条件：

1. 宿主已不再依赖第三方 runtime 假入口

### 阶段 1：组件状态探测地基

目标：

1. 让宿主能看见组件状态
2. 区分“哪个组件 ready，哪个组件没 ready”

已完成：

1. `MemoryRobotRuntimeComponent`
2. `SpellFireHookRuntimeComponent`
3. `RuntimeComponentStatus`

验收：

1. 冒烟输出包含组件列表
2. 每个组件都有 `Ready/Reason`
3. `MemoryRobot` 与 `SpellFireHook` 状态可区分

停工条件：

1. 宿主已能稳定输出组件状态

### 阶段 2：Hook 资产布局阶段

目标：

1. 先确立自家 hook 的最低地基，而不是继续依赖旧注入资产
2. 让 `SpellFireHookRuntimeComponent` 能回答“当前进程是否具备实装 hook 的最低条件”

本阶段要做：

1. 明确 `MemoryRobot` 打开目标进程的口径
2. 明确 32 位目标识别口径
3. 明确最小远程分配闭环是否成立
4. 让探测从“旧资产缺件”收口成“自家 hook 地基是否成立”

本阶段不做：

1. 真注入
2. IPC 握手
3. 语义层接线

验收：

1. `SpellFireHook` 不再输出 `Well/EasyHook` 缺件口径
2. 输出稳定落在：
   - `TargetBitnessUnknown`
   - `TargetNot32Bit`
   - `MemorySessionOpenFailed`
   - `RemoteAllocationFailed`
   - `MemoryReadyForHook`

停工条件：

1. hook 地基已稳定
2. 再讨论旧资产目录已没有价值

### 阶段 3：最小 Attach 探测阶段

目标：

1. 在不把主程序接进来的前提下
2. 验证自家 hook 路径能否完成一次最小 attach 尝试

本阶段要做：

1. 在 `MemoryReadyForHook` 的前提下封装自家 hook attach 入口
2. 接 `SpellFire.MemoryRobot` 的 `LoadLibraryW` 注入能力
3. 接 `SpellFire.Hook.dll` ready 事件
4. 区分：
   - 前置条件不满足
   - 注入调用失败
   - 注入后等待失败
   - ready 事件超时
   - hook ready

本阶段不做：

1. 对象层
2. Lua 层
3. 运动层
4. 导航层

验收：

1. `SpellFireHook` 的状态能从“地基 ready”推进到“attach 尝试结果”
2. 错误原因能明确打印
3. 真实 WoW 进程上至少出现一次 `HookReady`

当前验收证据：

```text
OK cleanup RemovedProcessDirs=1 RemovedPayloadDirs=1 SkippedActiveProcessDirs=0 SkippedLockedDirs=0
OK preflight ProcessId=34124 State=Ready Components=2 | Name="SpellFireHook" Ready=True Reason="MemoryReadyForHook"
OK attach Name="SpellFireHook" Ready=True Reason="HookReady" ... Payload=C:\Users\ASUS\AppData\Local\Temp\SpellFireHookPayloads\34124\...\SpellFire.Hook.dll LoadLibraryExit=0x561E0000 ReadySignal=True
OK attach Name="SpellFireHook" Ready=True Reason="HookAlreadyReady" ... ExistingModule=0x561E0000 ReadySignal=True
OK status Name="SpellFireHook" Ready=True Reason="HookServiceAlive" ... HeartbeatSignal=True
OK shutdown Name="SpellFireHook" Ready=True Reason="HookShutdownRequested" ... PostStatusReason=HookServiceUnavailable
OK cleanup RemovedProcessDirs=0 RemovedPayloadDirs=0 SkippedActiveProcessDirs=1 SkippedLockedDirs=0
OK runtimehost-smoke pid=34124
```

停工条件：

1. attach 成败已能被清楚归因

### 阶段 4：Hook Ready 判定阶段

目标：

1. 把“注入成功”与“运行时 ready”区分开

本阶段要做：

1. 引入自家 hook ready 判定
2. 至少能回答：
   - 是否已注入
   - IPC 是否在线
   - 最低限度远端接口是否可 ping

本阶段不做：

1. Wow 语义层消费

验收：

1. `SpellFireHook` 状态出现 `Ready/NotReady` 的明确边界
2. `RuntimeHost.Cli command-ping <pid>` 能完成一次宿主到 Hook 的命令往返

当前验收证据：

```text
OK preflight ProcessId=14340 State=Ready Components=2 | Name="SpellFireHook" Ready=True Reason="MemoryReadyForHook"
OK attach Name="SpellFireHook" Ready=True Reason="HookReady" ... ReadySignal=True
OK attach Name="SpellFireHook" Ready=True Reason="HookAlreadyReady" ... ReadySignal=True
OK status Name="SpellFireHook" Ready=True Reason="HookServiceAlive" ... HeartbeatSignal=True
OK command-ping Name="SpellFireHook" Ready=True Reason="HookCommandPingOk" Detail=" StatusReason=HookServiceAlive StatusReady=True Ack=True Magic=0x53464850 Version=1 HeaderSize=72 Status=0x53464F4B Result=0x50494E47 PayloadLength=0 PingCount=1"
OK hook-info Name="SpellFireHook" Ready=True Reason="HookInfoOk" Detail=" StatusReason=HookServiceAlive StatusReady=True Ack=True Magic=0x53464850 Version=1 HeaderSize=72 Status=0x53464F4B Result=0x494E464F PayloadLength=16 PingCount=1 HookProcessId=17404 HookProtocolVersion=1 HookStartTick=32370750 HeartbeatCount=2"
OK read-self-module Name="SpellFireHook" Ready=True Reason="HookSelfModuleReadOk" Detail=" StatusReason=HookServiceAlive StatusReady=True Ack=True Magic=0x53464850 Version=1 HeaderSize=72 Status=0x53464F4B Result=0x50455244 PayloadLength=20 PingCount=1 ModuleBaseLow=0x5A740000 DosSignature=0x5A4D PeSignature=0x4550 Machine=0x14C SectionCount=9"
OK shutdown Name="SpellFireHook" Ready=True Reason="HookShutdownRequested" ... PostStatusReason=HookServiceUnavailable
OK runtimehost-smoke pid=17404
```

停工条件：

1. hook ready 能被单独判断

### 阶段 5：宿主清理与回收阶段

目标：

1. 让 `RuntimeHost` 对退出、失败、重复 attach 的行为变稳定

本阶段要做：

1. cleanup 明确化
2. 重复 attach 口径明确化
3. 失败后残留状态清理

验收：

1. 退出时不再只打印 `SessionDisposed=True`
2. 而是能反映组件清理是否完成

停工条件：

1. 宿主生命周期稳定

### 阶段 6：向 WowRuntime 供给阶段

目标：

1. 让 `RuntimeHost` 的 hook 结果能被后续 `WowRuntime` 消费

本阶段要做：

1. 暴露稳定的宿主上下文
2. 让 `WowRuntime` 以后不再直接碰裸 hook 细节

本阶段不在当前主线里立刻开工，但必须作为后续阶段写清。

## 当前阶段判定

截至本次文档更新：

1. 阶段 0 已完成
2. 阶段 1 已完成
3. 阶段 2 已完成
4. 阶段 3 已完成最小可信链路
5. 阶段 4 已完成 ready/status/command-ping 地基
6. 阶段 5 已完成临时 payload 回收地基
7. `SpellFire.Hook` 最小服务化已完成：heartbeat/status/shutdown
8. `SpellFire.Hook` 命令封包头已固定：Magic/Version/HeaderSize/Command/Sequence/Status/Result/PayloadLength/PingCount
9. 第一条非 ping 安全命令 `GetHookInfo` 已完成
10. 宿主侧协议模型 `HookProtocol` 已抽离完成
11. 第一条最小内存读命令 `ReadSelfModule` 已完成
12. Hook 侧协议头 `HookProtocol.h` 已抽离完成
13. 远程卸载仍后置
14. `MemoryRobot` session/权限诊断已完成第一刀：
   - `MemorySessionDiagnostics`
   - `MemorySessionProbeResult`
   - `RuntimeHost.Cli memory-probe`
   - `runtimehost-smoke.ps1` 已纳入 `memory-probe`
15. `MemoryRobot` 失败样本矩阵已完成第一刀：
   - `tools/memory-probe-matrix.ps1`
   - `ProcessUnavailable`
   - `AccessDenied`
   - `TargetNot32Bit`

阶段状态：

1. RuntimeHost 测试壳：**可用 smoke 壳**
2. MemoryRobot 远程执行地基：**成型地基**
3. Hook ready/status/command 地基：**成型地基**
4. CLI smoke 自动验收：**可信成品**
5. Memory probe 失败矩阵：**可信成品**
6. 远程卸载：**后置专项**
7. 对象/Lua/移动语义层：**后置专项**

## 禁止事项

1. 禁止重新把 `WRobot` fallback 塞回来
2. 禁止在 hook 未接通前写成 `Ready`
3. 禁止跳过阶段 2 直接撞注入
4. 禁止把 Wow 语义层混进 `RuntimeHost`
5. 禁止恢复单独 `RuntimeHost.Tool`
6. 禁止再让主程序页面承担 `RuntimeHost` 验收
7. 禁止反射旧 `Well` 合同冒充 hook ready
8. 禁止再把 `Well/EasyHook` 旧资产探测写成当前主线

## 验收方式

当前整个专项的最小统一验收命令就是：

```powershell
tools\runtimehost-smoke.ps1 -ProcessId <pid> -Shutdown
tools\memory-probe-matrix.ps1
```

当前验收看九项：

1. `memory-probe` 是否能输出底层 session/权限诊断
2. 进程是否能 attach 到宿主 session
3. `MemoryRobot` 是否 ready
4. `SpellFireHook` 是否能 `HookReady`
5. `status` 是否能证明服务心跳在线
6. `command-ping` 是否能证明命令通道可往返
7. `hook-info` 是否能返回 Hook 自身状态
8. `read-self-module` 是否能只读验证 Hook 自身 PE 头
9. `shutdown` 与 cleanup 是否稳定

如果目标进程已经加载 `SpellFire.Hook.dll`，但 ready/heartbeat 不在线，`runtimehost-smoke.ps1` 必须直接失败为：

```text
ExistingStaleHookPayload
```

这类状态不允许继续跑 repeat attach/status/command-ping，因为它代表目标进程已是脏进程，完整 smoke 结果不再可信。

## 2026-07-05 脏进程快失败与新进程复验

本轮继续确认：

1. 旧 Wow PID `5192` 已处于脏状态：
   - 目标进程内已有 `SpellFire.Hook.dll`
   - `ReadySignal=False`
   - `HeartbeatSignal=False`
   - `runtimehost-smoke.ps1` 现在会快速失败为 `ExistingStaleHookPayload`
2. 结束旧 PID 后按同一路径 `D:\Games\BFWZ\Wow.exe` 启动新 Wow PID `1360`。
3. 新 PID 完整 smoke 已通过：

```text
OK runtimehost-smoke pid=1360
```

覆盖结果：

1. `memory-probe`：`SessionOpened`
2. `preflight`：`MemoryReadyForHook`
3. 首次 `attach`：`HookReady`
4. 重复 `attach`：`HookAlreadyReady`
5. `status`：`HookServiceAlive`
6. `command-ping`：`HookCommandPingOk`
7. `hook-info`：`HookInfoOk`
8. `read-self-module`：`HookSelfModuleReadOk`
9. `shutdown`：`HookShutdownRequested`

结论：

```text
stale hook 检测有效；
干净目标进程上的 HookReady/命令通道/自读模块/shutdown 闭环有效。
```

## 2026-07-05 MemoryRobot 生命周期第一刀

本轮不再继续扩 `RuntimeHost` 测试壳，推进点切回 `SpellFire.MemoryRobot` 本体。

已完成：

1. 新增 `MemorySessionSnapshot`。
2. `MemoryRobotSessionManager` 支持：
   - `GetSessions()`
   - `TryGetSession(processId, out snapshot)`
   - `CloseSession(processId)`
   - `ReleaseAll()`
3. `IMemorySessionFactory` 暴露上述生命周期审计/释放能力。
4. `MemoryRobotRuntimeComponent.Cleanup(processId)` 不再空实现，改为关闭指定 PID 的 MemoryRobot session。

设计边界：

1. `CloseSession(processId)` 只处理指定 PID，避免组件 cleanup 误伤其它目标。
2. `ReleaseAll()` 只作为进程退出或全局兜底能力保留。
3. 本轮不新增对象/Lua/移动语义层。

验收：

```text
dotnet build SpellFire.MemoryRobot: OK
dotnet build SpellFire.RuntimeHost.Cli: OK
memory-probe-matrix: OK
runtimehost-smoke pid=3296 -Shutdown: OK
```

## 收尾规则

满足以下条件前，不把 `RuntimeHost.Cli + runtimehost-smoke.ps1` 之外的部分包装成可信成品：

1. `SpellFireHook` 至少在真实 Wow 进程上完成一次 `HookReady`
2. attach 成败已能被清楚归因
3. cleanup 不再只是 session 级别
4. command-ping 至少在真实 Wow 进程上完成一次往返

本轮审计后的新口径：

1. `RuntimeHost.Cli + runtimehost-smoke.ps1` 可以按“可信成品”使用
2. `MemoryRobot + Hook` 只能按“成型地基”使用
3. `RuntimeHost` 只能按“测试壳 / smoke 壳”使用
4. 后续主线可以基于这些地基继续施工，但不允许对外宣称“运行时成品已完成”

在此之前，`RuntimeHost` 的状态口径固定为：

1. **测试壳 / smoke 壳**

## 下一步

当前最值钱的一刀已经固定：

1. 收口当前 HookReadyPath 小阶段
2. 下一阶段不继续扩 Hook 命令
3. `MemoryRobot` 的 session/权限诊断第一刀已完成
4. `MemoryRobot` 的基础失败矩阵已完成
5. 下一主线应转向 `SpellFire.Runtime` 如何消费 `MemoryRobot + Hook` 这套成型地基

不是：

1. 把 `LoadLibraryW` 返回值冒充成 hook ready
2. 重新碰第三方 runtime
3. 先接 Wow 语义层
4. 在生命周期未设计前强行远程卸载 payload
