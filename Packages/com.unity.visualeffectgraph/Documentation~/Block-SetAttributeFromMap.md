# Set Attribute from Map

菜单路径：**Attribute > Set <Attribute> From Map**

**Set Attribute from Map** 代码块是一个通用代码块，它根据从纹理采样的数据计算值，然后将这些值组合成给定的 **attribute**。

为了获得不同的结果，这个代码块可以使用各种采样模式。采样模式有：

+   **Index**、**IndexRelative** 和 **Sequential** 采样模式使用粒子索引对纹理的像素进行采样。这些模式能够以各种方式对 [Point Caches](point-cache-in-vfx-graph.md) 或 [Attribute Maps](PropertyBinders.md) 进行采样。
+   **Sample2DLOD** 和 **Sample3DLOD** 采样模式使用 2D 和 3D 坐标以及 LOD 因子对纹理进行采样。您可以使用这些模式来投影各种值，例如颜色或深度。
+   **Random** 和 **RandomUniformPerParticle** 样本模式允许您从存储在纹理中的值池中获取随机值，例如 Point Caches 或 Attribute Maps。

在此代码块从纹理中采样一个值后，它还可以对其应用比例和偏差。例如，如果纹理存储无符号归一化值，其中 0 是中间灰色，您可以应用 -0.5 的偏差将该值重新解释为零。

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Initialize](Context-Initialize.md)
+   [Update](Context-Update.md)
+   任何输出上下文

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Attribute** | Attribute | **（检查器）** 指定要写入的属性。 |
| **Composition** | Enum | **（检查器）** 指定此代码块如何合成属性。选项有：  <br/>• **Set**：用新值覆盖位置属性。  <br/>• **Add**：将新值添加到位置属性值。  <br/>• **Multiply**：将位置属性值乘以新值。  <br/>• **Blend**：在位置属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |
| **Sample Mode** | Enum | 指定代码块如何采样纹理。选项：  <br/>• **IndexRelative**：使用从浮点端口读取的值来确定像素索引。输入值预计在 0..1 范围内，并将乘以纹理的像素数来确定索引。  <br/>• **Index**：使用从整数端口读取的值作为像素索引。  <br/>• **Sequential**：使用粒子 ID 属性作为像素索引。  <br/>• **Sample2DLOD**：使用从 vector2 输入端口提供的坐标和来自另一个输入端口的 LOD 来采样 2D 纹理。  <br/>• **Sample3DLOD**：使用从 vector3 输入端口提供的坐标和来自另一个输入端口的 LOD 来采样 3D 纹理。  <br/>• **Random**：使用随机 2D/3D 位置对纹理进行采样。  <br/>• **RandomUniformPerParticle**：使用（每个粒子）唯一 2D/3D 位置对纹理进行采样。 |
| **Channels** | Enum | 指定此代码块要影响的属性通道。此代码块不会影响您未包含在此属性中的通道。  <br/>此设置仅在您设置的 **Attribute** 包含通道时显示。 |

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Attribute Map** | Texture2D/Texture3D | 此代码块从中采样的纹理。 |
| **RelativePos** | float | 从中采样的相对于纹理中像素数的索引。此属性需要 0 到 1 范围内的值，代码块重新映射到 0 到 N 范围内，其中 N 是纹理中的像素总数（宽度*高度）。 <br/>此属性仅在将 **Sample Mode** 设置为 **Index Relative** 时显示。 |
| **Index** | uint | 从中采样的索引。此属性需要 0 到 N 范围内的值，其中 N 是纹理中的像素总数（宽度*高度）。  <br/>此属性仅在将 **Sample Mode** 设置为 **Index** 时显示。 |
| **Sample Position** | Vector2/Vector3 | 要从中采样的 2D 或 3D 纹理中的坐标。  <br/>此属性仅在将 **Sample Mode** 设置为 **Sample2DLOD** 或 **Sample3DLOD** 时显示。 |
| **LOD** | float | 要从中采样的 2D 或 3D 纹理的 LOD。  <br/>此属性仅在将 **Sample Mode** 设置为 **Sample2DLOD** 或 **Sample3DLOD** 时显示。 |
| **Seed** | uint | 此代码块用于计算随机值的种子。  <br/>此属性仅在将 **Sample Mode** 设置为 **RandomUniformPerParticle** 时显示。 |
| **Blend** | float | 当前属性值与新计算的值之间的混合百分比。  <br/>此属性仅在将 **Composition** 或 **Alpha Composition** 设置为 **Blend** 时显示。 |
