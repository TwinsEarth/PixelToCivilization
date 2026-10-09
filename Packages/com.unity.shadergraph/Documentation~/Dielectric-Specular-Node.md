
Dielectric Specular 节点
======================


描述
--


返回物理材质的**介电镜面反射 (Dielectric Specular)** F0 值。可使用[节点](Node.md)上的 **Material** 下拉选单参数选择要使用的材质。


**Common** **材质**类型定义了 0\.034 到 0\.048 的 sRGB 值范围。可使用 **Range** 参数选择这一范围内的值。此**材质**类型应当用于诸如塑料和织物之类的各种材质。


您可以使用 **Custom** 材料类型来定义自己的物理材质值。在这种情况下，输出值由其折射率定义。此值可由 **IOR** 参数设置。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Out | 输出 | Float | 无 | 输出值 |


控件
--




| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Material | 下拉选单 | Common、RustedMetal、Water、Ice、Glass、Custom | 选择要输出的材质值。 |
| Range | 滑动条 |  | 控制 **Common** 材质类型的输出值。 |
| IOR | 滑动条 |  | 控制 **Custom** 材质类型的折射率。 |


生成的代码示例
-------


以下示例代码表示此节点在每个**材质**模式下的一种可能结果。


**Common**



```
float _DielectricSpecular_Range = 0.5;
float _DielectricSpecular_Out = lerp(0.034, 0.048, _DielectricSpecular_Range);

```
**RustedMetal**



```
float _DielectricSpecular_Out = 0.030;

```
**Water**



```
float _DielectricSpecular_Out = 0.020;

```
**Ice**



```
float _DielectricSpecular_Out = 0.018;

```
**Glass**



```
float _DielectricSpecular_Out = 0.040;

```
**Custom**



```
float _DielectricSpecular_IOR = 1;
float _DielectricSpecular_Out = pow(_Node_IOR - 1, 2) / pow(_DielectricSpecular_IOR + 1, 2);

```

