# RobotManager 最小成品前置契约审计

## 结论
`RobotManager` 已进入最小成品收口审计阶段。当前重点不是扩展产品调度器，而是确认最小生命周期、状态机、PulseLoop、ProductContext 契约都可重复验收，并且 `RobotManager` 只能消费 `SpellFire.WowRuntime` 暴露的 WoW 语义接口，不能直连 `MemoryRobot`、`Hook`、`RuntimeHost` 或 `RuntimeFacade`。

## 当前事实
- `WowRuntime` 已提供 `ObjectManager / WorldState / WorldSnapshots / Movement / Scripts`。
- `WowRuntime` 内部基础设施可以引用 `MemoryRobot` 和 `Runtime`，这是语义层的实现细节。
- `SpellFire.RobotManager` 已独立成 `net48/x86` 项目，只引用 `SpellFire.WowRuntime`。
- `SpellFire.RobotManager.Cli` 是验收壳，允许创建 `WowRuntime` 并调用 RobotManager。
- `Navigation` 仍未进入当前阶段，`Movement.Go / FaceTo / FaceObject` 继续返回 `FeatureUnavailable`。

## 允许的 RobotManager 依赖面
- 允许依赖 `SpellFire.WowRuntime.Core.IWowRuntime`。
- 允许依赖更窄的 `SpellFire.WowRuntime.Bot.IWowRuntimeContext`。
- 允许使用以下 WowRuntime 部门：
  - `World`
  - `WorldSnapshots`
  - `ObjectManager`
  - `Movement`
  - `Scripts`
- 允许读取 `WowRuntimeSnapshot / RuntimeWorldSnapshot / ObjectManagerSnapshot / MovementStateSnapshot` 这类 DTO。

## 禁止的 RobotManager 依赖面
- 禁止引用 `SpellFire.MemoryRobot`。
- 禁止引用 `SpellFire.Hook`。
- 禁止引用 `SpellFire.RuntimeHost`。
- 禁止引用 `SpellFire.Runtime` / `IRuntimeFacade`。
- 禁止引用进程注入、远程线程、模块加载、Hook 命令通道等底层能力。
- 禁止在 `RobotManager` 内新增对象层地址模型、Lua 桥协议、Movement 内存读写逻辑。

## 最小成品边界
第一版 `RobotManager` 已完成范围：
- `IRobotManager`
- `IProduct`
- `ProductContext`
- `PulseLoop`
- `Start / Pause / Stop`
- 一个内置测试 Product
- 状态机硬化：重复 Start/Stop、Paused/Faulted 拒绝、Faulted 后 Stop 清理。
- 定时 PulseLoop：固定间隔后台 Pulse，可停止。
- ContextProbeProduct：通过 ProductContext 调用 Scripting 和 WorldSnapshots。

第一版 `RobotManager` 不做：
- 官方 wRobot Product 兼容。
- Quest / Gather / Combat 占位。
- 导航和路径规划。
- CTM 写入。
- 新对象层字段扩展。

## 契约守卫
新增脚本：

```powershell
python tools\robotmanager-contract-guard.py
```

守卫规则：
- 如果 `src/SpellFire.RobotManager` 尚不存在，只扫描 `src/SpellFire.WowRuntime/Bot`。
- 如果未来新增 `src/SpellFire.RobotManager`，自动纳入扫描。
- 扫描 `.cs` 与 `.csproj`。
- 命中禁止命名空间、项目引用或底层关键词时失败。

## 验收命令
```powershell
python -m py_compile tools\robotmanager-contract-guard.py
python tools\robotmanager-contract-guard.py
python tools\wowruntime-matrix.py --pid 4120
```

## 当前验收入口
```powershell
python tools\robotmanager-contract-guard.py
python tools\robotmanager-minimal-matrix.py
```

矩阵覆盖：
- 项目引用只允许 `SpellFire.RobotManager -> SpellFire.WowRuntime`。
- `ProductContext` public 成员白名单。
- `noop-lifecycle`。
- `state-machine-hardening`。
- `timed-pulse-loop`。
- `context-probe-product`。

## 下一步
`RobotManager 最小成品收口审计` 当前已具备可验证入口：`python tools\robotmanager-contract-guard.py` 与 `python tools\robotmanager-minimal-matrix.py`。下一步只做提交前整理和边界复核；若继续设计下一阶段 Product 体系前置工作，仍然不能从 Hook、MemoryRobot 或官方产品加载器倒推。
