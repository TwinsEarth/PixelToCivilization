# URP ShaderLab Pass 标签

本节包含 URP 专用的 ShaderLab Pass 标签说明。

> **注意**：URP 不支持以下 `LightMode` 标签：`Always`、`ForwardAdd`、`PrepassBase`、`PrepassFinal`、`Vertex`、`VertexLMRGBM`、`VertexLM`。

## LightMode<a name="lightmode"></a>

该标签的值决定了管线在执行渲染管线的不同部分时使用哪个 Pass。

如果在 Pass 中未设置 `LightMode` 标签，URP 将默认使用 `SRPDefaultUnlit` 作为该 Pass 的 `LightMode` 值。

`LightMode` 标签的可用值如下：

| **属性** | **描述** |
| :--- | :--- |
| **UniversalForward** | 该 Pass 负责渲染对象几何体并计算所有光照贡献。URP 在前向渲染路径（Forward Rendering Path）中使用此标签值。 |
| **UniversalGBuffer** | 该 Pass 仅渲染对象几何体，不计算任何光照贡献。此标签值用于 URP 在延迟渲染路径（Deferred Rendering Path）中执行的 Pass。 |
| **UniversalForwardOnly** | 该 Pass 负责渲染对象几何体并计算所有光照贡献，类似于 `UniversalForward`。<br/>不同之处在于，URP 可在前向和延迟渲染路径中使用该 Pass。<br/>当 URP 使用延迟渲染路径，但某些对象的 Shader 数据不适用于 GBuffer（例如具有 Clear Coat 法线的对象）时，请使用此标签值。<br/>如果 Shader 需要同时支持前向和延迟渲染路径，则声明两个 Pass，分别使用 `UniversalForward` 和 `UniversalGBuffer` 标签值。<br/>如果 Shader 需要始终使用前向渲染路径，无论 URP 采用哪种渲染路径，都应仅声明一个 `LightMode` 设为 `UniversalForwardOnly` 的 Pass。<br/>如果使用 SSAO 渲染器功能，请添加 `LightMode` 设为 `DepthNormalsOnly` 的 Pass。有关详细信息，请参阅 `DepthNormalsOnly` 值。 |
| **DepthNormalsOnly** | 在延迟渲染路径中，与 `UniversalForwardOnly` 结合使用。此值允许 Unity 在深度和法线预通道（prepass）中渲染 Shader。如果缺少 `DepthNormalsOnly` Pass，Unity 将无法在 Mesh 周围生成环境光遮蔽（AO）。 |
| **Universal2D** | 该 Pass 负责渲染 2D 物体并计算 2D 光照贡献。URP 在 2D 渲染器中使用此标签值。 |
| **ShadowCaster** | 该 Pass 负责从光源的角度渲染对象深度，并存储到阴影贴图或深度纹理中。 |
| **DepthOnly** | 该 Pass 仅从相机视角渲染深度信息，并存储到深度纹理中。 |
| **Meta** | 该 Pass 仅在 Unity 编辑器中烘焙光照贴图时执行。Unity 在构建 Player 时会剔除该 Pass。 |
| **SRPDefaultUnlit** | 使用此 `LightMode` 标签值时，将在渲染对象时额外绘制一个 Pass，例如用于绘制对象轮廓。此标签值适用于前向和延迟渲染路径。<br/>如果 Pass 没有 `LightMode` 标签，URP 将默认使用此值。 |

## UniversalMaterialType<a name="universalmaterialtype"></a>

Unity 在延迟渲染路径中使用此标签。

`UniversalMaterialType` 标签的可用值如下：

如果未在 Pass 中设置此标签，Unity 默认使用 `Lit` 值。

| **属性** | **描述** |
| :--- | :--- |
| **Lit** | 该值指示 Shader 类型为 Lit。<br/>在 G-buffer Pass 期间，Unity 使用模板缓冲区（stencil）标记使用 Lit Shader 类型的像素（采用 PBR 反射模型）。<br/>如果 Pass 未设置此标签，Unity 默认使用此值。 |
| **SimpleLit** | 该值指示 Shader 类型为 SimpleLit。<br/>在 G-buffer Pass 期间，Unity 使用模板缓冲区标记使用 SimpleLit Shader 类型的像素（采用 Blinn-Phong 反射模型）。 |
