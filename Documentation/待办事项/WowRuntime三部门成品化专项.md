# WowRuntime 三部门成品化专项

## Summary
本专项用于固定下一阶段施工边界：先把 `WorldState / ObjectManager / Movement` 收成可被后续导航稳定依赖的成品层。导航、RobotManager、Product 全部后置。

当前目标不是扩功能面，而是把已有 WowRuntime 三个基础部门做成可验证、可复用、失败语义清晰的运行时地基。

## 当前主线
- `ObjectManager`：只读对象层成品化。
- `WorldState`：基于对象层聚合玩家与世界快照。
- `Movement`：只做可验证最小动作层。
- `Navigation`：本阶段禁止设计和实现。
- `RobotManager / Product`：不进入本阶段。

## 当前状态
- `ObjectManager` 已有 `Me / Target / Objects / Guid / Entry / Kind / Nearby / Nearest`。
- `WorldState.GetPlayer()` 已优先走 `ObjectManager.GetMe()`。
- `WorldState.GetPhase()` 已按 WRobot `Usefuls` 地址读取 `InGame / IsLoadingOrConnecting`，用于区分登录/角色列表、加载/连接、世界内。
- `Movement` 已有 `Jump / StopMove / StopMoveTo`。
- `Movement.Go` 已明确拒绝，导航专项前必须继续返回 `FeatureUnavailable`。
- `Scripting` 已作为 Movement 动作通道存在，但本专项不扩展复杂 Lua return 值系统。

## 成品标准
- CLI 输出稳定，字段可被脚本持续解析。
- Python 矩阵可重复运行，失败时能给出命令、退出码、原始输出。
- 失败原因必须可区分：进程不存在、Hook 未就绪、对象链不可用、对象不存在、地址模型缺失。
- 未知字段必须显式表达未知，不允许伪造成有效数据。
- 所有动作必须有真实 Lua/Hook 证据或游戏内可见反馈。
- `ObjectManager` 保持只读，不做 Pulsator，不接管运动、战斗或产品生命周期。
- `Movement` 只保留最小动作能力，不做路径规划，不做导航替代品。

## 下一刀顺序
1. `ObjectManager` 快照语义和失败语义收口。
2. `WorldState / RuntimeWorldSnapshot` 聚合字段收口。
3. `Movement` 状态语义修正，处理 `InMovement=false` 假稳定问题。
4. CLI 与 Python 矩阵补齐验收。
5. 更新 `Documentation/PLAN.md` 的 `Immediate Next Mainline` 状态。

## 固定验收命令
```powershell
dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false
python tools\wowruntime-objectmanager-matrix.py
python tools\wowruntime-scripting-matrix.py
python tools\wowruntime-movement-matrix.py
python tools\wowruntime-worldphase-watch.py --samples 3 --interval 0.2 --expect-phase InWorld
python tools\wowruntime-matrix.py
python tools\runtime-facade-matrix.py
```

## 每轮施工记录要求
每轮施工结束必须记录以下内容：

- 本轮改了哪个部门。
- 哪些 CLI 命令通过。
- 哪些字段仍是未知。
- 是否触碰导航、RobotManager、Product。预期答案必须是否。

## 施工记录

### 2026-07-06 第一刀：Movement 状态语义与 WorldSnapshot 聚合
- 本轮改动部门：`Movement`、`WorldState / RuntimeWorldSnapshot`、CLI 验收矩阵。
- `Movement.InMovement` 裸 `bool` 已移除，改为 `GetMovementState()` 返回 `WowRuntimeResult<MovementStateSnapshot>`，避免未知状态被伪装成 `false`。
- 新增 CLI：`movement-state`。
- `RuntimeWorldSnapshot` 已显式携带 `Player`，`world-snapshot` 输出 `Player=MapId=... Pos=... Movement=...`。
- 已通过 CLI/脚本：
  - `movement-state`
  - `movement-jump`
  - `movement-stop`
  - `movement-stop-to`
  - `world-player`
  - `world-phase`
  - `world-snapshot`
  - `object-snapshot`
  - `object-me`
  - `object-by-guid`
  - `object-nearest`
- 已通过验收命令：
  - `dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false`
  - `python tools\wowruntime-objectmanager-matrix.py`
  - `python tools\wowruntime-movement-matrix.py`
  - `python tools\wowruntime-matrix.py`
  - `python tools\runtime-facade-matrix.py`
