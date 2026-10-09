# 样本缓冲器

 **Menu Path : Operator > Sampling > Sample Buffer**

Sample Buffer操作符使你能够获取结构化缓冲区并对其进行采样。结构化缓冲区是[图形缓冲区](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.html)使用目标创建的[Structured](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.Target.Structured.html)。

## 运算符设置

| **Input** | **Type** | **描述**|
| --------- | -------- | ------------------------------------------------------------ |
| **Mode**  | Enum     |用于序列的环绕模式。选项包括：<br/>•**夹钳**：钳制第一个顶点和最后一个顶点之间的索引。<br/>•**包裹**：将索引环绕到顶点列表的另一侧。<br/>•**镜子**：镜像顶点列表，以便超出范围的索引在列表中来回移动。|

### 运算符属性

| **Input**  | **Type**                                | **描述**|
| ---------- | --------------------------------------- | ------------------------------------------------------------ |
| **Input**  | [Configurable](#operator-configuration) |结构类型。|
| **Buffer** | GraphicsBuffer                          |要提取的结构化缓冲区。只能将公开的属性连接到此输入端口。|
| **Index**  | uint                                    |要提取的元素的索引。|

| **Output** | **Type**  | **描述**|
| ---------- | --------- | ------------------------------------------------------------ |
| **s**      | Dependent |索引处的采样结构，考虑**模式**到设置。|

## 运算符配置

要查看运算符的配置，请单击**齿轮**运算符标题中的图标。

### 可用类型
此运算符支持对使用直接复制到本机结构中的类型的结构化缓冲区进行采样。内置可直接复制到本机结构中的类型的列表为：
-  **float**
-  **int**
-  **uint**
-  **Vector2**
-  **Vector3**
-  **Vector4**
-  **Matrix4x4**

你还可以声明自定义类型。为此，请将 `[VFXType]` 属性添加到结构中，并使用 `VFXTypeAttribute.Usage.GraphicsBuffer` 类型。例如：

```c#
using UnityEngine;
using UnityEngine.VFX;

[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
struct CustomData
{
    public Vector3 color;
    public Vector3 position;
}
```

## 局限性
运算符有以下限制：

- 此运算符需要使用目标创建的 [Structured](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.Target.Structured.html)GraphicsBuffer.
- GraphicsBuffer声明的跨距必须与结构跨距匹配。
- 结构必须是可直接复制的。这意味着该结构不能存储对Texture2D的引用，但它可以存储任何其他可直接复制到本机结构。
- 此运算符仅支持使用Visual Effect Graph支持的可直接复制到本机结构中的公共类型之一的结构化缓冲区。有关可用类型的列表，请参见[可用类型](#available-types)。
