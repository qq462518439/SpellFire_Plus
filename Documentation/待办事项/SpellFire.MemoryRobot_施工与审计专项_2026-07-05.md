# SpellFire.MemoryRobot 施工与审计专项

## 当前结论

`SpellFire.MemoryRobot` 应作为四大件中的第一刀，且必须先于 `RuntimeHost / WowRuntime / NavMesh` 开工。

它的定位不是：

1. 新的 `Well`
2. 新的 `wManager`
3. 产品容器
4. 插件宿主

它的定位就是：

1. 进程内存底座
2. 远程地址空间操作层
3. 低层汇编/patch 能力承载层

当前真实建议：

1. 先做最小内存底座 MVP
2. 从 `Well` 抽通用底层，不照搬整仓
3. 暂时不掺产品、插件、WoW 语义

## 当前代码真实状态

当前已经成立的代码事实：

1. `SpellFire.MemoryRobot` 已建立独立项目
2. 已按基础分层落下目录：
   - `Abstractions`
   - `Native`
   - `Process`
   - `Reading`
   - `Writing`
   - `MemoryMap`
   - `Diagnostics`
   - `Assembly`
3. 已有 `MemorySession`
4. 已有 `MemoryReader / MemoryWriter`
5. 已有 `RemoteAllocator`
6. 已有 `RemoteThreadRunner`
7. 已有 `RemoteLibraryLoader`
8. 已有 `ProcessModuleSnapshotProvider`
9. 已有 `MemoryRegionQueryService`
10. 已有 `MemoryRobotFacade`
11. 已有 `MemoryRobotException`
12. 已有 session/权限诊断第一刀：
   - `MemorySessionDiagnostics`
   - `MemorySessionProbeResult`
13. `RuntimeHost.Cli memory-probe <pid>` 已能输出：
   - `ProcessFound`
   - `ProcessName`
   - `Responding`
   - `HostX64OS`
   - `HostX64Process`
   - `TargetWow64Known`
   - `TargetWow64`
   - `RequestedAccess`
   - `Win32Error`
   - `Win32Message`

当前定级：

1. `SpellFire.MemoryRobot` 属于 **成型地基**

不能升级为“可信成品”的原因：

1. 还没有 session cache / session manager
2. 还没有多进程长期运行压力证据
3. 还没有把读写 API 做成稳定公共验收面
4. 还没有进程退出中等竞态样本矩阵

已完成的失败样本矩阵：

1. 无进程：`ProcessUnavailable`
2. 系统进程权限拒绝：`AccessDenied`
3. 64 位目标进程：`TargetNot32Bit`

当前明确不是：

1. 业务运行时
2. 对象层
3. Lua 层
4. 移动层
5. 产品/插件容器

## 开工前总约束

这不是只服务单进程、单按钮、单样本的玩具底层。

开工前必须同时满足下面几个方向：

1. 多进程兼容
2. 规范分层
3. 在够用基础上保留扩展可能性
4. 以现有可编译源码树作为骨架参考
5. 不制造凭空想象的新轮胎

这意味着第一刀虽然小，但设计口径不能小气。

## 四大件之间的真实依赖关系

推荐依赖方向：

1. `SpellFire.RuntimeHost -> SpellFire.MemoryRobot`
2. `SpellFire.WowRuntime -> SpellFire.MemoryRobot`
3. `SpellFire.WowRuntime -> SpellFire.RuntimeHost`：按需
4. `SpellFire.NavMesh`：尽量独立，不依赖前三层
5. 上层聚合层后置决定是否存在

禁止反向依赖：

1. `SpellFire.MemoryRobot` 禁止依赖 `RuntimeHost`
2. `SpellFire.MemoryRobot` 禁止依赖 `WowRuntime`
3. `SpellFire.MemoryRobot` 禁止依赖 `NavMesh`
4. `SpellFire.NavMesh` 禁止依赖 `WowRuntime`
5. 产品/插件禁止直接依赖 `MemoryRobot`

结论：

1. `MemoryRobot` 不是上层的“通天总线”
2. 它只是全体系的底层地基
3. 这层一旦反向吃语义层，后面会整仓返工

## 多进程兼容要求

`SpellFire.MemoryRobot` 第一版就必须按多进程场景设计，而不是默认只有一个 `WowMemory` 全局静态实例。

必须满足：

