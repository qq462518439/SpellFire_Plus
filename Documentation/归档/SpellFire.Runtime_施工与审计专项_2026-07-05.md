# SpellFire.Runtime 施工与审计专项

## 当前结论

`SpellFire.Runtime` 不能继续空着。

它当前不应承担四大件实现本体，但必须承担：

1. 上层唯一消费入口
2. 运行时快照模型
3. 宿主接线门面
4. 默认组合根

一句话：

`SpellFire.Runtime` 当前是 **薄中间层**，不是空目录，也不是大而全实现仓。

## 当前定位

它当前负责：

1. 对上提供统一合同
2. 对下消费底层组件的诊断/能力合同
3. 将底层结果转换成稳定快照模型

它当前不负责：

1. 内存读写实现
2. hook 实现
3. Wow 语义实现
4. NavMesh 实现

## 当前已落地内容

1. `IRuntimeFacade`
2. `IRuntimeSessionService`
3. `RuntimeSessionSnapshot`
4. `RuntimeComponentSnapshot`
5. `RuntimeSessionService`
6. `RuntimeFacade`
7. `RuntimeCompositionRoot`
8. `IRuntimeMemoryProbeService`
9. `RuntimeMemoryProbeSnapshot`
10. `RuntimeMemoryProbeService`

## 当前边界

`SpellFire.Runtime` 当前只做：

1. `Attach(processId)`：兼容旧的一次性快照
2. `ProbeMemory(processId)`：暴露 `SpellFire.MemoryRobot` 的只读诊断结果

当前不做：

1. 业务级多角色/多账号调度
2. Wow 语义服务编排
3. 导航接线
4. 插件接线
5. HookReady 生命周期接管
6. 底层内存读写实现

## 当前阶段

当前属于：

1. **成型地基**

不是成品，因为：

1. 它现在只提供 attach 快照、memory probe 快照
2. 还没有对象层、Lua 层、运动层等稳定上游服务
3. 还没有形成主程序连接流程的最终切换门槛

## 下一步

下一刀应做：

1. 保持 `Attach` 与 `ProbeMemory` 的薄门面边界
2. 继续用 CLI/smoke 证明底层能力
3. 等对象层/Lua/运动有真实能力后，再决定是否新增更高层 Runtime 服务

不是：

1. 把实现细节反灌进 `SpellFire.Runtime`
2. 把 `RuntimeHost` 当成生产宿主
3. 把 HookReady、Lua、对象层提前塞进 MemoryRobot

## 2026-07-05 Runtime 消费 MemoryRobot 第一刀

本轮新增的是诊断消费链，不是主流程切换：

1. `IRuntimeFacade.ProbeMemory(int processId)`
2. `IRuntimeMemoryProbeService`
3. `RuntimeMemoryProbeSnapshot`
4. `RuntimeMemoryProbeService`
5. `RuntimeCompositionRoot` 默认组合 `RuntimeMemoryProbeService`
6. `SpellFire.MemoryRobot.Cli runtime-probe` 用来验证 Runtime facade 能消费 MemoryRobot 诊断

边界：

1. `RuntimeMemoryProbeService` 只调用 `MemorySessionDiagnostics.Probe(processId)`。
2. `Runtime` 不直接持有 `MemorySession`。
3. `Runtime` 不负责远程分配、远程线程、LoadLibrary。
4. `Runtime` 不因此接管 HookReady。
5. `RuntimeHost` 仍是测试壳，不是生产主宿主。

验收命令：

```text
dotnet build .\src\SpellFire.Runtime\SpellFire.Runtime.csproj -c Debug
dotnet build .\src\SpellFire.MemoryRobot.Cli\SpellFire.MemoryRobot.Cli.csproj -c Debug
powershell -ExecutionPolicy Bypass -File .\tools\memoryrobot-smoke.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File .\tools\memoryrobot-failure-matrix.ps1 -SkipBuild
```

本轮验收结果：

```text
dotnet build .\src\SpellFire.Runtime\SpellFire.Runtime.csproj -c Debug: OK, 0 warnings, 0 errors
dotnet build .\src\SpellFire.MemoryRobot.Cli\SpellFire.MemoryRobot.Cli.csproj -c Debug: OK, 0 warnings, 0 errors
memoryrobot-smoke: OK, included OK runtime-probe TargetProcessId=11632 Ready=True Reason="SessionOpened"
memoryrobot-failure-matrix: OK
```

## 2026-07-06 Runtime facade 成品入口验收

本轮结论：

1. `SpellFire.Runtime.RuntimeFacade` 已从 `Attach / ProbeMemory` 扩展为正式上层入口。
2. 当前 facade 已覆盖：
   - `Preflight`
   - `AttachHook`
   - `GetHookStatus`
   - `PingHook`
   - `GetHookInfo`
   - `ReadHookSelfModule`
   - `LuaSmoke`
   - `ExecuteLua`
   - `ShutdownHook`
3. `RuntimeCompositionRoot.CreateDefaultFacade()` 已显式组合 `RuntimeHookService`。
4. `tools/runtime-facade-command.ps1` 可从 32 位 PowerShell 宿主直接调用 `RuntimeFacade`。
5. `tools/runtime-facade-matrix.py` 是 Runtime facade 专属验收矩阵。

当前有效边界：

1. `SpellFire.Runtime` 是上层正式入口。
2. `SpellFire.RuntimeHost` 继续承担宿主实现与 smoke 壳职责。
3. `SpellFire.MemoryRobot` 继续只作为底层内存/进程地基。
4. 本轮不引入对象层、移动层、产品生命周期。

已验证：

```powershell
dotnet build .\src\SpellFire.Runtime\SpellFire.Runtime.csproj -c Debug -p:UseSharedCompilation=false
python .\tools\runtime-facade-matrix.py
```

验收结果：

1. 无效 PID 稳定返回 `ProcessUnavailable`。
2. 正常 Wow 进程可通过 facade 完成 `AttachHook`。
3. facade 可完成 `HookStatus / PingHook / HookInfo / ReadSelfModule`。
4. facade 可完成真实 `LuaSmoke`，结果包含 `LuaBridgeReady=True`。
5. facade 可完成真实 `ExecuteLua`，结果包含 `TextPayload=OK:FrameScriptExecute=0`。
6. facade 可完成 `ShutdownHook`。

重要修正：

`HookPayloadPathResolver` 不再只依赖 `AppDomain.CurrentDomain.BaseDirectory`。从 PowerShell、主程序或其他宿主加载 facade 时，payload 查找必须能落到 `SpellFire.RuntimeHost` 程序集目录。
