# Branch

菜单路径：**Operator > Logic > Branch**

**Branch** 运算符计算一个谓词，并在谓词为`true`时返回指定为 **True** 的值，否则返回指定为 **False** 的值。

## 运算符属性

| **输入** | **类型** | **描述** |
| --- | --- | --- |
| **Predicate** | bool | 要测试的布尔值。如果为`true`，则此运算符输出指定为 **True** 的值。如果为`false`，则此运算符输出指定为 **False** 的值。 |
| **True** | [Configurable](#运算符属性) | **Predicate** 为`true`时输出的值。 |
| **False** | [Configurable](#运算符属性) | **Predicate** 为`false`时输出的值。 |

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **输出** | [Configurable](#运算符属性) | 输出值取决于 **Predicate**。如果 **Predicate** 为`true`，则输出值是指定为 **True** 的值。如果 **Predicate** 为`false`，则输出值是指定为 **False** 的值。 |

## 运算符配置

要查看该节点的配置，请单击节点标题上的**齿轮**图标。

| **属性** | **描述** |
| --- | --- |
| **类型** | **True** 和 **False** 端口以及输出值的值类型。有关此属性支持的类型的列表，请参阅[可用类型](#可用类型)。 |

### 可用类型

您可以将以下类型用于**输入值**和**输出**端口：

+   **Bool**
+   **Int**
+   **Uint**
+   **Float**
+   **Vector2**
+   **Vector3**
+   **Vector4**
+   **Gradient**
+   **AnimationCurve**
+   **Matrix**
+   **OrientedBox**
+   **Color**
+   **Direction**
+   **Position**
+   **Vector**
+   **Transform**
+   **Circle**
+   **ArcCircle**
+   **Sphere**
+   **ArcSphere**
+   **AABox**
+   **Plane**
+   **Cylinder**
+   **Cone**
+   **ArcCone**
+   **Torus**
+   **ArcTorus**
+   **Line**
+   **Flipbook**
+   **Camera**

此列表不包含与缓冲区或纹理对应的任何类型，因为无法在生成的 HLSL 代码中将这些类型指定为局部变量。
