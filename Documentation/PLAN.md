# SpellFire_Plus 全局蓝图 v2

## Summary
目标不是走一步看一步，而是按 `MemoryRobot -> Runtime/Hook -> WowRuntime -> RobotManager -> Product` 的顺序成型。`robotManager` 是明确目标层，但当前不先实现产品调度器，而是先建设它必须依赖的 `WowRuntime.ObjectManager`、Lua、Movement 三个基础部门。

## Target Architecture
- `SpellFire.MemoryRobot`：只做进程、内存、模块、线程、远程库加载等底层原语。
- `SpellFire.Hook`：只做进程内桥，负责 Lua/main-thread/命令通道。
- `SpellFire.RuntimeHost`：只做宿主实现和冒烟验证，不承载业务。
- `SpellFire.Runtime`：主程序/上层调用的正式门面。
- `SpellFire.WowRuntime`：WoW 语义运行时，承载 ObjectManager、Scripting、Movement、Navigation 的稳定接口。
- `SpellFire.RobotManager`：未来的产品调度层，负责 Product 生命周期、Pulse、状态机、暂停/恢复、异常隔离。
- `Products`：最终业务插件/产品，只消费 `RobotManager + WowRuntime`，不直接摸 Hook/MemoryRobot。

## Build Order
1. `ObjectManager 只读地基`
   - 实现 `Me / Target / ObjectList / GetByGuid / GetByEntry / NearbyObjects / NearestObjects`。
   - 输出稳定 DTO，不引入导航、战斗、产品逻辑。
   - 这是 `robotManager` 的第一前置条件。
   - 当前状态：已实装并通过 `tools/wowruntime-objectmanager-matrix.py` 验证。
   - 当前边界：只读对象层，不做 Pulsator，不做写入，不接管生命周期。

2. `Scripting 门面`
   - 把现有 LuaSmoke/LuaExec 从 Runtime 能力包装成 WowRuntime 语义接口。
   - 不追求复杂返回值系统，先保证执行命令、游戏内可见反馈、错误语义稳定。
   - 这是 `robotManager` 控制产品脚本能力的第二前置条件。
   - 当前状态：已实装并通过 `tools/wowruntime-scripting-matrix.py` 验证。

3. `Movement 最小动作层`
   - 只做可验证动作：Jump、StartMoveForward、StartMoveBackward、StartStrafeLeft、StartStrafeRight、Stop、StopTo、TurnLeft、TurnRight、TurnStop、Go 单点 CTM。
   - 不做导航，不做路径规划；`Go` 当前只消费第一个目标点，不解释路径队列。
   - 这是 `robotManager` 调度行为的第三前置条件。
   - 当前状态：已实装 Jump / Forward / Backward / StrafeLeft / StrafeRight / Stop / StopTo / TurnLeft / TurnRight / TurnStop / MovementState / CTM 诊断 / Go 单点 CTM；通过 `tools/wowruntime-ctm-matrix.py` 与 `tools/wowruntime-movement-matrix.py` 验证。
   - 当前边界：`FaceTo` / `FaceObject` 仍返回 `FeatureUnavailable`；`Go` 不是导航，只是单点 CTM 动作。

4. `WorldState / RuntimeWorldSnapshot 聚合层`
   - 聚合 `Phase / Player / Me / Target / Counts / NearestUnit / NearestGameObject`。
   - 世界阶段通过 `world-phase` 与 `tools/wowruntime-worldphase-matrix.py` 验证。
   - 当前状态：`InWorld` 样本已稳定；登录界面、角色列表、加载地图仍需真实场景样本补齐。

5. `RobotManager 最小成品`
   - 建立 `IRobotManager`、`IProduct`、`ProductContext`、`PulseLoop`、`Start/Pause/Stop`。
   - 只跑一个内置测试 Product，不接官方 Products。
   - Product 只能通过 `WowRuntime` 访问对象、Lua、运动，不允许直连底层。

6. `Product 体系`
   - 在 `RobotManager` 稳定后，再考虑加载外部 Product。
   - 官方 wRobot Product 只作为参考，不作为短期兼容目标。
   - 不承诺直接跑官方产品，除非 Hook/Lua/Object/Movement 全部验证达标。

## Immediate Next Mainline
`Hook CTM native command + Movement.Go 单点样本` 已完成。当前不能直接声称有 Navigation：Navigation 仍由 `UnavailableNavigationService` 返回 NotImplemented，`Movement.Go` 只是单点 CTM，不做路径解释、避障、分段、失败恢复。

`Movement.Go 成品语义收口 + Navigation 前置边界` 已完成第一段：CLI 坐标守卫、InWorld live 矩阵选择、Navigation capability 固定为不可用事实。