1. 一个进程对应一个独立 `MemorySession`
2. 句柄、模块、页面信息都绑定到具体进程实例
3. 不允许用全局静态单例缓存当前目标进程
4. 允许未来同时存在多个活动 session

不建议出现：

1. `CurrentProcessMemory`
2. `GlobalMemory`
3. `WowMemory` 式单一全局入口

推荐形态：

1. `MemorySession`
2. `MemoryReader`
3. `MemoryWriter`
4. `MemorySessionCache`：可选，仅服务底层 session 复用

这里必须区分两类“管理”：

1. `MemoryRobot` 内允许存在“底层 session 复用”
2. 业务级进程扫描、选择、切换、挂产品、挂插件，不属于 `MemoryRobot`

也就是说：

1. `MemoryRobot` 只负责“如何操作一个具体进程”
2. `RuntimeHost` 或宿主上层才负责“当前有哪些进程、每个进程挂什么”
3. 不靠静态状态串全局

## 规范分层要求

第一版就要明确“哪类代码属于哪一层”，不然后面很快又会堆成一个大类。

### 底层 native 层

只放：

1. P/Invoke
2. flags
3. native structs

### session 层

只放：

1. 打开/关闭进程
2. 句柄生命周期
3. 进程元信息

### read/write 层

只放：

1. 读写
2. marshalling
3. 字符串/结构体转换

### memory map 层

只放：

1. `VirtualQueryEx`
2. 区段模型
3. 区段枚举

### assembly 层

只放：

1. assembler 抽象
2. 指令编码抽象

### diagnostics 层

只放：

1. 错误模型
2. 调试快照
3. 操作结果账本

## 扩展可能性要求

第一刀虽然不做产品/插件，但必须为后续扩展留下健康接口。

要保留的扩展位：

1. assembler 抽象
2. 远程分配抽象
3. 模块枚举抽象
4. session manager
5. 错误/诊断结果对象

暂时不做，但要避免堵死的扩展位：

1. 远程线程执行
2. trampoline/code cave
3. Hook 注入
4. 运行时 ready 状态
5. 产品/插件容器

换句话说：

1. 先不实现
2. 但类边界和接口命名要允许将来往上长

## 参考骨架怎么用

你手里的 `Tools/wManager_clean_project` 不能当 `MemoryRobot` 源码直接骨架，但可以当“上层消费模式参考”。

它更适合回答：

1. 上层未来希望从内存层拿到什么能力
2. `Memory.WowMemory.Memory` 一类入口最终会被怎样消费
3. 哪些底层能力会支撑 `ObjectManager / MovementManager / Lua / PathFinder`

它不适合回答：

1. `MemoryRobot` 第一版类结构应该长什么样
2. `MemoryRobot` 的 native 层该怎样拆

真正的参考关系应该是：

1. `Well` 提供底层实现来源
2. `wManager_clean_project` 提供上层消费样式参考

## 不要自造假轮子

这点必须写清楚。

不能因为要“自研”，就凭空发明全新的抽象体系去代替成熟常见做法。

应该优先复用的常见思路：

1. `MemorySession`
2. `MemoryReader / MemoryWriter`
3. `RemoteAllocation`
4. `ModuleSnapshot`
5. `MemoryRegion`
6. `IAssembler`

不建议自造的东西：

1. 过度玄学命名
2. 单类包天下
3. 难以替换的静态全局中心
4. 为了“看起来原创”而故意绕开成熟分层

## 与产品和插件的长期关系

产品和插件是长期要考虑的，但它们不属于 `MemoryRobot` 第一刀。

长期健康关系：

1. `MemoryRobot` 提供底层能力
2. `RuntimeHost` 提供运行时上下文与生命周期
3. 产品/插件容器挂在 `RuntimeHost` 或更上层

这意味着：

1. `MemoryRobot` 要支持多 session
2. `RuntimeHost` 未来才能在多进程上挂多个产品实例
3. 产品/插件层不该直接拿裸句柄四处读写

## 对新仓的真实建议

`SpellFireWPF_Plus` 里不应一上来把四大件一起开工。

更稳的顺序：

1. 先把 `SpellFire.MemoryRobot` 做对
2. 再让 `RuntimeHost` 吃它
3. 再让 `WowRuntime` 吃前两层
4. 最后才考虑产品和插件如何挂接

这样做的收益：

1. 多进程模型不会后面才补
2. 分层会天然更干净
3. 上层不会反向把底层拖歪

