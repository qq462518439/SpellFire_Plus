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
python tools\wowruntime-matrix.py
python tools\runtime-facade-matrix.py
```

## 每轮施工记录要求
每轮施工结束必须记录以下内容：

- 本轮改了哪个部门。
- 哪些 CLI 命令通过。
- 哪些字段仍是未知。
- 是否触碰导航、RobotManager、Product。预期答案必须是否。

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
