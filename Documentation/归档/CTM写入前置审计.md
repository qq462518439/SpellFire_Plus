# CTM 写入前置审计

## 结论

当前不能直接声称有 Navigation 执行层；但 CTM native command 与 `Movement.Go` 单点 CTM 已经通过自动验证。

审计结果表明：`wRobot` 的 CTM 移动不是简单写入全局 X/Y/Z 字段，而是通过进程内 Hook 调用 WoW 原生函数 `CGPlayer_C__ClickToMove`。

因此 SpellFire_Plus 的真实前置条件应修正为：

`Hook native-call 通道 -> CTM native command -> Movement.Go 最小短距离样本 -> Navigation 最小执行`

在 `Movement.Go` 仍只是单点 CTM 的前提下，Navigation 主体必须后置到路径队列、到达判定、卡住判定和停止语义定义完成之后。

## 已核实事实

### 1. 本仓当前 CTM 状态

文件：

- `src/SpellFire.WowRuntime/World/WorldAddressTable.cs`
- `src/SpellFire.WowRuntime/World/StaticWorldAddressProvider.cs`
- `src/SpellFire.WowRuntime/Infrastructure/ScriptMovementService.cs`

事实：

- `WorldAddressTable` 只有 `ClickToMoveType`。
- `StaticWorldAddressProvider` 只有 `ClickToMoveType = 0x008A11F4`。
- 没有 CTM 目标 X/Y/Z 写入字段。
- 没有 CTM action/type 触发字段。
- 没有 CTM stop/recovery 写入模型。
- `GetClickToMoveDiagnostic()` 仍保留诊断输出，历史样本曾输出 `ReadOnly=True CtmWriteKnown=False`。
- `Movement.Go()` 当前消费 Runtime facade 的 CTM 白名单命令，只取第一个目标点执行单点 CTM。

结论：

- 本仓已有可直接用于单点移动的 CTM native command，但还没有导航级路径执行能力。

### 2. wRobot ClickToMove 参考形状

反编译来源：

- `C:\Users\ASUS\Documents\RZB\Bin\wManager.dll`
- 类型：`wManager.Wow.Helpers.ClickToMove`

关键事实：

- `GetClickToMoveTypePush()` 读取 `9048564`，rebase 后即 `0x8A11F4`。
- `GetClickToMovePosition()` 读取：
  - `9048676`
  - `9048680`
  - `9048684`
- `CGPlayer_C__ClickToMove(float x, float y, float z, ulong guid, int action, float precision)` 会：
  - 分配 12 字节坐标缓冲。
  - 分配 8 字节 GUID 缓冲。
  - 写入 guid / x / y / z。
  - 通过 `InjectAndExecute` 调用 WoW 内部 `CGPlayer_C__ClickToMove`。
  - action `4` 表示 Move。

结论：

- `0x8A11F4` 是 CTM 状态读取，不是完整移动写入模型。
- CTM 移动关键不是写全局坐标，而是进程内调用原生函数。

### 3. wRobot MovementManager 参考形状

反编译来源：

- `C:\Users\ASUS\Documents\RZB\Bin\wManager.dll`
- 类型：`wManager.Wow.Helpers.MovementManager`

关键事实：

- `MovementManager.MoveTo(Vector3 point)` 只是设置当前 MoveTo 目标。
- 后台移动线程检查：
  - 当前目标点。
  - `ClickToMove.GetClickToMovePosition()`。
  - `ClickToMove.InMove`。
- 如果目标不一致或当前不在 CTM Move，则调用：

```text
ClickToMove.CGPlayer_C__ClickToMove(vector.X, vector.Y, vector.Z, 0, 4, 0.5f)
```

结论：

- wRobot 的 MoveTo 是基于 CTM native call 的循环控制，不是直接写静态坐标字段。

### 4. SpellFire Hook 当前能力

文件：

- `src/SpellFire.Hook/src/HookProtocol.h`
- `src/SpellFire.Hook/src/dllmain.cpp`
- `src/SpellFire.RuntimeHost/Components/HookProtocol.cs`
- `src/SpellFire.RuntimeHost/Components/HookCommandChannel.cs`

当前命令：

- Ping
- GetHookInfo
- ReadSelfModule
- LuaSmoke
- ExecuteLua

事实：

- 已有 main-thread / EndScene 执行桥。
- 已能执行 Lua。
- 尚无 native call 命令。
- 尚无 CTM 命令。
- 尚无参数结构承载 x/y/z/guid/action/precision。

结论：

- SpellFire Hook 可以作为 CTM native call 的正确地基，但需要扩协议。
- 不能绕过 Hook 在宿主侧直接写内存伪造 CTM。

## 修正后的阶段顺序

### P1A：Hook native-call 能力审计与协议扩展

目标：

- 让 Hook 协议能承载一个明确白名单 native command。

只允许实现：

- `ClickToMoveMove`
- `ClickToMoveStop` 如能确认原生 stop 地址，否则继续用 Lua/StopMove 兜底并标明不是原生 CTM stop。

