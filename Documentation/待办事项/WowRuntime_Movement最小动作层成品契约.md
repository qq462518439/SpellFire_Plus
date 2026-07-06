# WowRuntime Movement 最小动作层成品契约

## 结论
`Movement` 当前成品边界是可验证的最小动作层，不是导航层，也不是 CTM 写入层。它只提供键盘动作原语、状态读取、只读 CTM 诊断，并用 CLI/脚本证明动作确实产生坐标位移或朝向变化。

## 已成品能力
- `Jump`
- `StartMoveForward`
- `StartMoveBackward`
- `StartStrafeLeft`
- `StartStrafeRight`
- `StartTurnLeft`
- `StartTurnRight`
- `StopTurn`
- `StopMove`
- `StopMoveTo`
- `GetMovementState`
- `GetClickToMoveDiagnostic`

## 正式 CLI
- `movement-jump`
- `movement-forward-start`
- `movement-backward-start`
- `movement-strafe-left-start`
- `movement-strafe-right-start`
- `movement-turn-left-start`
- `movement-turn-right-start`
- `movement-turn-stop`
- `movement-stop`
- `movement-stop-to`
- `movement-state`
- `movement-ctm-diagnostic`
- `movement-speed-sample --action forward`
- `movement-speed-sample --action backward`
- `movement-speed-sample --action strafe-left`
- `movement-speed-sample --action strafe-right`

## 成品证据
- 四向移动必须通过 `movement-speed-sample` 证明。
- 验收必须同时检查：
  - `Action`
  - `Moved=True`
  - `Distance > 0.3`
  - `ComputedSpeed > 0.3`
  - `StateSpeedKnown`
  - `StateSpeed`
  - 方向对应的 `Detail`
- `StateSpeedKnown / StateSpeed` 只能作为辅助观测字段，不能单独证明平移移速。
- 转向必须通过 `world-player Rotation` 数值变化证明。
- `movement-stop` 必须同时停止前进、后退、左右平移、上升、左右转。

## 明确后置
- `FaceTo(Vector3)`
- `FaceObject(ulong guid)`
- `Go(IReadOnlyList<Vector3>)`
- CTM 写入
- 路径规划
- 导航
- 产品调度

## 禁止误判
- Lua ACK 只能证明脚本执行，不等于移动成功。
- 转向/镜头变化不能证明移速。
- `ClickToMoveTypeRaw=13` 不代表角色没有通过键盘移动。
- 只有位置位移成立，才允许 `movement-speed-sample` 声明平移移速成立。
- `SpeedMoving > 0` 只能说明动作期状态字段变化，不能替代 `Distance / ComputedSpeed`。
- 世界外、加载中、角色列表状态下，动作命令可以返回 Lua 成功，但 Movement live 验收不得宣称成品通过。

## 验收入口
```powershell
dotnet build .\src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj -c Debug -p:UseSharedCompilation=false
python tools\wowruntime-movement-contract-guard.py
python tools\wowruntime-movement-matrix.py
python tools\wowruntime-matrix.py
python tools\runtime-facade-matrix.py
```

## 自动守卫
`tools/wowruntime-movement-contract-guard.py` 必须检查：
- `IMovementService` 暴露四向动作和停止动作。
- `ScriptMovementService` 的 `FaceTo / FaceObject / Go` 继续返回 `FeatureUnavailable`。
- `ScriptMovementService.GetClickToMoveDiagnostic()` 继续输出 `ReadOnly=True CtmWriteKnown=False`。
- `movement-speed-sample` 支持四个合法动作。
- `wowruntime-movement-matrix.py` 断言四向动作的 `Detail`、`Action`、`Moved=True`、`Distance > 0.3`、`ComputedSpeed > 0.3`。
