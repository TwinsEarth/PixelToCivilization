# 转换您的着色器

为内置渲染管线编写的着色器与Universal Render Pipeline（URP）着色器不兼容。如果对象使用内置渲染管线着色器，Unity将使用[默认的品红错误着色器](https://docs.unity.cn/cn/tuanjiemanual/Manual/shader-error.html)进行渲染。

使用[渲染管线转换器](features/rp-converter.md)将Unity内置渲染管线的任何材质和着色器转换为URP材质和着色器。有关更多信息，请参阅[着色器映射](#shader-mappings)。

**注意**：渲染管线转换器对项目进行的更改是不可逆的。请在转换之前备份您的项目。

**提示**：如果在转换后，项目视图中的预览缩略图没有正确显示，尝试右键单击项目视图中的任意位置并选择**Reimport All**。

对于[SpeedTree](https://docs.unity.cn/cn/tuanjiemanual/Manual/SpeedTree.html)着色器，Unity在重新导入时不会重新生成材质，除非点击**Generate Materials**或**Apply & Generate Materials**按钮。

<a name="custom-shaders"></a>

## 自定义着色器

无法升级为内置渲染管线编写的自定义Unity着色器。相反，自定义着色器必须重新编写以便与URP兼容，或者在[ShaderGraph](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@14.0/manual/index.html)中重新创建。有关如何重写和升级内置渲染管线自定义着色器以兼容URP的示例，请参阅[升级自定义着色器以支持URP](urp-shaders/birp-urp-custom-shader-upgrade-guide.md)。

在升级项目以使用URP时，场景中使用自定义着色器的任何材质会变为粉红色，表示该材质不再有效。为了解决此问题，请升级或更改材质的着色器为与URP兼容的着色器。

**注意**：URP不支持[表面着色器](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-SurfaceShaders.html)。

<a name="built-in-to-urp-shader-mappings"></a>

## 着色器映射

下表显示了在使用渲染管线转换器时，内置渲染管线着色器会转换为哪些URP着色器。

| Unity内置着色器                              | Universal Render Pipeline着色器              |
| -------------------------------------------- | ------------------------------------------- |
| Standard                                     | Universal Render Pipeline/Lit               |
| Standard (Specular Setup)                    | Universal Render Pipeline/Lit               |
| Standard Terrain                             | Universal Render Pipeline/Terrain/Lit       |
| Particles/Standard Surface                   | Universal Render Pipeline/Particles/Lit     |
| Particles/Standard Unlit                     | Universal Render Pipeline/Particles/Unlit   |
| Mobile/Diffuse                               | Universal Render Pipeline/Simple Lit        |
| Mobile/Bumped Specular                       | Universal Render Pipeline/Simple Lit        |
| Mobile/Bumped Specular(1 Directional Light)  | Universal Render Pipeline/Simple Lit        |
| Mobile/Unlit (Supports Lightmap)             | Universal Render Pipeline/Simple Lit        |
| Mobile/VertexLit                             | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Diffuse                       | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Specular                      | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Bumped Diffuse                | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Bumped Specular               | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Self-Illumin/Diffuse          | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Self-Illumin/Bumped Diffuse   | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Self-Illumin/Specular         | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Self-Illumin/Bumped Specular  | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Transparent/Diffuse           | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Transparent/Specular          | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Transparent/Bumped Diffuse    | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Transparent/Bumped Specular   | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Transparent/Cutout/Diffuse    | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Transparent/Cutout/Specular   | Universal Render Pipeline/Simple Lit        |
| Legacy Shaders/Transparent/Cutout/Bumped Diffuse | Universal Render Pipeline/Simple Lit    |
| Legacy Shaders/Transparent/Cutout/Bumped Specular | Universal Render Pipeline/Simple Lit    |