禁止：

- 任意地址调用器。
- 通用 shellcode 执行器。
- 对外暴露“任意函数调用”。

### P1B：CTM native command 最小实现

目标：

- 在 Hook 内部执行 `CGPlayer_C__ClickToMove(x,y,z,guid,action,precision)`。

最小输入：

- `x`
- `y`
- `z`
- `guid`
- `action`
- `precision`

最小输出：

- `Ack`
- `Status`
- `Result`
- `LastNativeStatus`
- `ClickToMoveTypeBefore`
- `ClickToMoveTypeAfter`

### P1C：Movement.Go 短距离样本

目标：

- 只移动到当前位置附近极短距离。
- 证明角色真实位移。
- 结束后必须 Stop。

输出必须包含：

- `StartPos`
- `TargetPos`
- `EndPos`
- `Moved`
- `DistanceMoved`
- `DistanceToTarget`
- `Arrived`
- `StopIssued`

### P1D：Navigation 最小执行

前置：

- P1A-P1C 全部通过。

目标：

- 才允许实现 `navigation-move-to-sample`。

## 停工条件

以下任一情况必须停工：

- 无法确认 `CGPlayer_C__ClickToMove` 地址。
- Hook native command 触发客户端崩溃。
- CTM command Ack 成功但角色无位移。
- Stop 后角色仍失控。
- LuaSmoke / ExecuteLua 退化。
- ObjectManager / WorldState 退化。

## 当前下一刀

下一刀不是 Navigation。

下一刀是：

`P1A：Hook native-call 能力审计与协议扩展`

第一步：

- 在 `SpellFire.Hook` 中增加一个白名单 CTM 命令，而不是通用 native call。

## 2026-07-06 实测进展：白名单 CTM 命令已进入 Hook，但 native 调用仍失败

已完成：

- `SpellFire.Hook` 协议新增白名单命令 `ClickToMoveMove = 6`。
- RuntimeHost CLI 新增 `ctm-move <pid> <x> <y> <z> [guid] [action] [precision]`。
- Runtime facade 新增 `ClickToMoveMove(...)`。
- Hook 使用 EndScene 主线程桥执行 CTM，不从 worker 线程直接调用 WoW 函数。
- Hook、RuntimeHost.Cli、WowRuntime.Cli 均已编译通过。
- 旧 Hook 可 shutdown，新的 Hook payload 可重新 attach。

已验证：

```text
hook-info       -> HookInfoOk
read-self-module -> HookSelfModuleReadOk
lua-smoke       -> LuaSmokeExecuted
ctm-move 非世界状态 -> CTMV + ERR:ActivePlayerObjectMissing
```

世界内短距离样本：

```text
StartPos=(5939.453,624.744,650.647)
TargetPos=(5940.953,624.744,650.647)
ctm-move Result=CTMV Status=Failed TextPayload=ERR:CTM:0xC0000005
MovementState=InMovement=False ClickToMoveTypeRaw=13 Speed=0
EndPos=(5939.453,624.744,650.647)
StopMove=LuaExecuteSucceeded
Moved=False
```

结论：

- CTM 命令通道已经不是旧 Hook 的 UnsupportedCommand，确实进入新 Hook。
- `ActivePlayerObjectMissing` 场景能安全失败。
- 世界内调用 `CGPlayer_C__ClickToMove` 触发 `0xC0000005`，但被 SEH 捕获，客户端未崩溃，Stop 正常。
- 因此当前缺口不是协议通道，而是 `CGPlayer_C__ClickToMove` 的函数入口、调用桩或调用约定仍未完全匹配。

已排除：

- 不是 Hook 未就绪。
- 不是 Lua 主线程桥未就绪。
- 不是 RuntimeHost CLI 未接通。
- 不是 Vector3 直接传 32 字节命令结构的问题；已改为独立 12 字节 `CtmVector3` 后仍为 `0xC0000005`。

下一步：

- 停止 Navigation。
- 继续专项审计 Well/wRobot 的 CTM 调用桩。
- 重点确认 `0x727400` 是否能直接作为 `__thiscall bool(this, int, guid*, vec3*, float)` 调用，还是需要额外 trampoline/栈布局/对象上下文。
- 在未证明 native call 可安全移动前，`Movement.Go` 必须继续拒绝。

## 2026-07-06 追加实测：地址正确，但直接 native call 仍不可用

本轮新增验证：

- 反混淆 wRobot `CallWrapperCode`，确认 CTM 注入片段形态：
  - `call activePlayer`
  - `test eax, eax`
  - `je @out`
  - `mov ecx, eax`
  - `push precision`
  - `push positionPtr`
  - `push guidPtr`
  - `push action`
  - `call clickToMove`
  - `@out:`
  - `retn`
