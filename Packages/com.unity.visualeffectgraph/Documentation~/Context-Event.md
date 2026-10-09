# 事件

菜单路径: **Context > Event**

**Event** Context 定义输入事件名称。它不一定需要唯一的命名;您可以在图表中的不同位置复制相同的事件名称。

## 上下文设置

| **设置**    | **类型** | **描述**                                              |
| -------------- | -------- | ------------------------------------------------------------ |
| **Event Name** | String   | 事件的名称。此名称显示在[GetOutputEventNames](https://docs.unity3d.com/2020.2/Documentation/ScriptReference/VFX.VisualEffect.GetOutputEventNames.html)返回。默认值 **OnPlay** 是 VFX Graph 发送到[Spawn](Context-Spawn.md) Context 中。<br/>**Send** 按钮获取当前打开的场景中的所有活动 Visual Effect 组件，并使用 [VisualEffect.SendEvent](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.SendEvent.html) 将此事件发送给他们所有人。这意味着 **Send** 也会影响不使用您正在编辑的 Visual Effect 资源的组件。|

## 工作流

| **端口**   | **描述**                                    |
| ---------- | -------------------------------------------------- |
| **Output** | 连接到 [Spawn](Context-Spawn.md) 上下文。 |
