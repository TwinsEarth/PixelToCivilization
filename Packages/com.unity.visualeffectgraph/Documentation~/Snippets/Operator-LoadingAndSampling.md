## Loading and sampling

在 Visual Effect Graph 中，有多个 Operator 可以从纹理中读取纹素值。在底层 （HLSL） 中，其中一些使用 Load（），而另一些则使用 Sample（）。

使用 Load（） 的 Operator 和使用 Sample（） 的 Operator 之间的区别如下：

* Load（） 不对最终纹素值应用任何过滤，而 Sample（） 使用与目标纹理的 [导入设置](https://docs.unity3d.com/Manual/class-TextureImporter.html) 相同的 Filter Mode。 
* Load（） 不应用任何换行，而是为指定纹理外部纹素的坐标返回 0。Sample（） 使用与目标 Texture 的 [导入设置](https://docs.unity3d.com/Manual/class-TextureImporter.html) 相同的 Wrap Mode。
* Load（） 使用纹素坐标（在 0 到纹理的宽度/高度减 1 的范围内），而 Sample（） 使用 UV 坐标（在 0-1 的范围内）。