- 仍是未知/未完成字段：
  - `MapId` 当前兼容输出为 WRobot 风格 `ContinentId`，在线样本已实证为 `571 / Northrend`，不是未知。
  - `MovementFlags` 目前来自 `WorldState`，当前在线样本为 `None`；真实移动位仍需后续地址模型实证。
  - 对象 `Name` 仍为空，尚未进入对象名读取专项。
- 是否触碰导航：否。
- 是否触碰 RobotManager：否。
- 是否触碰 Product：否。

### 2026-07-06 第二刀：WorldPhase 世界外状态地基
- 本轮改动部门：`WorldState / RuntimeWorldSnapshot`、CLI 验收矩阵。
- 新增 `WorldPhaseSnapshot` 与 `WorldPhaseKind`，阶段只表达可观测状态：`LoginOrCharacterList`、`LoadingOrConnecting`、`InWorld`、`Unknown`。
- 新增 CLI：`world-phase`。
- `world-snapshot` 已输出 `Phase=... InGame=... LoadingOrConnecting=... Source="Usefuls"`。
- 状态来源已按 WRobot `Usefuls` 核对：
  - `InGame`：十进制 `8193938`，十六进制 `0x7D0792`，rebase 后读 `byte > 0`。
  - `IsLoadingOrConnecting`：十进制 `7776824`，十六进制 `0x76AA38`，rebase 后读 `int != 0`。
- 在线样本实证：
  - `world-phase` 输出 `Phase=InWorld InGame=True LoadingOrConnecting=False`。
  - `world-snapshot` 同步输出 `Phase=InWorld`，并携带 `Player=MapId=571 ContinentName="Northrend"`。
- 已通过验收命令：
  - `dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false`
  - `python tools\wowruntime-objectmanager-matrix.py`
  - `python tools\wowruntime-movement-matrix.py`
  - `python tools\wowruntime-matrix.py`
  - `python tools\runtime-facade-matrix.py`
- 仍是未知/未完成字段：
  - 登录界面、角色列表、加载地图状态已具备 CLI 读取能力，但还需要在实际切换客户端状态时采集样本确认。
  - `MovementFlags` 仍需后续地址模型实证。
  - 对象 `Name` 仍为空，尚未进入对象名读取专项。
- 是否触碰导航：否。
- 是否触碰 RobotManager：否。
- 是否触碰 Product：否。

### 2026-07-06 第三刀：WorldPhase 采样工具与矩阵降噪
- 本轮改动部门：`WorldState` 验收工具、CLI 验收矩阵。
- 新增 `tools/wowruntime-worldphase-watch.py`，只调用 `world-phase`，不 Hook、不 Lua、不对象扫描，专用于登录界面、角色列表、加载地图、世界内状态采样。
- 脚本支持：
  - `--pid` 指定目标进程；未指定时自动选择最新 `Wow.exe`。
  - `--expect-phase` 等待指定阶段，例如 `InWorld`、`LoginOrCharacterList`、`LoadingOrConnecting`。
  - `--samples` 与 `--interval` 控制采样次数和间隔。
- `tools/wowruntime-matrix.py` 已对成功子脚本长输出做截断，失败输出仍完整保留，避免对象列表过长造成验证噪音。
- 在线样本实证：
  - `python tools\wowruntime-worldphase-watch.py --samples 3 --interval 0.2 --expect-phase InWorld`
  - 输出 `Phase=InWorld InGame=True LoadingOrConnecting=False`，并在第一帧匹配成功。
- 已通过验收命令：
  - `dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false`
  - `python tools\wowruntime-worldphase-watch.py --samples 3 --interval 0.2 --expect-phase InWorld`
  - `python tools\wowruntime-matrix.py`
- 仍是未知/未完成字段：
  - 登录界面、角色列表、加载地图尚未实际切换采样；当前只是工具已就绪。
  - `MovementFlags` 仍需后续地址模型实证。
  - 对象 `Name` 仍为空，尚未进入对象名读取专项。
- 是否触碰导航：否。
- 是否触碰 RobotManager：否。
- 是否触碰 Product：否。

