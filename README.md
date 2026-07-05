# SpellFireWPF_Plus

独立试验仓。

当前定位：

1. 承接 SpellFire 自研四大件蓝图后的新项目骨架
2. 不直接复制主仓历史包袱
3. 先建立 `MemoryRobot / RuntimeHost / WowRuntime / NavMesh` 的独立演进空间

当前阶段不做：

1. 不直接替换主仓运行链
2. 不混入旧注入主链和 UI 壳
3. 不把第三方 DLL 当成长期实现心脏

## 当前接力入口

`SpellFire.MemoryRobot` 已有专属 CLI 和 smoke，不再只借 `RuntimeHost` 验收底层内存能力。

当前推荐验收：

```powershell
dotnet build .\src\SpellFire.MemoryRobot.Cli\SpellFire.MemoryRobot.Cli.csproj -c Debug
powershell -ExecutionPolicy Bypass -File .\tools\memoryrobot-smoke.ps1 -SkipBuild
powershell -ExecutionPolicy Bypass -File .\tools\memoryrobot-failure-matrix.ps1 -SkipBuild
```

当前边界：

1. `MemoryRobot` 只负责明确目标进程的底层内存读写、模块、页面、远程分配、远程线程、LoadLibrary 基础能力。
2. `MemoryRobot` 不负责对象层、Lua、移动、产品/插件、HookReady 生命周期。
3. `MemoryRobot.Cli` 的成功 LoadLibrary/远程线程样本只在 CLI 自身进程执行，不污染 Wow。

详细接力文档：

1. `Documentation/待办事项/SpellFire.MemoryRobot_施工与审计专项_2026-07-05.md`
