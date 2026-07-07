# MovementManager 运动管理核心专项

## 结论
`MovementManager` 是下一阶段主线。它不是导航本体，也不是 CTM 原语；它是 `NavigationService` 的下游执行部门，负责把路径点变成稳定、可诊断、可恢复的移动过程。

当前不能继续把逻辑堆进 `NavigationExecutionService`。后续要按 wRobot 的部门关系收口：`RDManaged/PathFinder` 只给路径，`MovementManager` 执行路径，`Movement` 原语只做 CTM/Stop/Jump/Turn 等底层动作。

## wRobot 口径核对
wRobot 不是把“解卡”当作导航部门处理，而是放在移动执行闭环里：

- `PathFinder` / `Pather` / `RDManaged`：负责路径生成、`FindPath`、`FindZ`、路径平滑、路径点偏移、墙距参数等路径层工作。
- `MovementManager`：持有当前路径、当前点、移动线程、MoveTo、StopMove、StopMoveTo、CTM 推进、移动循环、进度监测。
- `StuckResolver`：独立辅助部门，但由 `MovementManager` 在移动循环中触发；它属于运动恢复能力，不属于 PathFinder。
- `wManagerSetting`：把 `BackwardWhenStuck`、`GenerateNewPathToEndWhenStuck`、`DismountWhenStuck`、`AvoidBlacklistedZonesPathFinder`、`WallDistancePathFinder` 分成不同配置项，说明“路径修正”和“移动解卡”是相关但不同的职责。
- `GoToTask` 等任务层：请求 PathFinder 生成路径，然后交给 MovementManager 执行，不自己实现底层移动循环。

所以 SpellFire 的口径应固定为：`StuckResolver` 是 `MovementManager` 的子部门/协作部门，不是 `NavigationService` 的子部门。`NavigationService` 可以根据运动失败请求重新寻路，但不能拥有解卡动作细节。

## 当前状态
- `Movement.Go` 已是单点 CTM 原语，只消费第一个点，不解释路径队列。
- `RDManaged` provider 已可加载 mesh，`FindPath/FindZ` 已通过样本验证。
- RuntimeHost 冒烟页已有“导航到选中目标”按钮，可跑 `目标读取 -> 路径生成 -> 执行 -> 诊断`。
- 普通平面/近中距离目标已能成功到达目标容差。
- 楼上目标、高度差、复杂障碍失败属于预期，因为还没有完整运动管理核心。
- 已新增 `IMovementManager`、`CtmMovementManager`、`MovementProgressSnapshot`、`MovementProgressStatus`。
- `NavigationExecutionService` 已开始改为委托 `IMovementManager`，但仍是过渡调度壳。

## 部门边界
- `IMovementService`：底层动作原语，只负责 `Go/Stop/Jump/Turn/状态读取`，不做路径队列、不做解卡、不做策略。
- `IMovementManager`：运动执行核心，负责路径点推进、进度监控、超时、卡住判定、解卡、Stop/Cleanup。
- `MovementStuckResolver`：`MovementManager` 的恢复子部门，负责安全解卡动作和解卡诊断；不属于 `NavigationService`。
- `INavigationService`：路径服务，负责 `FindPath/FindZ/capability`，不直接操作 CTM。
- `NavigationExecutionService`：过渡调度层，负责拿路径并调用 `MovementManager`，后续应继续瘦身。
- `RuntimeHost` 冒烟页：只做验收入口和诊断展示，不承载运动策略。
- `RobotManager/Product`：后置，不允许绕过 `WowRuntime` 直接触碰 Hook 或 MemoryRobot。

## 禁止事项
- 禁止把解卡、路径点过滤、墙距修正继续塞进 `NavigationExecutionService`。
- 禁止让 `Movement.Go` 变成路径执行器。
- 禁止 UI 拼业务策略；UI 只能调用已成型的 CLI/facade 并展示结果。
- 禁止为了某个测试点通过而写随机移动、乱跳、乱转。
- 禁止在 MovementManager 未稳定前接入 Product/Quest/Gather/Combat。

## 目标形状
`MovementManager` 最终应拆成清晰子部门：

