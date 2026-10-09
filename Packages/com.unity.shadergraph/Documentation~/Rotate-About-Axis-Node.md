
Rotate About Axis 节点
====================


描述
--

绕轴 **Axis** 将输入矢量 **In** 旋转值 **Rotation**。可以通过参数 **Unit** 选择旋转角度单位。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | Vector 3 | 无 | 输入值 |
| Axis | 输入 | Vector 3 | 无 | 要围绕其旋转的轴 |
| Rotation | 输入 | Float | 无 | 要应用的旋转量 |
| Out | 输出 | Vector 3 | 无 | 输出值 |


控件
--

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Unit | 下拉选单 | Radians、Degrees | 切换输入 **Rotation** 的单位 |


生成的代码示例
-------


以下示例代码表示此节点在每个 **Unit** 模式下的一种可能结果。


**Radians**



```
void Unity_RotateAboutAxis_Radians_float(float3 In, float3 Axis, float Rotation, out float3 Out)
{
    float s = sin(Rotation);
    float c = cos(Rotation);
    float one_minus_c = 1.0 - c;

    Axis = normalize(Axis);
    float3x3 rot_mat = 
    {   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
        one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
        one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
    };
    Out = mul(rot_mat,  In);
}

```
**Degrees**



```
void Unity_RotateAboutAxis_Degrees_float(float3 In, float3 Axis, float Rotation, out float3 Out)
{
    Rotation = radians(Rotation);
    float s = sin(Rotation);
    float c = cos(Rotation);
    float one_minus_c = 1.0 - c;

    Axis = normalize(Axis);
    float3x3 rot_mat = 
    {   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
        one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
        one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
    };
    Out = mul(rot_mat,  In);
}

```

