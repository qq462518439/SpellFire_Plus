# MemoryRobot 核心能力成品化 后置专项

## 当前状态

`SpellFire.MemoryRobot` 是当前最稳的底层能力来源，已覆盖：

1. 进程会话
2. 模块枚举
3. 内存读写
4. 内存区域查询
5. 远程分配/释放
6. 远程线程
7. LoadLibrary
8. CLI / smoke 验收

## 后置原因

当前不继续把 MemoryRobot 打磨成发布级成品，避免又进入项目整理和 API 审计循环。

## 后置验收项

1. 冻结 MemoryRobot 只做进程/内存底层能力。
2. 禁止对象层、Lua、运动、产品生命周期进入 MemoryRobot。
3. 审计 `IMemoryRobot`、reader、writer、session、allocator、thread、library、module、region API。
4. 固定 `memoryrobot-smoke.ps1` 和 `memoryrobot-failure-matrix.ps1`。
5. 输出一页短 README，说明能力边界和验收命令。

## 当前冻结边界

1. 暂不继续重构 MemoryRobot API。
2. 暂不做发布包装。
3. 暂不把业务 Runtime 能力下沉进 MemoryRobot。
4. 不把 MemoryRobot 成品化混入下一条主线。