## 它到底负责什么

`SpellFire.MemoryRobot` 第一阶段只负责：

1. 打开/关闭目标进程
2. 维护有效进程句柄
3. 读取内存
4. 写入内存
5. 枚举模块
6. 查询页面信息
7. 分配/释放远程内存
8. 修改页面保护

第二阶段才考虑：

1. 代码洞
2. trampolines
3. 远程调用辅助
4. assembler 抽象

不负责：

1. 对象层
2. Lua
3. ObjectManager
4. MovementManager
5. 产品生命周期
6. 插件发现/加载
7. 导航网格

## 可直接参考的现有资产

当前最像 `MemoryRobot` 的参考，不在 `wManager_clean_project` 上层语义代码里，而在 `Well` 现有底层里：

1. `Well/Util/Memory.cs`
2. `Well/Util/SystemWin32.cs`
3. `Well/Warden/PageCheckHook.cs`
4. `Well/Warden/WardenBuster.cs`
5. `Well/Controller/RemoteMain.cs`
6. `Well/Controller/ControlInterface.cs`

其中第一刀直接相关的是：

1. `Memory.cs`
2. `SystemWin32.cs`

## 现有参考的真实优缺点

### `Well/Util/Memory.cs`

真实优点：

1. 已经有基础读写能力
2. 已经有字符串读取
3. 已经有结构体读取
4. 代码短，便于第一批抽取

真实问题：

1. 只是一把大锤，没有清晰接口分层
2. `PROCESS_ALL_ACCESS` 太粗
3. 没有显式释放句柄
4. 没有错误模型
5. 没有模块快照/页面信息抽象
6. 32 位指针处理写死味道较重

结论：

1. 它适合作为第一版参考
2. 不适合原样迁移

### `Well/Util/SystemWin32.cs`

真实优点：

1. 已有不少底层 P/Invoke
2. 覆盖了当前内存层和页面层最基本的 API

真实问题：

1. 混了大量不属于 `MemoryRobot` 的窗口消息/UI API
2. 常量、委托、结构体全堆在一起
3. 缺少按领域拆分

结论：

1. 它适合作为 native 声明来源
2. 不应整文件直接搬入 `SpellFire.MemoryRobot`

## 第一阶段项目结构建议

建议目录：

1. `Abstractions/`
2. `Native/`
3. `Process/`
4. `Reading/`
5. `Writing/`
6. `MemoryMap/`
7. `Diagnostics/`
8. `Assembly/`

### `Abstractions/`

建议首批接口：

1. `IMemorySession`
2. `IMemoryReader`
3. `IMemoryWriter`
4. `IRemoteAllocator`
5. `IModuleSnapshotProvider`

### `Native/`

建议首批内容：

1. `Kernel32Native`
2. `User32Native` 不进入第一刀
3. `ProcessAccessFlags`
4. `MemoryProtection`
5. `MemoryState`
6. `MemoryBasicInformation`

### `Process/`

建议首批内容：

1. `MemorySession`
2. `RemoteAllocation`
3. `ProcessModuleInfo`

### `Reading/`

建议首批内容：

1. `MemoryReader`
2. `StructMarshaller`
3. `StringReader`

### `Writing/`

建议首批内容：

1. `MemoryWriter`
2. `ProtectionScope` 如有必要

### `MemoryMap/`

建议首批内容：

1. `MemoryRegion`
2. `MemoryRegionQueryService`

### `Assembly/`

第一阶段只放抽象：

1. `IAssembler`
2. `IInstructionEncoder`

先不做真实 FASM 接入。

## 第一刀最小能力清单

第一刀必须交付：

1. `OpenProcess`
2. `CloseHandle`
3. `ReadBytes`
4. `WriteBytes`
5. `Read<T>`
6. `Write<T>`
7. `ReadString`
8. `VirtualQueryEx`
9. `VirtualAllocEx`
10. `VirtualFreeEx`
11. `VirtualProtectEx`
12. `GetProcessModules`

第一刀不交付：

1. 远程线程启动
2. 汇编拼装
3. 代码注入壳
4. Hook 容器
5. WoW 偏移封装

## 与产品/插件扩展的关系

`SpellFire.MemoryRobot` 不直接承接产品和插件。

正确扩展链路应是：

1. `SpellFire.MemoryRobot`
2. `SpellFire.RuntimeHost`
3. 产品容器 / 插件容器

