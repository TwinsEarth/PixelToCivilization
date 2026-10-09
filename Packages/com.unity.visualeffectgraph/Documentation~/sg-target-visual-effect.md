# Visual Effect Target

Visual Effect Shader Graph [Target](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest?subfolder=/manual/Graph-Target.html) 使您能够创建自定义光照和无光照 Shader Graph，以便在视觉效果中使用。

要创建使用 Visual Effect Target 的 Shader Graph，请选择 **Assets** > **Create** > **Shader Graph** > **VFX Shader Graph**。

## 上下文

此 Shader Graph Target 具有自己的一组 Graph Settings。由于设置和 Blocks 之间的关系，这会影响哪些 Block 与 Graph 相关。本部分包含有关此目标默认添加的块的信息，以及哪些块为此目标的 Graph Settings 设置属性。

### 顶点上下文

#### 默认

当您使用 Visual Effect Target 创建新的 Shader Graph 时，Vertex Context 默认包含以下块：

| **属性**   | **描述**                                              | **设置依赖关系** | **默认值** |
| ------------ | -------------------------------------------- | ---------------------- | ---------------------- |
| **Position** | 每个顶点的对象空间顶点法线。   | None                   | CoordinateSpace.Object |
| **Normal**   | 每个顶点的对象空间顶点位置。 | None                   | CoordinateSpace.Object |
| **Tangent**  | 每个顶点的对象空间顶点切线。 | None                   | CoordinateSpace.Object |

#### 相关

默认情况下，此 Target 会将其所有 Vertex Blocks 添加到 Vertex Context，并且没有额外的相关块。

### 片段上下文

#### 默认

当您使用 Visual Effect Target 创建新的 Shader Graph 时，默认情况下，Fragment Context 包含以下块：

| **属性**   | **描述**                                              | **设置依赖关系** | **默认值** |
| -------------- | ------------------------------------------------------------ | ---------------------- | ----------------- |
| **Base Color** | 材质的底色                    | None                   | Color.grey        |
| **Alpha**      | 材质的 Alpha 值。这决定了材质的透明度。预期范围为 0 - 1。 | None                   | 1.0               |
| **Emission**   | 要从此材质的表面发射的光的颜色。自发光材质在场景中显示为光源。 | None                   | Color.black       |

#### 相关
根据您使用的 [Graph Settings](#graph-settings)，Shader Graph 可以将以下块添加到 Fragment 上下文中：

| **属性**   | **描述**                                              | **设置依赖关系** | **默认值** |
| -------------------------- | ------------------------------------------------------------ | ----------------------------------- | ----------------------- |
| **Alpha Clip Threshold**   | HDRP 用于确定是否渲染每个像素的 Alpha 值限制。如果像素的 Alpha 值等于或高于限制，则 HDRP 会渲染该像素。如果该值低于限制，则 HDRP 不会渲染像素。 |  启用 **Alpha Clipping**  | 0.5                     |
| **Metallic**               | 材质的金属价值。这定义了材质表面的“金属状”程度（介于 0 和 1 之间）。当表面的金属感更强时，它会更多地反射环境，并且其反照率颜色变得不那么明显。在完全金属级别，表面颜色完全由环境反射驱动。当表面的金属感较低时，其反照率颜色会更清晰，并且任何表面反射都会在表面颜色的顶部可见，而不是遮挡它。 |  **Material** 设置为 **Lit** | 0.0                     |
| **Smoothness**             | 材料的光滑度。照射到光滑表面的每条光线都会以可预测且一致的角度反弹。要获得像镜子一样反射光线的完美平滑表面，请将此值设置为 1。不太平滑的表面会在更宽的角度范围内反射光线（因为光线会照射到微表面中的凹凸），因此反射的细节较少，并以更漫射的图案分布在整个表面上。 |  **Material** 设置为 **Lit** | 0.5                     |
| **Normal (Tangent Space)** | 材质在切线空间中的法线。             |  **Material** 设置为 **Lit** | CoordinateSpace.Tangent |

## Graph Settings

| **属性**       | **描述**                                              |
| ------------------ | ------------------------------------------------------------ |
| **Material**       | 指定场景中的照明是否影响材质。选项包括：<br/> **Unlit**: 场景照明不影响材质。<br/> **Lit**: 场景照明会影响材质。此选项将额外的 Block 添加到 fragment Context 中。 |
| **Alpha Clipping** | 指示此材料是否类似于[ Cutout Shader](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html)。 |
