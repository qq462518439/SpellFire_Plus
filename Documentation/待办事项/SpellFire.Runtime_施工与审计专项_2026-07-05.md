# SpellFire.Runtime 施工与审计专项

## 当前结论

`SpellFire.Runtime` 不能继续空着。

它当前不应承担四大件实现本体，但必须承担：

1. 上层唯一消费入口
2. 运行时快照模型
3. 宿主接线门面
4. 默认组合根

一句话：

`SpellFire.Runtime` 当前是 **薄中间层**，不是空目录，也不是大而全实现仓。

## 当前定位

它当前负责：

1. 对上提供统一合同
2. 对下消费 `SpellFire.RuntimeHost`
3. 将宿主结果转换成稳定快照模型

它当前不负责：

1. 内存读写实现
2. hook 实现
3. Wow 语义实现
4. NavMesh 实现

## 当前已落地内容

1. `IRuntimeFacade`
2. `IRuntimeSessionService`
3. `RuntimeSessionSnapshot`
4. `RuntimeComponentSnapshot`
5. `RuntimeSessionService`
6. `RuntimeFacade`
7. `RuntimeCompositionRoot`

## 当前边界

`SpellFire.Runtime` 当前只做：

1. attach 一个进程
2. 返回一份宿主快照

当前不做：

1. 常驻 session 管理
2. Wow 语义服务编排
3. 导航接线
4. 插件接线

## 当前阶段

当前属于：

1. **成型地基**

不是成品，因为：

1. 它现在还只汇总宿主状态
2. 还没有向 `WowRuntime` 提供统一消费面

## 下一步

下一刀应做：

1. 让 `SpellFire.Runtime` 消费 `WowRuntime` 的只读能力

不是：

1. 把实现细节反灌进 `SpellFire.Runtime`