也就是说：

1. 产品和插件不该直接吃 `MemoryRobot` 底层 API
2. 产品和插件应该吃 `RuntimeHost` 暴露的更高层运行时上下文
3. `MemoryRobot` 只提供底层能力，不承担扩展协议

否则会出现的问题：

1. 插件直接写内存，边界失控
2. 产品层反向绑定底层实现细节
3. 后续很难替换 Hook/Assembler 方案

## `fasmdll_managed.dll` 在本专项中的口径

本专项中它只是一种候选底层依赖，不是第一刀前提。

当前结论：

1. `SpellFire.MemoryRobot` 第一阶段不必强绑 `fasmdll_managed.dll`
2. 先把 assembler 抽象接口留出来
3. 未来真做机器码拼装时，再决定：
   - 继续用 FASM
   - 包一层 `fasmdll_managed.dll`
   - 或改成自有机器码模板

因此当前施工纪律是：

1. 不因为 `fasmdll_managed.dll` 存在，就提前污染第一刀
2. 也不把它包装成 `MemoryRobot` 的天然核心

## 第一刀可抽取资产清单

可优先抽取：

1. `Well/Util/Memory.cs`
   - 只抽读取/写入思路
   - 不原样照搬类设计
2. `Well/Util/SystemWin32.cs`
   - 只抽 `kernel32` 相关内存 API
   - 不带 UI/window message 相关内容

可后置参考：

1. `Well/Warden/PageCheckHook.cs`
2. `Well/Warden/WardenBuster.cs`
3. `Well/Controller/RemoteMain.cs`
4. `Well/Controller/ControlInterface.cs`

它们当前不应进入第一刀的原因：

1. 已经混入 Hook/反检测/远程壳语义
2. 超出单纯内存底座范围
3. 容易把 `RuntimeHost` 的职责提前污染进来

## 禁止事项

1. 禁止把 `Well` 整仓复制成 `SpellFire.MemoryRobot`
2. 禁止第一刀就实现产品/插件容器
3. 禁止把 WoW 偏移和对象模型混进 `MemoryRobot`
4. 禁止把 `fasmdll_managed.dll` 当四大件之一推进
5. 禁止用一个大类继续包所有 native/读写/模块/分配能力

## 验收方式

第一阶段第一刀验收不看游戏内效果，先看底层能力是否成立：

1. 项目可独立构建
2. 能打开一个本地进程
3. 能完成基础读写
4. 能读模块列表
5. 能做页面查询和远程分配
6. 没有混入 WoW 语义和产品容器

当前已补充的自动验收：

```powershell
tools\runtimehost-smoke.ps1 -ProcessId <pid> -Shutdown
tools\memory-probe-matrix.ps1
```

其中 `memory-probe` 负责验收 session/权限诊断：

```text
OK memory-probe ProcessId=30188 State=Ready Components=2 | Name="MemoryRobot" Ready=True Reason="SessionOpened" Detail=" ProcessFound=True ProcessName="Wow" Responding=True HostX64OS=True HostX64Process=False TargetWow64Known=True TargetWow64=True RequestedAccess=DefaultMemoryAccess Win32Error=0 Win32Message="""
```

本轮真实 Wow 验收结果：

1. `MemoryRobot` 可打开目标进程
2. 目标进程识别为 WOW64 32 位进程
3. 诊断字段完整输出
4. 后续 Hook 完整 smoke 仍通过
5. 测试后 Wow 进程仍响应

失败样本矩阵验收结果：

```text
OK memory-probe-case Name=missing-process Pid=999999 Exit=1 Reason=ProcessUnavailable
OK memory-probe-case Name=system-access Pid=4 Exit=1 Reason=AccessDenied
OK memory-probe-case Name=explorer-bitness Pid=10792 Exit=1 Reason=TargetNot32Bit
OK memory-probe-matrix
```

## 停工条件

满足以下条件后，不再继续空谈设计，直接开工：

1. 项目职责已经清楚
2. 第一刀清单已经清楚
3. 可抽资产已经清楚
4. 禁止事项已经清楚

当前已满足，且第一批代码已经落地。

下一步应直接进入：

1. `MemorySessionCache / SessionManager` 是否需要进入下一刀
2. 或继续补进程退出中/重复打开关闭等竞态样本
3. 不应继续停留在“是否该开 MemoryRobot”的讨论