- 保持 RuntimeHost / Runtime facade 只暴露受控 CTM 入口，不提供任意 native call。
- `Movement.Go` 固定为单点 CTM：空点集拒绝，多点只取第一个点，路径队列留给 Navigation。
- `movement-go` CLI 必须显式传入 `--x --y --z`，禁止隐式使用 `0,0,0`。
- live movement 矩阵必须先探测 `world-phase`，只选择 `Phase=InWorld` 的 Wow 进程；登录界面、角色列表、加载中进程只能做阶段状态验证，不能跑移动动作。
- 补充 CTM 停止语义：当前 Stop 仍走 Lua StopMove 系列，不伪装为原生 CTM stop。
- Navigation capability 已固定输出：`CanFindPath=False / CanExecutePath=False / CanFindZ=False / SupportsPathQueue=False / SupportsArrivalCheck=False / SupportsStuckDetection=False`。
- 下一刀进入 Navigation 最小契约设计：输入路径点、分段策略、到达判定、卡住判定、Stop/Cleanup 责任。
- 不进入 Product / Quest / Gather / Combat。

全局真相以 `Documentation/全局真相路线图.md` 为准；CTM 前置审计已归档到 `Documentation/归档/CTM写入前置审计.md`。

- 固定 `tools/wowruntime-matrix.py` 作为 WowRuntime 三部门聚合验收入口。
- 固定 `tools/wowruntime-worldphase-matrix.py` 作为世界阶段验收入口。
- 保持 `ObjectManager` 只读，不做 Pulsator。
- 保持 `Scripting` 只做 LuaSmoke / Execute，不做复杂 return 值系统。
- 保持 `Movement` 只做 Jump / Forward / Backward / StrafeLeft / StrafeRight / Stop / Turn / 状态读取 / CTM 诊断 / Go 单点 CTM；`FaceTo` / `FaceObject` 继续拒绝。
- 已产出 `RobotManager` 最小接口草案：`IRobotManager / IProduct / ProductContext / ProductState / RobotManagerResult`。
- 已建立可重复验收的最小生命周期：`Start -> PulseOnce -> Pause -> Resume -> PulseOnce -> Stop`，当前只运行内置 `NoopProduct`。
- 已硬化状态机：异常后 `Faulted` 语义、重复 Start/Stop、暂停态 Pulse 拒绝、Stop 后清理一致性。
- 已建立最小定时 `PulseLoop`：可启动、可停止、固定间隔、脚本验收至少多次 Pulse，不引入产品加载器。
- 已完成最小上下文感知测试 Product：`ContextProbeProduct` 通过 `ProductContext.Scripts.Execute()` 执行安全 Lua 可见消息，并通过 `ProductContext.WorldSnapshots.Capture()` 尝试读取世界快照。
- 已冻结 `ProductContext` 契约：允许成员为 `ProcessId / World / WorldSnapshots / ObjectManager / Movement / Scripts / Snapshot`，禁止公开 `IWowRuntime / Navigation / RuntimeFacade / MemoryRobot / Hook`。
- 已完成 `RobotManager` 最小成品收口审计：接口、CLI、矩阵、文档、守卫足以作为后续 Product 体系前置地基。
- RobotManager 当前只作为后续 Product 体系前置地基保留，不再作为当前施工主线。
- `RobotManager` 只能通过 `WowRuntime` 访问对象、Lua、运动和世界快照。
- 每轮 RobotManager 施工后必须运行 `python tools\robotmanager-contract-guard.py`。

## Acceptance Criteria
- ObjectManager 能通过 CLI/脚本稳定读到本地玩家、目标、对象计数、附近对象。
- Scripting 能通过 CLI/脚本执行 Lua 冒烟和聊天框可见 Lua。
- Movement 能通过 CLI/脚本执行 Jump / Forward / Backward / StrafeLeft / StrafeRight / Stop / StopTo / TurnLeft / TurnRight / TurnStop / Go 单点 CTM，且 `FaceTo` / `FaceObject` 明确拒绝。
- WorldSnapshot 能通过 CLI/脚本输出 `InWorld / HasPlayer / HasTarget / NearestUnit / NearestGameObject`。
- WorldPhase Watch 能输出 `FirstPhase / LastPhase / PhaseCounts / EnterCount` 汇总证据。
- 失败原因必须可区分：Hook 未就绪、进程不存在、内存读取失败、对象不存在、地址模型缺失。
- `MemoryRobot`、`RuntimeHost`、`RuntimeFacade` 现有矩阵不退化。
- 不新增假的 Product/Quest/Gather/Combat 占位。
- 不允许 `RobotManager` 直接依赖 `MemoryRobot` 或 `Hook`，它只能依赖 `WowRuntime`。

## Assumptions
- 现在不做官方 `robotManager.dll` 兼容。
- 现在不加载官方 Products。
- 现在不把 `RuntimeHost` 升级成业务运行时。
- `robotManager` 是明确目标，但必须等 ObjectManager、Scripting、Movement 三个基础部门有实证后再落地。
