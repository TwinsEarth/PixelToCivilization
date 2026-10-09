# Sample Signed Distance Field

 **Menu Path : Operator > Sampling > Sample Signed Distance Field**

Sample Signed Distance Field 操作符允许你获取存储在Texture3D中的距离场。

Sample Signed Distance Field（SDF）确定从空间中的点到形状表面的距离。按照惯例，此函数对于形状内部的点为负，而对于形状外部的点为正。对象表面上的SDF等于零。

### 运算符属性

| **Input**       | **Type**                           | **描述**|
| --------------- | ---------------------------------- | ------------------------------------------------------------ |
| **texture**     | Texture3D                          |存储SDF的3D纹理。|
| **position**    | [Position](Type-Position.md)       |SDF的采样位置。|
| **orientedBox** | [OrientedBox](Type-OrientedBox.md) |指定要应用于SDF的变换的“定向”框。|
| **Level**       | float                              |mipmap级别。|

| **Output**    | **Type** | **描述**|
| ------------- | -------- | ------------------------------------------------------------ |
| **distance**  | float    |从**位置**到SDF定义的曲面的带符号距离。当**位置**位于形状外部时，该值为正值；当位于形状内部时，该值为负值**位置**。|
| **direction** | Vector3  |指向SDF定义的曲面上最近点的方向。|

### 附加说明

可以使用[定向框](Type-OrientedBox.md)设置SDF的位置，方向与比例。OrientedBox的中心与SDF的中心相对应。

#### 局限性

要使该操作符在世界坐标中输出正确的距离，OrientedBox的尺寸（大小）必须与用于烘焙SDF的长方体的尺寸匹配。如果设置不正确，纹理边界内部和外部的距离将具有不同的比例，这意味着输出不会显示预期的行为。

此外，如果将非均匀比例应用于SDF（即，与用于烘焙的长方体的尺寸不成比例），则会导致距离扭曲。