### 2026-07-06 第四刀：MovementState 脱离对象层并接入 ClickToMove 只读状态
- 本轮改动部门：`Movement`、`WorldState`、CLI 验收矩阵。
- 结论：`MovementManager.InMovement` 在 WRobot 中是路径线程内部布尔值，不是可直接读取的内存事实；短期不复刻该线程状态。
- 已接入更可靠的只读事实：
  - `ClickToMove.GetClickToMoveTypePush()` 来源为十进制 `9048564`，十六进制 `0x8A11F4`，rebase 后读 `int`。
  - `ClickToMoveType.Move = 4` 视为 CTM 移动中。
  - `ClickToMoveType.None = 13` 视为当前没有 CTM 移动。
- `IWorldState` 新增 `GetMovementState()`，`movement-state` 不再依赖 `ObjectManager.GetMe()`，因此登录界面/角色列表/对象链不可用时仍能返回可解释状态。
- `movement-state` 输出已扩展：
  - `ClickToMoveTypeRaw`
  - `ClickToMoveState`
  - `SpeedKnown`
  - `Speed`
  - `Detail` 中带 `Phase=...`
- 当前角色列表/登录阶段样本实证：
  - `world-phase` 输出 `Phase=LoginOrCharacterList InGame=False LoadingOrConnecting=False`。
  - `movement-state` 输出 `Ready=True InMovement=False Flags=None ClickToMoveTypeRaw=13 ClickToMoveState=None SpeedKnown=False`。
- 已通过验收命令：
  - `dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false`
  - `python tools\wowruntime-movement-matrix.py`
- 仍是未知/未完成字段：
  - `Me.GetMove / SpeedMoving` 来源为 `base+216 -> +140`，需要本地对象在世界内稳定时再接入速度读取；本轮只接静态 CTM 状态。
  - `MovementFlags` 仍不是完整 WoW movement flags，只是最小语义聚合。
  - 对象 `Name` 仍为空，尚未进入对象名读取专项。
- 是否触碰导航：否。
- 是否触碰 RobotManager：否。
- 是否触碰 Product：否。

### 2026-07-06 第五刀：MovementState 接入 SpeedMoving 世界内速度证据
- 本轮改动部门：`Movement`、`WorldState`、CLI 验收矩阵。
- 已按 WRobot `WoWUnit.SpeedMoving` 核对速度来源：
  - 本地对象 `BaseAddress + 216` 读取 movementInfo 指针。
  - `movementInfo + 140` 读取 `float SpeedMoving`。
  - `GetMove` 语义等价于 `SpeedMoving > 0`。
- `GetMovementState()` 现在组合两类只读证据：
  - 静态 CTM 状态：`ClickToMoveTypeRaw / ClickToMoveState`。
  - 世界内对象速度：`SpeedKnown / Speed`。
- 世界外或对象层不可用时不失败，保持 `SpeedKnown=False`，继续返回 CTM/Phase 状态。
- 世界内样本实证：
  - `object-me` 输出 `Base=0x24808F78`。
  - `movement-state` 输出 `Phase=InWorld ClickToMoveTypeRaw=13 ClickToMoveState=None SpeedKnown=True Speed=0`。
  - 当前角色静止，因此 `InMovement=False Flags=None` 是合理状态。
- 已通过验收命令：
  - `dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false`
  - `python tools\wowruntime-movement-matrix.py`
  - `python tools\wowruntime-matrix.py`
- 仍是未知/未完成字段：
  - 尚未采集真实移动中样本，仍需验证 `Speed > 0` 或 `ClickToMoveTypeRaw=4` 时 `InMovement=True`。
  - `MovementFlags` 仍不是完整 WoW movement flags，只是由 CTM 与速度聚合出的最小语义。
  - 对象 `Name` 仍为空，尚未进入对象名读取专项。
- 是否触碰导航：否。
- 是否触碰 RobotManager：否。
- 是否触碰 Product：否。

### 2026-07-06 第六刀：world-player 输出契约与三部门矩阵收口
- 本轮改动部门：`WorldState` CLI 输出契约、CLI 验收矩阵。
- 修复点：`PlayerSnapshot` 已携带 `ClickToMoveTypeRaw / ClickToMoveState`，但 `world-player` CLI 漏输出这两个字段，导致对象层矩阵契约失败。
- 现在 `world-player` 与 `world-snapshot` 的玩家字段保持一致，均输出：
  - `Movement`
  - `ClickToMoveTypeRaw`
  - `ClickToMoveState`
