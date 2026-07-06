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
   - 实现 `Me / Target / ObjectList / ObjectDictionary / GetByGuid / GetByEntry / NearbyObjects`。
   - 输出稳定 DTO，不引入导航、战斗、产品逻辑。
   - 这是 `robotManager` 的第一前置条件。
   - 当前状态：已实装并通过 `tools/wowruntime-objectmanager-matrix.py` 验证。

2. `Scripting 门面`
   - 把现有 LuaSmoke/LuaExec 从 Runtime 能力包装成 WowRuntime 语义接口。
   - 不追求复杂返回值系统，先保证执行命令、游戏内可见反馈、错误语义稳定。
   - 这是 `robotManager` 控制产品脚本能力的第二前置条件。
   - 当前状态：已实装并通过 `tools/wowruntime-scripting-matrix.py` 验证。

3. `Movement 最小动作层`
   - 只做可验证动作：Jump、Stop、Face/Move 基础能力。
   - 不做导航，不做路径规划。
   - 这是 `robotManager` 调度行为的第三前置条件。
   - 当前状态：已实装 Jump / Stop / StopTo；`Go` 明确返回未实现，避免伪导航。通过 `tools/wowruntime-movement-matrix.py` 验证。

4. `RobotManager 最小成品`
   - 建立 `IRobotManager`、`IProduct`、`ProductContext`、`PulseLoop`、`Start/Pause/Stop`。
   - 只跑一个内置测试 Product，不接官方 Products。
   - Product 只能通过 `WowRuntime` 访问对象、Lua、运动，不允许直连底层。

5. `Product 体系`
   - 在 `RobotManager` 稳定后，再考虑加载外部 Product。
   - 官方 wRobot Product 只作为参考，不作为短期兼容目标。
   - 不承诺直接跑官方产品，除非 Hook/Lua/Object/Movement 全部验证达标。

## Immediate Next Mainline
下一刀是 `WowRuntime 三部门收口 -> RobotManager 最小成品前置`。当前不继续扩 ObjectManager 字段，也不进入导航；先把 `ObjectManager / Scripting / Movement` 的验收入口和边界固定，确保 `RobotManager` 只能消费 `WowRuntime`，不能直连 Hook / MemoryRobot。

- 固定 `tools/wowruntime-matrix.py` 作为 WowRuntime 三部门聚合验收入口。
- 保持 `ObjectManager` 只读，不做 Pulsator。
- 保持 `Scripting` 只做 LuaSmoke / Execute，不做复杂 return 值系统。
- 保持 `Movement` 只做 Jump / Stop / StopTo；`Go` 在导航专项前必须继续拒绝。
- 下一阶段才建立 `RobotManager` 最小 `PulseLoop`，并只允许通过 `WowRuntime` 访问对象、Lua、运动。

## Acceptance Criteria
- ObjectManager 能通过 CLI/脚本稳定读到本地玩家、目标、对象计数、附近对象。
- Scripting 能通过 CLI/脚本执行 Lua 冒烟和聊天框可见 Lua。
- Movement 能通过 CLI/脚本执行 Jump / Stop / StopTo，且 `Go` 明确拒绝。
- 失败原因必须可区分：Hook 未就绪、进程不存在、内存读取失败、对象不存在、地址模型缺失。
- `MemoryRobot`、`RuntimeHost`、`RuntimeFacade` 现有矩阵不退化。
- 不新增假的 Product/Quest/Gather/Combat 占位。
- 不允许 `RobotManager` 直接依赖 `MemoryRobot` 或 `Hook`，它只能依赖 `WowRuntime`。

## Assumptions
- 现在不做官方 `robotManager.dll` 兼容。
- 现在不加载官方 Products。
- 现在不把 `RuntimeHost` 升级成业务运行时。
- `robotManager` 是明确目标，但必须等 ObjectManager、Scripting、Movement 三个基础部门有实证后再落地。
