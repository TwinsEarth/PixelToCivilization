# Calculate Mass from Volume

菜单路径：**Attribute > Derived > Calculate Mass from Volume**

**Calculate Mass from Volume** 代码块根据粒子派生自 **Scale** 属性和代码块 **Density** 属性的体积，设置粒子的 **Mass** 属性。此代码块可用于计算具有不同比例的粒子的质量，令其在物理模拟过程中表现得令人信服。

## 代码块兼容性

此代码块兼容于以下上下文：

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Density** | Float | 基于体积的粒子质量属性。此属性的单位是 kg/dm<sup>3</sup>. |