- 世界内样本实证：
  - `world-player` 输出 `MapId=571 MapIdKnown=True ContinentName="Northrend" Movement=None ClickToMoveTypeRaw=13 ClickToMoveState=None`。
  - `world-phase` 输出 `Phase=InWorld InGame=True LoadingOrConnecting=False`。
  - `movement-state` 输出 `InMovement=False ClickToMoveTypeRaw=13 ClickToMoveState=None SpeedKnown=True Speed=0`。
- 已通过验收命令：
  - `dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false`
  - `python tools\wowruntime-objectmanager-matrix.py --pid 6300`
  - `python tools\wowruntime-movement-matrix.py --pid 6300`
  - `python tools\wowruntime-matrix.py --pid 6300`
  - `python tools\wowruntime-worldphase-watch.py --pid 6300 --expect-phase InWorld --samples 3 --interval 0.5`
- 仍是未知/未完成字段：
  - 尚未采集真实移动中样本，仍需验证 `Speed > 0` 或 `ClickToMoveTypeRaw=4` 时 `InMovement=True`。
  - 对象 `Name` 仍为空，尚未进入对象名读取专项。
  - 登录界面、角色列表、加载地图状态已有 watch 工具，但仍需在实际切换状态时采样归档。
- 是否触碰导航：否。
- 是否触碰 RobotManager：否。
- 是否触碰 Product：否。

### 2026-07-06 第七刀：世界外状态自动采样与 Enter 推进验证
- 本轮改动部门：`WorldState` 验收工具。
- `tools/wowruntime-worldphase-watch.py` 已扩展为可选 Enter 推进工具：
  - 默认仍是纯观察，不发送按键。
  - 只有显式传入 `--send-enter-when-not-inworld` 时，才会在 `Phase != InWorld` 时向目标 Wow 主窗口发送 Enter。
  - 通过 `--enter-interval` 和 `--max-enters` 控制发送间隔与次数，避免无节制输入。
- 世界外状态实证链：
  - 初始观察：`Phase=LoginOrCharacterList InGame=False LoadingOrConnecting=False`。
  - 发送两次 Enter 后：`Phase=LoadingOrConnecting InGame=False LoadingOrConnecting=True`。
  - 后续只观察：`Phase=InWorld InGame=True LoadingOrConnecting=False`。
- 进世界后回归实证：
  - `movement-state` 输出 `Phase=InWorld ClickToMoveTypeRaw=13 ClickToMoveState=None SpeedKnown=True Speed=0`。
  - `python tools\wowruntime-matrix.py --pid 4120` 通过，ObjectManager / Scripting / Movement 三组均绿色。
- 已通过验收命令：
  - `python -m py_compile tools\wowruntime-worldphase-watch.py`
  - `dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false`
  - `python tools\wowruntime-worldphase-watch.py --pid 4120 --samples 12 --interval 1 --expect-phase InWorld --send-enter-when-not-inworld --enter-interval 2 --max-enters 2`
  - `python tools\wowruntime-worldphase-watch.py --pid 4120 --samples 40 --interval 1 --expect-phase InWorld`
  - `python tools\wowruntime-matrix.py --pid 4120`
- 仍是未知/未完成字段：
  - 对象 `Name` 仍为空，尚未进入对象名读取专项。
  - 尚未采集真实移动中样本，仍需验证 `Speed > 0` 或 `ClickToMoveTypeRaw=4` 时 `InMovement=True`。
- 是否触碰导航：否。
- 是否触碰 RobotManager：否。
- 是否触碰 Product：否。

### 2026-07-06 第八刀：GameObject 名称读取专项第一刀
- 本轮改动部门：`ObjectManager`、CLI 验收矩阵。
- 修复点：对象层原本把所有 `Name` 写死为空，导致附近邮箱、铁砧、炉子等物体只能看位置和距离，不能看名字。
- 已按 WRobot 源码核对 GameObject 名称链：
  - `WoWGameObject.Name` 来源：`ReadUInt32(base + 420) -> ReadUInt32(info + 144) -> ReadStringUTF8(namePtr, 80)`。
- 已按 WRobot 源码核对 Unit 名称链但未完整收口：
  - 本地玩家：WRobot 使用 `RebaseAddress(8887576)` 读取字符串，本地当前样本仍为空，暂不声明完成。
  - 普通 Unit：WRobot 使用 `base + 2404 -> DBCacheRow + 92 -> UTF8`，本轮只接入非破坏性读取，不把 Unit 名称作为验收门槛。
  - 非本地 Player：WRobot 使用 `GetPlayerName.GetName(guid)`，短期不接，避免引入缓存函数复刻范围。
