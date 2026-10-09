

Metal Reflectance 节点
====================


描述
--


返回物理材质的**金属反射 (Metal Reflectance)** 值。可使用[节点](Node.md)上的 **Material** 下拉选单参数选择要使用的材质。


在 [PBR 主节点](PBR-Master-Node.md)上使用 **Specular** **Workflow** 时，应将此值提供给 **Specular** [端口](Port.md)。使用 **Metallic** **Workflow** 时，应将此值提供给 **Albedo** 端口。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Out | 输出 | Vector 3 | 无 | 输出值 |


控件
--

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Material | 下拉选单 | Iron、Silver、Aluminium、Gold、Copper、Chromium、Nickel、Titanium、Cobalt、Platform | 选择要输出的材质值。 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。


**Iron**



```
float3 _MetalReflectance_Out = float3(0.560, 0.570, 0.580);

```
**Silver**



```
float3 _MetalReflectance_Out = float3(0.972, 0.960, 0.915);

```
**Aluminium**



```
float3 _MetalReflectance_Out = float3(0.913, 0.921, 0.925);

```
**Gold**



```
float3 _MetalReflectance_Out = float3(1.000, 0.766, 0.336);

```
**Copper**



```
float3 _MetalReflectance_Out = float3(0.955, 0.637, 0.538);

```
**Chromium**



```
float3 _MetalReflectance_Out = float3(0.550, 0.556, 0.554);

```
**Nickel**



```
float3 _MetalReflectance_Out = float3(0.660, 0.609, 0.526);

```
**Titanium**



```
float3 _MetalReflectance_Out = float3(0.542, 0.497, 0.449);

```
**Cobalt**



```
float3 _MetalReflectance_Out = float3(0.662, 0.655, 0.634);

```
**Platinum**



```
float3 _MetalReflectance_Out = float3(0.672, 0.637, 0.585);

```

