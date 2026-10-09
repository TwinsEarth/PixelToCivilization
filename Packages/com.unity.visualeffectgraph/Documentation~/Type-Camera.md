# Camera

Unity Camera 由变换、视野、近平面、远平面、宽高比、分辨率定义。您也可以访问颜色和深度缓冲区。

## 属性

| **属性**                | **描述**                                                                                                                                                                                                                                                                                      |
|-----------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Transform**               | 摄像机的变换。这是一个包含摄像机位置和方向的 [Transform](Type-Transform.md)。                                                                                                                                                                           |
| **Orthographic**            | 相机是正交 （true） 还是透视 （false）？有关更多信息，请参阅 [orthographic](https://docs.unity3d.com/ScriptReference/Camera-orthographic.html)。                                                                                                                              |
| **Field Of View**           | 摄像机沿本地 y 轴的视角（以度数为单位）高度。有关更多信息，请参阅 [fieldOfView](https://docs.unity3d.com/ScriptReference/Camera-fieldOfView.html)。                                                                                                                |
| **Near Plane**              | 绘制发生位置的相对于摄像机的最近平面。有关更多信息，请参阅 [nearPlane](https://docs.unity3d.com/ScriptReference/Camera-nearClipPlane.html)。                                                                                                                       |
| **Far Plane**               | 绘制发生位置的相对于摄像机的最远平面。有关更多信息，请参阅 [farPlane](https://docs.unity3d.com/ScriptReference/Camera-farClipPlane.html)。                                                                                                                        |
| **Orthographic Size**       | 相机在正交模式下为一半大小。有关更多信息，请参阅 [orthographicSize](https://docs.unity3d.com/ScriptReference/Camera-orthographicSize.html)。                                                                                                                               |
| **Aspect Ratio**            | 摄像机的宽度和高度之间的比例关系。有关更多信息，请参阅 [aspect](https://docs.unity3d.com/ScriptReference/Camera-aspect.html)。                                                                                                                                |
| **Pixel Dimensions**        | 摄像机的宽度和高度（以像素为单位）。有关更多信息，请参阅 [pixelWidth](https://docs.unity3d.com/ScriptReference/Camera-pixelWidth.html) 和 [pixelHeight](https://docs.unity3d.com/ScriptReference/Camera-pixelHeight.html)。                        |
| **Scaled Pixel Dimensions** | 动态分辨率缩放后相机的宽度和高度（以像素为单位）。有关更多信息，请参阅 [scaledPixelWidth](https://docs.unity3d.com/ScriptReference/Camera-scaledPixelWidth.html) 和 [scaledPixelHeight](https://docs.unity3d.com/ScriptReference/Camera-scaledPixelHeight.html)。 |
| **Lens Shift**              | 相机的镜头偏移。有关更多信息，请参阅 [lensShift](https://docs.unity3d.com/ScriptReference/Camera-lensShift.html)。                                                                                                                                                                |
| **Depth Buffer**            | 指定此摄像机的深度缓冲区。                                                                                                                                                                                                                                                          |
| **Color Buffer**            | 指定此摄像机的颜色缓冲区。                                                                                                                                                                                                                                                          |
