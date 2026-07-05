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

当前能被验证的能力：

1. 打开真实 32 位 Wow 进程
2. 拒绝不存在的 PID
3. 识别权限拒绝
4. 识别 64 位目标不适配
5. 完成远程分配/释放前置验证
6. 支撑 `SpellFire.Hook` 的 `LoadLibraryW` 注入 smoke
7. 支撑 Hook command-ping / hook-info / read-self-module smoke

## 当前定级

`SpellFire.MemoryRobot` 当前定级为：**可继续推进的底层组件**。

不是“业务成品”，原因：

1. 还没有独立于 `RuntimeHost` 的 MemoryRobot 专属验收脚本。
2. 读写结果模型刚补齐，缺少直接覆盖 `TryRead/TryWrite` 的专属样本。
3. session 生命周期已有基础接口，但缺少竞态样本，例如目标进程退出中、重复打开关闭、关闭后再读写。
4. 远程线程和 LoadLibrary 能力存在，但还没有被收口成 MemoryRobot 自己的验收矩阵。

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

## 下一刀

下一刀应该做：**MemoryRobot 专属 smoke CLI / 脚本**。

最小命令建议：

1. `probe <pid>`
2. `session-open-close <pid>`
3. `module-snapshot <pid>`
4. `memory-region <pid>`
5. `remote-alloc-free <pid>`
6. `try-read-invalid`

暂不做 `try-write-real-process`，除非写入目标是本工具自己启动的安全子进程或远程临时分配页。

## 停工线

如果某个能力不能用独立 MemoryRobot smoke 证明，就不要把它写成已完成。

如果某个能力需要知道 Wow 偏移、对象结构、Lua、移动或产品生命周期，它就不属于 `SpellFire.MemoryRobot`。
