# 为光照准备和升级 sprite 与项目

要使用 [2D 光源](2DLightProperties.md) 为 sprite 照明，首先打开该 sprite 的 [Sprite Renderer](xref:class-SpriteRenderer) 组件，并为其分配一个可以响应 2D 光源的材质。当您将 sprite 拖入场景时，Unity 会自动为它们分配 `Sprite-Lit-Default` 材质，从而使其能够与 2D 光源交互并看起来被照亮。

您还可以通过 [Shader Graph 包](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest) [创建自定义 Shader](ShaderGraph.md) 来响应光源。

## 升级现有材质

如果您在具有预先存在的 prefab、材质或场景的项目中安装 URP 包，并希望使用该包的 2D 光照功能，则需要将使用的材质升级为兼容光照的 Shader。

**警告：** 以下操作会自动以不可逆的方式升级场景或项目。Unity 无法将已升级的场景或项目还原到其先前状态。在开始此操作之前，请备份您不希望丢失或转换的任何文件。

要升级项目，请转到 **Window > Rendering > Render Pipeline Converter**。启用 **Material Upgrade**，然后选择 **Convert Assets** 开始升级。

有关将为内置渲染管线项目制作的资产转换为与 2D URP 兼容的资产的信息，请参阅 [Render Pipeline Converter](features/rp-converter.md#converters)。
