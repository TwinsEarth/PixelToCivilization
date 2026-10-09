# Visual Effect Graph 中的有向距离场

有序距离场 （SDF） 是 3D 纹理，其中每个纹素存储与对象表面的距离。按照惯例，此距离在对象内部为负值，在对象外部为正值。SDF 可用于创建与复杂几何体交互的粒子效果。

## 使用 SDF

在 Visual Effect Graph 中，有几个节点使用 SDF 创建效果：

- [*Position On Signed Distance Field*](Block-SetPosition(SignedDistanceField).md)：将粒子定位在 SDF 的体积内或其表面上。
- [*Conform To Signed Distance Field*](Block-ConformToSignedDistanceField.md): 将粒子吸引到 SDF。这对于将粒子拉向使用其他 Force Block 难以复制的复杂形状非常有用。
- [*Collide With Signed Distance Field*](Block-CollideWithSignedDistanceField.md): 模拟粒子与 SDF 之间的碰撞。当您希望粒子与复杂形状碰撞时，这非常有用。
- [*Sample Signed Distance Field*](Operator-SampleSDF.md): 对 SDF 进行采样，并使您能够使用结果创建自定义行为。

## 生成 SDF

有多种方法可以生成用于视觉效果的 SDF：

- 您可以在 Unity Editor 中使用窗口生成 SDF，并在运行时使用 API 和内置的 [SDF Bake Tool](sdf-bake-tool.md) 生成 SDF。有关详细信息，请参见 [SDF 烘焙工具](sdf-bake-tool.md)。
- 您可以使用与 [*VFXToolbox*](https://github.com/Unity-Technologies/VFXToolbox)（位于 /DCC~ 文件夹中）捆绑的 Houdini Volume Exporter 烘焙 SDF。

## 限制和注意事项

[SDF 烘焙工具](sdf-bake-tool.md)（窗口和 API）可生成标准化的 SDF。这意味着底层表面会缩放，使得 Texture 的最大边的长度为 1。如果您使用 Sample Signed Distance Field 等运算符，请记住这一点。