- 反混淆 wRobot 签名：
  - CTM 函数签名：`55 8B EC 83 EC 18 53 8B D9 8B 43 08`
  - ActivePlayer 包装签名：`E8 ? ? ? ? 68 ? ? ? ? 68 ? ? ? ? 6A 10 52 50 E8 ? ? ? ? 83 C4 14 C3`
- 只读 dump 目标进程内机器码，确认：
  - `0x727400` 命中 CTM 函数签名。
  - `0x4038F0` 命中 ActivePlayer 包装签名。

已尝试但未通过：

- CTM 地址从硬编码改为 wRobot 同款签名定位，运行时仍解析到 `0x727400`，短距离样本仍失败：

```text
ctm-move Target=(5908.232,636.781,647.07)
TextPayload=ERR:CTM:0xC0000005 Player=0x23BD7068 Fn=0x00727400
MovementState=InMovement=False ClickToMoveTypeRaw=13 Speed=0
EndPos 未产生 CTM 位移
StopMove=LuaExecuteSucceeded
```

- 将 guid/position 从栈局部变量改为 Hook 模块内稳定存储，模拟 wRobot `AllocData` 生命周期，仍失败。
- 尝试把 `call activePlayer` 放进同一段 asm 复刻 wRobot 连续片段，结果 `Player=0x00000000`，说明该包装入口不能在当前 Hook asm 形态下这样复用；此方向已回退。

当前结论：

- `0x727400` 不是假地址，CTM 入口签名正确。
- `0x4038F0` 不是假地址，取玩家包装签名正确。
- Hook/Lua/EndScene 主线程桥可用，StopMove 可用，客户端未因 CTM 失败崩溃。
- 直接把 CTM 入口当 `__thiscall bool(this, action, guid*, pos*, precision)` 调用仍会触发 `0xC0000005`。

新的停工点：

- 不允许进入 Navigation。
- 不允许实现 `Movement.Go`。
- 不允许让用户人工冒烟 CTM。
- 下一步必须继续定位 wRobot 注入执行环境差异，例如 Hook trampoline、返回点、帧锁/硬件断点保护、调用前后的寄存器/栈状态，而不是继续猜坐标或参数顺序。

## 2026-07-06 追加实测：根因修正为命令结构体对齐，CTM 已自动位移成功

本轮新增证据：

- `ClickToMove.smethod_0()` 反混淆日志为 `Avoid ctm crash`。
- 该方法会在 `CGPlayer_C__ClickToMove(...)` 前尝试安装硬件断点：
  - 断点地址：`RebaseAddress(3306547)`。
  - 命中后如果 `DetourInUse()` 且 `Eax != 3`，将 `Eip` 改到 `RebaseAddress(3306561)`。
  - 异常日志名：`CTMSecure EVENT` / `CTMSecure`。
- 这说明 wRobot 已知 CTM 另有防崩保护，但本轮实测最终根因不是必须先复刻 HWBP，而是 SpellFire Hook 命令结构体布局错位。

本轮自动样本：

```text
PID=9080
StartPos=(5923.302,627.581,646.481)
TargetPos=(5924.802,627.581,646.481)
ctm-move Ready=True Reason=ClickToMoveCommandSucceeded
TextPayload=OK:CGPlayer_C__ClickToMove
MovementState=InMovement=True ClickToMoveTypeRaw=4 ClickToMoveState=Move Speed=7
EndPos=(5924.454,627.647,645.967)
StopMove=LuaExecuteSucceeded
```

解释：

- Hook 侧 `ClickToMoveCommand` 原本含 `unsigned long long Guid`，C++ 默认对齐会把 `Guid` 放到 offset 16。
- C# 命令通道按 offset 12 写入 `Guid`、offset 20 写入 `Action`、offset 24 写入 `Precision`。
- 因此 Hook 侧读取到的 `Action` 实际是 `Precision=0.5f` 的位模式 `0x3F000000`，导致 CTM 内部函数在 `0x00715C81` 访问错误表项并触发 `0xC0000005`。
- 修复方式：Hook 侧 `ClickToMoveCommand` 使用 `#pragma pack(push, 1)`，让 C++ 命令结构体布局与 C# 写入布局一致。
- 修复后 CTM 命令返回成功，MovementState 在移动中采样到 `ClickToMoveTypeRaw=4`、`Speed=7`，玩家坐标产生真实位移。

当前状态：

- CTM 白名单命令已通过自动短距离位移验证。
- Hook/Lua/WorldState/MovementState 未退化。
- 客户端未崩溃，StopMove 可正常执行。
- `Avoid ctm crash` 仍作为后续兼容/安全参考保留，但不再是当前 CTM Move 的阻塞项。

下一步建议：

- 保留 `tools/wowruntime-ctm-matrix.py`，固定“当前坐标 + 安全偏移 -> CTM -> MovementState -> Stop”的验收。
- `WowRuntime Movement.Go` 已改为消费 Runtime facade 的 CTM 白名单命令。
- 进入 Navigation 最小执行前，先定义路径队列、到达判定、卡住判定和 Stop/Cleanup 责任。
