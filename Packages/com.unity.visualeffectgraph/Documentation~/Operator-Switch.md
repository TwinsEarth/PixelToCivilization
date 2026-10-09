# Switch

菜单路径：**Operator > Logic > Switch**

**Switch** 运算符将其输入与 case 值进行比较，并根据输入与 case 的匹配结果输出一个值：

+   如果输入匹配某个 case 值，则此运算符输出与匹配的 case 对应的值。
+   如果输入与 case 值不匹配，则此运算符输出默认值。

它的工作方式与 C# 中的 [switch 语句](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/switch)相似。

## 运算符设置

| **属性** | **类型** | **描述** |
| --- | --- | --- |
| **Entry Count** | Unsigned int | 要测试的 case 的数量。最大值为 **32**。 |
| **Custom Case Value** | bool | （**检查器**）启用后，您可以为每个 case 指定自定义 case 整数。否则，此运算符使用默认自然数。 |

## 运算符属性

| **输入** | **类型** | **描述** |
| --- | --- | --- |
| **Test Value** | int | 输入整数值，将使用 case 条目对其进行测试。如果您启用 **Custom Case Value** 并且此值匹配多个 case，则此运算符输出匹配的第一个条目。 |
| **Case 0** | int | 用于测试第一个 case 的值。如果您分配此端口，它将重写此运算符据之检查 **Test Value** 的值。此端口的默认值为 **0**。此端口仅在您启用 **Custom Case Value** 时显示。 |
| **Value 0** | [Configurable](#运算符设置) | 当 **Test Value** 匹配 **Case 0** 时要输出的值。 |
| **Case 1** | int | 用于测试第一个 case 的值。如果您分配此端口，它将重写此运算符据之检查 **Test Value** 的值。此端口的默认值为 **1**。此端口仅在您启用 **Custom Case Value** 时显示。 |
| **Value 1** | [Configurable](#运算符设置) | 当 **Test Value** 匹配 **Case 1** 时要输出的值。 |
| **Case N** | int | 要公开更多案件，请增加 **Entry Count**。 |
| **Value N** | [Configurable](#运算符设置) | 要公开更多值，请增加 **Entry Count**。 |
| **Default** | [Configurable](#运算符设置) | 当没有 case 匹配时要输出的默认值。 |

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **输出** | [Configurable](#运算符设置) | 对应于与 **Test Value** 匹配的 case 的值，若无 case 匹配，则为 **Default**。 |

## 运算符配置

要查看该运算符的配置，请单击运算符标题上的**齿轮**图标。

| **属性** | **描述** |
| --- | --- |
| **类型** | 此运算符使用的值类型。有关此属性支持的类型的列表，请参阅[可用类型](#可用类型)。 |

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
