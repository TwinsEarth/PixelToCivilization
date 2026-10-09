# Swizzle

菜单路径： **Operator > Math > Vector**

**Swizzle** [统一运算符](uniform-operators.md) 基于 **Mask** 重排字符串重新排列输入矢量的组件，并将其输出到输出矢量中。

例如，(x,y,z,w) 的矢量可以重排为 (y,x,x,z)。

## 运算符设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Mask** | string | 重排掩码。重排掩码是一至四个字符的组合，字符可为 x、y、z、w。 |

## 运算符属性

| **输入** | **类型** | **描述** |
| --- | --- | --- |
| **X** | [Configurable](#operator-configuration) | 要重排其值的矢量。 |

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **输出** | Dependent | 重排后的输出矢量（取决于 Mask 设置）。  </br>**Type** 会更改以匹配 **Mask** 中的字符数目。 |

## 运算符配置

要查看此[统一运算符](uniform-operators.md)的配置，请单击节点标题中的**齿轮**图标。您可以在此处配置此运算符使用的数据类型。

### 可用类型

您可以将以下类型用于**输入值**和端口：

+   **Position**
+   **Vector**
+   **Direction**
+   **Vector4**
+   **Vector3**
+   **Vector2**
+   **Float**
