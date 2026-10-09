<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"> <b>实验性：</b>此功能目前是实验性的，在以后的主要版本中可能会发生变化。</div>

# Sample Mesh Index

 **Menu Path : Operator > Sampling > Sample Mesh Index**

 **Menu Path : Operator > Sampling > Sample Skinned Mesh Renderer Index**

使用“样例网格”或“蒙皮网格渲染器索引”操作符可以获取几何体的索引缓冲区数据。同时[UInt16](https://docs.unity3d.com/ScriptReference/ModelImporterIndexFormat.UInt16.html)支持和[UInt32](https://docs.unity3d.com/ScriptReference/ModelImporterIndexFormat.UInt32.html)格式。此运算符的输出始终是一个uint.

## 运算符设置

| **Property** | **Type** | **描述**|
| ------------ | -------- | ------------------------------------------------------------ |
| **Source**   | Enum     |选择要采样的几何图形的类型，可以是 **网眼****蒙皮网格渲染器**|

### 运算符属性

| **Input**                 | **Type**              | **描述**|
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Mesh**                  | Mesh                  |要采样的源网格资源。仅当设置**源头**为**网眼**时，才会显示该<br/>属性|
| **Skinned Mesh Renderer** | Skinned Mesh Renderer |要采样的源蒙皮网格渲染器组件。这是对场景中组件的引用。若要将蒙皮网格渲染器指定给该端口，请在中[Blackboard](Blackboard.md)创建蒙皮网格渲染器属性并显示该属性。该<br/>属性仅在设置**源头**为**蒙皮网格渲染器**时才会显示|
| **Index**                 | Uint                  |对当前索引缓冲区进行采样的索引偏移量。|

| **Output** | **Type** | **描述**|
| ---------- | -------- | ------------------------------------------------------------ |
| **Index**  | UInt     |采样索引，如果输入**索引**超出界限，则为零。|

#### 局限性

“网格索引”（Mesh Index）采样功能具有以下限制：

- 如果网格不[readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html)是，则**位置（网格）**块和**Sample Mesh Index**操作符在尝试对其进行采样时返回零值。有关如何使网格可读的信息，请参见 [模型导入设置](https://docs.unity3d.com/Manual/FBXImporter-Model.html)

 ![](Images/ReadWrite.png)
