# LuaBridge 后置专项

## 当前状态

`Lua冒烟` 已证明可以走真实 Hook 命令通道触发：

```text
JumpOrAscendStart();
DEFAULT_CHAT_FRAME:AddMessage("SPELLFIRE_LUA_OK");
```

当前不继续把 LuaBridge 扩成主线。

## 2026-07-06 验收补充

新增 Runtime facade 矩阵后，LuaBridge 通过了上层 facade 入口验收：

```powershell
python .\tools\runtime-facade-matrix.py
```

已确认：

1. `RuntimeFacade.LuaSmoke(processId)` 返回 `LuaSmokeExecuted`。
2. `RuntimeFacade.ExecuteLua(processId, script)` 返回 `LuaExecuteSucceeded`。
3. 输出包含 `LuaBridgeReady=True`。
4. 输出包含 `TextPayload=OK:FrameScriptExecute=0`。

当前结论：

LuaBridge 已可作为 Runtime facade 的已验证能力被调用，但仍不扩展为对象层、移动层或产品层主线。

## 后置原因

1. LuaBridge 已完成一次冒烟突破，但稳定性还需要单独专项验证。
2. 当前主线不应继续扩 Lua 命令、对象层、运动层。
3. EndScene / 主线程执行桥属于高风险底层能力，必须独立审计。

## 后置验收项

1. 多次点击 `Lua冒烟` 不崩。
2. 关闭 RuntimeHost 不残留异常状态。
3. 新 Wow 进程重复 Attach / LuaSmoke 结果稳定。
4. 日志能显示明确失败码，而不是只显示失败。
5. 禁止 `SendInput`、粘贴、宏、`/run print` 伪冒烟。

## 当前冻结边界

1. 保留 `Attach` 和 `Lua冒烟` 两个测试入口。
2. 不新增任意 Lua 输入框。
3. 不把 LuaBridge 接入主程序。
4. 不基于 LuaBridge 继续扩对象层、运动层或产品层。
