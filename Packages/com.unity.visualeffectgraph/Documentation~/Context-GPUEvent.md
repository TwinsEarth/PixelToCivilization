<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>实验性:</b> 此功能目前是试验性的，在以后的主要版本中可能会发生变化。要使用此功能，请在工程 Preferences 的  <b>Visual Effects</b> 选项卡中启用  <b>Experimental Operators/Blocks</b>。</div>

# GPU 事件

菜单路径： **Context > GPUEvent**

**GPU 事件**上下文允许您从 Update 或 Initialize Contexts 中的特定块生成新粒子。

## 上下文设置

| **设置** | **类型** | **描述**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Evt**      | GPUEvent | 从触发GPU事件的[块](Blocks.md)连接。触发GPU事件的块是：<br/>&#8226; **Trigger Event Always**.<br/>&#8226; **Trigger Event On Die**.<br/>&#8226; **Trigger Event Rate** |

## Flow

| **端口**       | **描述**                                              |
| -------------- | ------------------------------------------------------------ |
| **SpawnEvent** | 连接到 [初始化](Context-Initialize.md)  上下文。来自 Spawn 上下文且 GPU 事件不能混合的流程锚点。 |
