# Buffer Count

**菜单路径 : Operator > Sampling > Buffer Count**

Buffer Count 运算符使您能够获取[GraphicsBuffer](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.html) 中的元素数。

## Operator Properties

| **Input**  | **类型**        | **描述**                                              |
| ---------- | --------------- | ------------------------------------------------------------ |
| **Buffer** | GraphicsBuffer | 用于获取元素数的源 GraphicsBuffer。您只能将公开的属性连接到此输入端口。 |


| **Output** | **类型** | **描述**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| **Count**  | UInt     | 缓冲区中的元素数。有关更多信息，请参阅 [GraphicsBuffer.count](https://docs.unity3d.com/ScriptReference/GraphicsBuffer-count.html) |
