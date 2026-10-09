
Transformation Matrix 节点
========================


描述
--


在着色器中定义通用**变换矩阵**的常量**矩阵 4x4** 值。可从下拉选单参数中选择**变换矩阵**。

该节点的两个输出值选项 **“反投影”**（Inverse Projection）和 **“反视图投影”**（Inverse View Projection）与 内置渲染管线目标不兼容。当您选择这两个选项中的任何一个并将内置渲染管线作为目标时，该节点将产生完全黑色的结果。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Out | 输出 | Matrix 4 | 无 | 输出值 |


控件
--




| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
|  | 下拉选单 | Model、InverseModel、View、InverseView、Projection、InverseProjection、ViewProjection、InverseViewProjection | 设置输出值 |


生成的代码示例
-------


以下示例代码表示此节点在每个模式下的一种可能结果。


**Model**



```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_M;

```
**InverseModel**



```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_I_M;

```
**View**



```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_V;

```
**InverseView**



```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_I_V;

```
**Projection**



```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_P;

```
**InverseProjection**



```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_I_P;

```
**ViewProjection**



```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_VP;

```
**InverseViewProjection**



```
float4x4 _TransformationMatrix_Out = UNITY_MATRIX_I_VP;

```

