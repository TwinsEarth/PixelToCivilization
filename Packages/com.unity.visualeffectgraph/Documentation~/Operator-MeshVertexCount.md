<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"> <b>实验性：</b>此功能目前是实验性的，在以后的主要版本中可能会发生变化。</div>

# Skinned Mesh Vertex Count

 **Menu Path : Operator > Sampling > Mesh Vertex Count****Menu Path : Operator > Sampling > Skinned Mesh Vertex Count**

“网格顶点数”操作符允许你检索网格或蒙皮网格渲染器的几何体中的顶点数。

## 运算符设置

| **Property** | **Type** | **描述**|
| ------------ | -------- | ------------------------------------------------------------ |
| **Source**   | Enum     | **（检验员）**选择源几何图形的类型，A **网眼**或 **蒙皮网格渲染器**|

### 运算符属性

| **Input**                 | **Type**              | **描述**|
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Mesh**                  | Mesh                  |要采样的源网格资源。仅当设置**源头**为**网眼**时，才会显示该<br/>属性|
| **Skinned Mesh Renderer** | Skinned Mesh Renderer |要采样的源蒙皮网格渲染器组件。这是对场景中组件的引用。若要将蒙皮网格渲染器指定给该端口，请在中[Blackboard](Blackboard.md)创建蒙皮网格渲染器属性并显示该属性。该<br/>属性仅在设置**源头**为**蒙皮网格渲染器**时才会显示|

| **Output** | **Type** | **描述**|
| ---------- | -------- | --------------------------------------- |
| **count**  | UInt     |几何体中的顶点数。|

#### 局限性

“Skinned Mesh Vertex Count”操作符具有以下限制：

- 如果网格不[readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html)是，则此运算符返回uint.maxValue.有关如何使网格可读的信息，请参见 [模型导入设置](https://docs.unity3d.com/Manual/FBXImporter-Model.html)

<img src="Images/ReadWrite.png" alt="image-20200320154843722" style="zoom:78%;" />
