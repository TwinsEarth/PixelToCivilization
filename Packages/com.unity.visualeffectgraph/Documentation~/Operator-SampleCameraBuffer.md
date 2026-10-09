# Sample CameraBuffer

菜单路径： **Operator > Sampling > Sample CameraBuffer**

 **Sample Texture2D**运算符对特定**像素尺寸**和**紫外线**的摄像头进行采样。

这将转换为高级着色语言（HLSL）中对纹理的示例调用。

## 运算符属性

| **Input**            | **Type**                             | **描述**|
| -------------------- | ------------------------------------ | --------------------------------------------- |
| **CameraBuffer**     | [CameraBuffer](Type-CameraBuffer.md) |此运算符采样的相机缓冲区。|
| **Pixel Dimensions** | Vector2                              |相机像素尺寸|
| **UV**               | Vector2                              |要对CameraBuffer进行采样的UV.|

| **Output** | **Type** | **描述**|
| ---------- | -------- | ---------------------------------- |
| **s**      | Vector4  |纹理中的采样值|

## 局限性

此操作符仅在GPU上运行，因此在插入**Spawner Context**端口时无法正常工作。
