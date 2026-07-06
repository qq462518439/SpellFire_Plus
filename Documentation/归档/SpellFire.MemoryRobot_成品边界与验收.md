# SpellFire.MemoryRobot 成品边界与验收

## 当前定位
`SpellFire.MemoryRobot` 是宿主可稳定依赖的底层组件，职责只停在：

- 进程会话
- 读写内存
- 远程分配/释放
- 远程线程
- 远程 `LoadLibrary/FreeLibrary`
- 模块快照
- 内存区查询

它不负责：

- WoW 对象层
- Lua 业务协议
- 导航
- 产品生命周期

## 宿主友好服务壳
本轮成品化新增 3 个服务：

- `ProcessAttachService`
- `ProcessSnapshotService`
- `RemoteExecutionService`

它们只负责底层原语编排，不引入业务语义。

## 正式验收入口
`SpellFire.MemoryRobot.Cli` 是本体专属验收入口。

核心命令：

- 会话：`probe` `session-open-close` `close-then-reopen` `snapshot-after-close` `session-close-all` `process-exit-after-open`
- 快照：`module-snapshot` `memory-region`
- 内存：`remote-alloc-free` `write-remote-allocation` `try-read-invalid`
- 远程执行：`remote-thread-invalid-start` `load-library-missing-file` `self-remote-thread-get-current-process-id` `self-load-library-known-system-dll` `self-free-library-known-system-dll` `self-load-then-free-library-known-system-dll`

## Python 回归
一键入口：

```powershell
python .\tools\memoryrobot-matrix.py
```

子矩阵：

- `memoryrobot-session-matrix.py`
- `memoryrobot-memory-matrix.py`
- `memoryrobot-remoteexec-matrix.py`
