# Get Texture Dimensions

菜单路径 : **Operator > Sampling > Get Texture Dimensions**

Get Texture Dimensions （获取纹理维度） 操作器允许您获取给定纹理的维度。

此 Operator 处理 Visual Effect Graph 中所有支持的纹理类型。当您将纹理附加到 input 属性时，它会自动推断 input 类型。您还可以在 [运算符属性](#运算符属性) 中手动指定输入类型。

## 运算符属性

| **Input** | **类型**                                 | **描述**                         |
| --------- | ---------------------------------------- | --------------------------------------- |
| **Tex**   | [Configurable](#operator-configuration). |要从中获取维度的纹理。
 |

| **Output** | **类型** | **描述**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| **width**  | uint     | 纹理的宽度。                                    |
| **height** | uint     | 纹理的高度。                  |
| **depth**  | uint     | 纹理 3D 的深度（仅适用于 Texture3D 类型）。     |
| **count**  | uint     | 纹理数组中的层计数（仅适用于 Texture2DArray 和 TextureCubeArray）。 |

## 运算符配置

要查看 Operator 的配置，请单击 Operator 标题中的**齿轮**图标。使用下拉列表选择 **Tex** 端口的类型。有关此属性支持的类型的列表，请参阅 [可用类型](#可用类型)。

### 可用类型

所有类型的纹理都可用于 **Texture** 端口：

- **Texture2D**
- **Texture3D**
- **Texture2DArray**
- **TextureCube**
- **TextureCubeArray**
