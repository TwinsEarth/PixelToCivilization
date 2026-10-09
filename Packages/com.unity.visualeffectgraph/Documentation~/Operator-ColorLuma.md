# Color Luma

菜单路径：**Operator > Color > Color Luma**

**Color Luma** 运算符输出的是输入颜色的亮度（感知亮度）。

![](Images/Operator-ColorHSVLuma.png)

颜色的亮度值在许多情况下都很有用，例如：

+   您可以使用它重新着色粒子并保持其原始亮度。
+   您可以在模拟本身中使用它并使最亮的粒子移动得更快。

备注：颜色运算符在每个粒子级别上工作。要在每像素级别重新着色粒子的纹理，请使用系统输出上下文中的 **Color Mapping**，或者通过 [Shader Graph](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest/index.html) 创建自己的着色器。

## 运算符属性

| **Inputs** | **类型** | **描述** |
| --- | --- | --- |
| **Color** | Color | 要计算其亮度的颜色。 |

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **luma** | Color | **Color** 的亮度值。 |