- `MovementPathExecutor`：消费路径点队列，按策略推进。
- `MovementProgressTracker`：采样当前位置、速度、CTM 状态、距离变化、最近进展时间。
- `MovementStuckDetector`：基于距离变化、速度、CTM 状态判断卡住。
- `MovementStuckResolver`：执行安全解卡动作；归属 MovementManager，不归属 Navigation。
- `MovementStopPolicy`：统一 Stop/Cleanup，保证失败和成功后状态收口。
- `MovementPathPointFilter`：短点合并、过近点跳过、长段拆分。
- `MovementDiagnostics`：输出稳定 DTO，供 CLI 和冒烟页解析。

第一阶段不需要一次全拆完，但新增代码必须能迁移到这个形状，不能形成新蜘蛛网。

## P1 施工顺序
1. `MovementProgressSnapshot` 扩容
   - 增加段起点、段目标、当前位置、距离、最佳距离、速度、CTM 状态、采样次数、是否触发 Stop。
   - 输出字段必须可被 CLI/冒烟页稳定解析。

2. `CtmMovementManager` 收口
   - 保留 `MoveToPoint` 作为第一入口。
   - 内部先拆私有方法：启动 CTM、采样进度、判断到达、判断卡住、Stop。
   - 不新增复杂策略，只先把结构切干净。

3. 最小解卡子部门
   - 新建/预留 `MovementStuckResolver`，由 `CtmMovementManager` 调用。
   - 第一版只允许安全动作：`Stop -> Jump -> 重新 CTM -> 重新采样`。
   - 每次解卡必须记录到诊断。
   - 不做随机乱跑，不做大角度乱转。
   - 不允许 `NavigationExecutionService` 直接调用解卡动作。

4. 路径点过滤
   - 过近点跳过。
   - 长段按距离拆分，避免单段过长导致 CTM 采样不稳定。
   - 保留墙距修正接口，但 `RDManaged.FixPathByDistanceToWall` 未完成验真前不启用。

5. Navigation 瘦身
   - `NavigationExecutionService` 只负责 `FindPath -> MovementManager.MovePath/MoveToPoint -> 汇总结果`。
   - 不再拥有卡住判定、超时预算、Stop 细节。
   - 允许 Navigation 在 MovementManager 返回 `Stuck/Timeout` 后触发“重新 FindPath”策略，但策略入口必须是重新寻路，不是直接解卡。

## 验收入口
- 构建：`dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false`
- 构建：`dotnet build .\src\SpellFire.RuntimeHost\SpellFire.RuntimeHost.csproj -c Debug -p:UseSharedCompilation=false`
- 现有矩阵：`python tools\wowruntime-navigation-capability-matrix.py`
- 冒烟页：`导航到选中目标`

后续需要新增 MovementManager 专属矩阵，但必须等 DTO 稳定后再写，避免测试脚本追着临时字段跑。

## 人工冒烟场景
- 近距离普通目标：10-20 码。
- 中距离普通目标：50-80 码。
- 相邻 tile 目标。
- 轻微障碍目标。
- 楼上/高度差目标。

楼上/高度差在 P1 阶段允许失败，但失败必须输出可解释诊断：卡在哪个点、当前坐标、最佳距离、CTM 状态、是否解卡、Stop 是否执行。

## 成品标准
- 普通目标成功率稳定，不依赖手工调整角色朝向。
- 失败能明确区分：路径失败、CTM 启动失败、无进展、超时、玩家状态不可读。
- Stop/Cleanup 始终可见。
- 解卡动作有限、可解释、可关闭。
- Navigation 不再包含运动细节。
- 冒烟页显示 MovementManager 诊断，而不是只显示最终 Success/Stuck/Timeout。

## 当前停工条件
- 如果普通目标也频繁失败，暂停 Navigation，回到 `Movement.Go/CTM 状态/玩家位置采样`。
- 如果 Wow 进程不在 `InWorld`，禁止跑移动动作。
- 如果 Hook 不可用，禁止跑 CTM。
- 如果目标在楼上/隔墙/复杂障碍，失败不作为 RDManaged provider 失败处理。
