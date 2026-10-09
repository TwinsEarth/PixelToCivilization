# VFX 类型参考

此页面引用了 Visual Effect Graph 使用的所有类型。其中一些类型是标准的 C# 和 Unity 类型，而另一些类型是 Visual Effect Graph 独有的。

## 基本类型

基本类型引用可用于存储基本数据的标准 C# 和 Unity 类型。

### 属性兼容类型

属性兼容类型是可用于在系统或 SpawnEvent 有效负载中存储属性的类型。

| **类型** | **描述** |
| --- | --- |
| **float** | 32 位浮点值。 |
| **int** | 32 位有符号整数值。 |
| **uint** | 32 位无符号整数值。 |
| **bool** | 1 位布尔值。 |
| **Vector2** | 二维 32 位浮点矢量。 |
| **Vector3** | 三维 32 位浮点矢量。 |
| **Vector3** | 四维 32 位浮点矢量。 |
| **Color** | 四分量（红色、绿色、蓝色、Alpha）32 位浮点线性颜色。 |

### 其他基本类型

以下基本类型与属性不兼容，这意味着您不能将它们用作粒子的属性，但如果它们是公开的属性，您可以在 Visual Effect 组件上设置它们。

| **类型** | **描述** |
| --- | --- |
| **Gradient** | Unity 的 [Gradient](https://docs.unity3d.com/ScriptReference/Gradient.html) 类型。 |
| **AnimationCurve** | Unity 的 [AnimationCurve](https://docs.unity3d.com/ScriptReference/AnimationCurve.html) 类型。 |
| **Texture2D** | Unity 的 [Texture2D](https://docs.unity3d.com/ScriptReference/Texture2D.html) 类型。 |
| **Texture3D** | Unity 的 [Texture3D](https://docs.unity3d.com/ScriptReference/Texture3D.html) 类型。 |
| **TextureCube** | Unity 的 [CubeMap](https://docs.unity3d.com/ScriptReference/Cubemap.html) 类型。 |
| **Texture2DArray** | Unity 的 [Texture2DArray](https://docs.unity3d.com/ScriptReference/Texture2DArray.html) 类型。 |
| **TextureCubeArray** | Unity 的 [CubeMapArray](https://docs.unity3d.com/ScriptReference/CubemapArray.html) 类型。 |
| **Mesh** | Unity 的 [Mesh](https://docs.unity3d.com/ScriptReference/Mesh.html) 类型。 |

## 高级类型

本节介绍 Visual Effect Graph 包含的高级类型。它们要么是基本类型的高级版本，要么是复合类型（由多个属性组成）。

### 空间基础类型

空间基本类型是随值嵌入了空间的 Vector 类型。它们在执行空间变换时也使用矢量语义。

| **类型** | **描述** |
| --- | --- |
| **Position** | 世界空间或本地空间三分量位置矢量。有关此类型及其属性的更多信息，请参见 [Position](Type-Position.md)。 |
| **Vector** | 世界空间或本地空间三分量矢量。有关此类型及其属性的更多信息，请参见 [Vector](Type-Vector.md)。 |
| **Direction** | 世界空间或本地空间三分量归一化方向。当您检索它们时，这种类型的值总是进行归一化。有关此类型及其属性的更多信息，请参见 [Direction](Type-Direction.md)。 |

### 形状类型

形状类型是高级类型，它根据基本类型的组合定义形状。

| **类型** | **描述** |
| --- | --- |
| **Sphere** | 由中心位置和半径定义的球体。有关此类型及其属性的更多信息，请参见 [Sphere](Type-Sphere.md)。 |
| **ArcSphere** | 由角度和 [Sphere](Type-Sphere.md) 定义的球体的实心弧体。有关此类型及其属性的更多信息，请参见 [ArcSphere](Type-ArcSphere.md)。 |
| **AABox** | 轴对齐的 3D 盒体，由中心和 3D 大小定义。有关此类型及其属性的更多信息，请参见 [AABox](Type-AAbox.md)。 |
| **Plane** | 一个无限 3D 平面，由一个位置和一个法线矢量定义。有关此类型及其属性的更多信息，请参见 [Plane](Type-Plane.md)。 |
| **OrientedBox** | 由位置、欧拉角（以度为单位）和缩放定义的定向盒体。有关此类型及其属性的更多信息，请参见 [OrientedBox](Type-OrientedBox.md)。 |
| **Circle** | 在 XY 平面上定向的 3D 圆，由位置和半径定义。有关此类型及其属性的更多信息，请参见 [Circle](Type-Circle.md)。 |
| **ArcCircle** | 由角度和 [Circle](Type-Circle.md) 定义的圆形的实心圆弧。有关此类型及其属性的更多信息，请参见 [ArcCircle](Type-ArcCircle.md)。 |
| **Torus** | 在 XY 平面上定向的 3D 圆环，由位置、大半径（圆环半径）和小半径（圆环厚度）定义。有关此类型及其属性的更多信息，请参见 [Torus](Type-Torus.md)。 |
| **ArcTorus** | 由角度和 3D [Torus](Type-Torus.md) 定义的圆环体的实心圆弧。有关此类型及其属性的更多信息，请参见 [ArcTorus](Type-ArcTorus.md)。 |
| **Cone** | 由高度、上半径和下半径定义的 3D 锥体。有关此类型及其属性的更多信息，请参见 [Cone](Type-Cone.md)。 |
| **ArcCone** | 由角度和 [Cone](Type-Cone.md) 定义的 3D 锥体的实心弧体。有关此类型及其属性的更多信息，请参见 [ArcCone](Type-ArcCone.md)。 |
| **Line** | 由两个位置定义的线。有关此类型及其属性的更多信息，请参见 [Line](Type-Line.md)。 |
| **Transform** | 由位置、欧拉角（以度表示）和比例定义的平移、旋转和缩放组件。有关此类型及其属性的更多信息，请参见 [Transform](Type-Transform.md)。 |

### 其他类型

| **类型** | **描述** |
| --- | --- |
| **TerrainType** | 由一组边界、高度贴图和高度定义的 Unity 地形。有关此类型及其属性的更多信息，请参见 [TerrainType](Type-TerrainType.md)。 |
| **Camera** | Unity Camera 由变换、视野、近平面、远平面、宽高比、分辨率定义。您也可以访问颜色和深度缓冲区。有关此类型及其属性的更多信息，请参见 [Camera](Type-Camera.md)。 |