- 在线样本实证：
  - 直接 CLI：`object-nearest --kind GameObject --radius 100 --pid 4120` 输出 `Name="邮箱"`。
  - GameObject 列表可读到 `邮箱`、`街灯`、`炉子`、`铁砧` 等名称。
- 新增验收脚本：
  - `tools/wowruntime-object-name-matrix.py`
  - 只调用 `object-nearest --kind GameObject`，断言最近 GameObject 的 `Name` 非空，不扫描大列表。
  - 已纳入 `tools/wowruntime-matrix.py` 聚合。
- 已通过验收命令：
  - `dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false`
  - `python tools\wowruntime-objectmanager-matrix.py --pid 4120`
  - `python tools\wowruntime-object-name-matrix.py --pid 4120`
  - `python tools\wowruntime-movement-matrix.py --pid 4120`
  - `python tools\wowruntime-matrix.py --pid 4120`
- 仍是未知/未完成字段：
  - 本地玩家 `Name` 当前仍为空，不能声明 Player 名称完成。
  - 普通 Unit 名称还未建立强验收样本。
  - Python 子进程在当前 GBK 控制台下显示中文可能被替换为 `?`，但直接 CLI 已证明运行时能读出中文名称；矩阵只断言非空，避免被控制台编码误伤。
- 是否触碰导航：否。
- 是否触碰 RobotManager：否。
- 是否触碰 Product：否。

### 2026-07-06 第九刀：Unit 名称验收与名称矩阵扩展
- 本轮改动部门：`ObjectManager` 验收矩阵。
- 结论：上一刀接入的 Unit 名称链已在当前在线样本中成立，不需要继续改运行时地址模型。
- 在线样本实证：
  - `object-nearest --kind Unit --radius 120 --pid 4120` 输出 `Name="萨布莉娜·哀凝"`。
  - `object-nearest --kind GameObject --radius 120 --pid 4120` 输出 `Name="邮箱"`。
- `tools/wowruntime-object-name-matrix.py` 已扩展：
  - 默认强制断言最近 GameObject 名称非空。
  - 支持 `--require-unit`，在当前这类附近有 Unit 的场景中强制断言最近 Unit 名称非空。
  - 聚合矩阵默认不强制 Unit，因为不是所有角色位置 120 码内都必有 Unit。
- 已通过验收命令：
  - `python tools\wowruntime-object-name-matrix.py --pid 4120 --require-unit`
  - `python -m py_compile tools\wowruntime-object-name-matrix.py tools\wowruntime-matrix.py`
  - `dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false`
  - `python tools\wowruntime-matrix.py --pid 4120`
- 注意点：
  - `object-list --kind Unit --limit 20` 返回 0 并不代表 Unit 名称失败；当前 `limit` 同时限制扫描深度，前 20 个对象里可能没有 Unit。
  - 后续需要把“扫描深度”和“返回数量”拆开，避免列表类命令因为早期对象分布误判。
- 仍是未知/未完成字段：
  - 本地玩家 `Name` 当前仍为空，不能声明 Player 名称完成。
  - 非本地 Player 名称依赖 WRobot `GetPlayerName.GetName(guid)` 的缓存/查询链，短期未复刻。
  - Python 子进程在当前 GBK 控制台下显示中文可能被替换为 `?`，但直接 CLI 已证明运行时能读出中文名称；矩阵只断言非空，避免被控制台编码误伤。
- 是否触碰导航：否。
- 是否触碰 RobotManager：否。
- 是否触碰 Product：否。

## 明确不做
- 不参考 `WR.Next`。
- 不进入导航设计。
- 不新增 Product / Quest / Gather / Combat 占位。
- 不把 `RuntimeHost` 升级成业务运行时。
- 不让 `RobotManager` 或未来 Product 直接依赖 `MemoryRobot` / Hook。

## Assumptions
- `WowRuntime` 是后续导航和 RobotManager 的唯一 WoW 语义依赖层。
- 导航设计必须等待世界状态、对象层、最小动作层稳定后再进入。
- 官方 wRobot Product 只作为长期参考，不作为本专项兼容目标。
