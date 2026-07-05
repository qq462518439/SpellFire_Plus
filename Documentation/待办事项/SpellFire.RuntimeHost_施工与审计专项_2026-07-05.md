# SpellFire.RuntimeHost 施工与审计专项

## 当前结论

`SpellFire.RuntimeHost` 不再走“第三方 runtime 像个组件”的旧试探口径，当前专项已经收缩到更干净的主线：

1. `RuntimeHost` 只负责托管 **自家组件**
2. 当前默认组件只有：
   - `MemoryRobotRuntimeComponent`
   - `SpellFireHookRuntimeComponent`
3. `RuntimeHost` 自己就是冒烟入口
4. 退出时必须自动 cleanup

当前不能再做的事：

1. 重新接回 `WRobotComponentAdapter`
2. 默认拼接主仓运行目录
3. 默认拼接官方 WRobot 目录
4. 用第三方资产把宿主状态伪装成“已就绪”

一句话：

`RuntimeHost` 当前专项不是“接第三方运行时”，而是“把 SpellFire 自己的运行时组件宿主做扎实”。

## 阶段收口审计结论

截至本轮审计，当前状态定级如下：

1. `SpellFire.MemoryRobot`：**成型地基**
2. `SpellFire.Hook`：**成型地基**
3. `SpellFire.RuntimeHost`：**成型地基**
4. `RuntimeHost.Cli + tools/runtimehost-smoke.ps1`：**可信成品**

不能把 `RuntimeHost + Hook` 整体升级为“可信成品”的原因：

1. 还没有远程卸载，当前 payload 仍依赖目标进程生命周期自然释放
2. `SpellFire.Hook` 只证明 ready、heartbeat、shutdown、command-ping、hook-info、read-self-module
3. 还没有对象、Lua、移动、战斗、导航语义层消费
4. `RuntimeHost` 还没有向 `WowRuntime` 输出稳定上下文
5. 当前命令通道仍是固定共享内存槽，不是完整 RPC/消息框架

可以认定为“可信成品”的部分：

1. 自动验收脚本可稳定构建、注入、重复 attach、检查心跳、执行命令、请求 shutdown、清理临时 payload
2. CLI 验收不依赖 WPF 页面，不需要人工点按钮
3. 测试后 Wow 进程仍响应，已有真实进程证据

因此本专项当前的正确收口口径是：

`RuntimeHost/Hook 已经形成可继续承载后续 runtime 能力的成型地基，但尚不是业务运行时成品。`

## 当前代码真实状态

当前已经成立的代码事实：

1. `SpellFire.RuntimeHost` 已经重写为独立可执行项目
2. `RuntimeHost`、`RuntimeHostSession`、`RuntimeHostFactory` 已重建
3. `IRuntimeComponent` 合同已重建
4. `MemoryRobotRuntimeComponent` 已接通
5. `SpellFireHookRuntimeComponent` 已从空占位升级到“前置条件探测器”
6. `RuntimeHost` 可直接执行最小冒烟测试
7. `RuntimeHost` 退出时会自动 `Dispose` session 并调用组件 `Cleanup`
8. `SpellFireHookRuntimeComponent` 已不再依赖 `Well.dll / EasyHook` 旧资产检查，而改为基于 `SpellFire.MemoryRobot` 的 hook 地基检查
9. `SpellFire.MemoryRobot` 已补出最小远程执行地基：
   - `IRemoteThreadRunner`
   - `IRemoteLibraryLoader`
   - `CreateRemoteThread + WaitForSingleObject + GetExitCodeThread`
   - `LoadLibraryW` 注入闭环
10. `RuntimeHost` 的 `Attach` 已开始消费这套能力
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

## 当前边界

本专项当前只做：

1. 多进程宿主实例
2. 组件状态探测
3. 组件状态汇总
4. 会话退出清理
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
3. 还没有多进程长期运行压力证据

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
5. 还没有跨多进程并发压力验收

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

1. RuntimeHost 骨架：**成型地基**
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

当前验收看四项：

1. `memory-probe` 是否能输出底层 session/权限诊断
2. 进程是否能 attach 到宿主 session
3. `MemoryRobot` 是否 ready
4. `SpellFireHook` 是否能 `HookReady`
5. `status` 是否能证明服务心跳在线
6. `command-ping` 是否能证明命令通道可往返
7. `hook-info` 是否能返回 Hook 自身状态
8. `read-self-module` 是否能只读验证 Hook 自身 PE 头
9. `shutdown` 与 cleanup 是否稳定

## 收尾规则

满足以下条件前，不把 `RuntimeHost` 包装成可信成品：

1. `SpellFireHook` 至少在真实 Wow 进程上完成一次 `HookReady`
2. attach 成败已能被清楚归因
3. cleanup 不再只是 session 级别
4. command-ping 至少在真实 Wow 进程上完成一次往返

本轮审计后的新口径：

1. `RuntimeHost.Cli + runtimehost-smoke.ps1` 可以按“可信成品”使用
2. `RuntimeHost + MemoryRobot + Hook` 只能按“成型地基”使用
3. 后续主线可以基于它继续施工，但不允许对外宣称“运行时成品已完成”

在此之前，`RuntimeHost` 的状态口径固定为：

1. **成型地基**

## 下一步

当前最值钱的一刀已经固定：

1. 收口当前 HookReadyPath 小阶段
2. 下一阶段不继续扩 Hook 命令
3. `MemoryRobot` 的 session/权限诊断第一刀已完成
4. `MemoryRobot` 的基础失败矩阵已完成
5. 下一主线应转向 `SpellFire.Runtime` 如何消费这套成型地基，或继续补 `MemoryRobot` 多进程 session 管理

不是：

1. 把 `LoadLibraryW` 返回值冒充成 hook ready
2. 重新碰第三方 runtime
3. 先接 Wow 语义层
4. 在生命周期未设计前强行远程卸载 payload
