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
4. 管理 Runtime 层显式连接/释放协议

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
11. `RuntimeConnectionSnapshot`
12. `Connect / Disconnect / GetConnection`

## 当前边界

`SpellFire.Runtime` 当前只做：

1. `Attach(processId)`：兼容旧的一次性快照
2. `Connect(processId)`：建立 Runtime 层显式连接
3. `Disconnect(processId)`：释放 Runtime 当前持有的 host session
4. `GetConnection(processId)`：查询 Runtime 当前连接状态
5. `ProbeMemory(processId)`：暴露 `SpellFire.MemoryRobot` 的只读诊断结果

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

1. 它现在只提供 attach 快照、memory probe 快照、连接/释放快照
2. 还没有对象层、Lua 层、运动层等稳定上游服务
3. 还没有形成主程序连接流程的最终切换门槛

## 下一步

下一刀应做：

1. 固定 `Connect / Disconnect / GetConnection` 作为主程序未来连接流程的候选协议
2. 给 Runtime 增加“只读连接状态审计”脚本或 CLI 命令，确认重复连接/重复释放/目标退出路径
3. 等连接生命周期稳定后，再决定是否新增对象层/Lua 层服务

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

## 2026-07-05 Runtime 连接/释放协议第一刀

本轮新增的是 Runtime 层显式生命周期协议，不改变底层实现职责：

1. `IRuntimeFacade.Connect(int processId)`
2. `IRuntimeFacade.Disconnect(int processId)`
3. `IRuntimeFacade.GetConnection(int processId)`
4. `IRuntimeSessionService.Connect(int processId)`
5. `IRuntimeSessionService.Disconnect(int processId)`
6. `IRuntimeSessionService.GetConnection(int processId)`
7. `RuntimeConnectionSnapshot`
8. `SpellFire.MemoryRobot.Cli runtime-connect-disconnect`

边界：

1. `Attach(processId)` 保持一次性快照兼容。
2. `Connect(processId)` 表达长期连接意图，底层会话由 `RuntimeHost` 持有。
3. `Disconnect(processId)` 只负责释放 Runtime 当前持有的 host session。
4. 本刀不新增对象层、Lua 层、运动层。
5. 本刀不改变 `SpellFire.MemoryRobot` 本体职责。
6. 本刀不让 Runtime 接管 HookReady 策略。

验收标准：

```text
runtime-connect-disconnect:
  Connect 后 Connected=True, SessionExists=True, SessionDisposed=False
  GetConnection 后 Connected=True
  Disconnect 后 Disconnected=True, SessionDisposed=True
  Disconnect 后再次 GetConnection 返回 SessionNotFound
```

本轮验收结果：

```text
dotnet build .\src\SpellFire.Runtime\SpellFire.Runtime.csproj -c Debug: OK, 0 warnings, 0 errors
dotnet build .\src\SpellFire.MemoryRobot.Cli\SpellFire.MemoryRobot.Cli.csproj -c Debug: OK, 0 warnings, 0 errors
memoryrobot-smoke:
  OK runtime-connect-disconnect
  Connected={Connected=True Disconnected=False HostState="Ready" SessionExists=True SessionDisposed=False}
  Disconnected={Connected=False Disconnected=True HostState="Detached" SessionExists=True SessionDisposed=True}
  StatusAfterDisconnect={Connected=False Disconnected=False HostState="Detached" SessionExists=False Reason="SessionNotFound"}
memoryrobot-failure-matrix: OK
```

## 2026-07-06 Runtime 生命周期审计第一刀

本轮新增固定审计命令：

1. `SpellFire.MemoryRobot.Cli runtime-lifecycle-audit`
2. `tools/memoryrobot-smoke.ps1` 纳入 `runtime-lifecycle-audit`

覆盖路径：

1. 重复 `Connect(processId)`：应复用现有连接并保持 `Connected=True`
2. 重复 `Disconnect(processId)`：第一次释放，第二次返回 `SessionNotFound`
3. 目标进程退出后 `Disconnect(processId)`：应完成清理，之后 `GetConnection(processId)` 返回 `SessionNotFound`

本轮验收结果：

```text
dotnet build .\src\SpellFire.Runtime\SpellFire.Runtime.csproj -c Debug: OK, 0 warnings, 0 errors
dotnet build .\src\SpellFire.MemoryRobot.Cli\SpellFire.MemoryRobot.Cli.csproj -c Debug: OK, 0 warnings, 0 errors
memoryrobot-smoke:
  OK runtime-lifecycle-audit
  RepeatedConnectOk=True
  RepeatedDisconnectOk=True
  TargetExitOk=True
memoryrobot-failure-matrix: OK
```
