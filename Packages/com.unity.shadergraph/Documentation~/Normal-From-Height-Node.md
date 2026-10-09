
Normal From Height 节点
=====================


描述
--


根据输入值 **Input** 定义的高度值以及输入值 **Strength** 定义的强度来创建法线贴图。


端口
--




| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | Float | 输入高度值 |
| Strength | 输入 | Float | 输出法线的强度。采用真实世界单位，推荐范围为 0 \- 0\.1。 |
| Out | 输出 | Vector 3 | 输出值 |


控件
--




| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Output Space | 下拉选单 | Tangent、World | 设置输出法线的坐标空间。 |


生成的代码示例
-------


以下示例代码表示此节点在每个 **Output Space** 模式下的一种可能结果。


**Tangent**



```
void Unity_NormalFromHeight_Tangent_float(float In, float Strength, float3 Position, float3x3 TangentMatrix, out float3 Out)
{
    float3 worldDerivativeX = ddx(Position);
    float3 worldDerivativeY = ddy(Position);

    float3 crossX = cross(TangentMatrix[2].xyz, worldDerivativeX);
    float3 crossY = cross(worldDerivativeY, TangentMatrix[2].xyz);
    float d = dot(worldDerivativeX, crossY);
    float sgn = d < 0.0 ?(-1.f) : 1.f;
    float surface = sgn / max(0.00000000000001192093f, abs(d));

    float dHdx = ddx(In);
    float dHdy = ddy(In);
    float3 surfGrad = surface * (dHdx*crossY + dHdy*crossX);
    Out = normalize(TangentMatrix[2].xyz - (Strength * surfGrad));
    Out = TransformWorldToTangent(Out, TangentMatrix);
}

```
**World**



```
void Unity_NormalFromHeight_World_float(float In, float Strength, float3 Position, float3x3 TangentMatrix, out float3 Out)
{
    float3 worldDerivativeX = ddx(Position);
    float3 worldDerivativeY = ddy(Position);

    float3 crossX = cross(TangentMatrix[2].xyz, worldDerivativeX);
    float3 crossY = cross(worldDerivativeY, TangentMatrix[2].xyz);
    float d = dot(worldDerivativeX, crossY);
    float sgn = d < 0.0 ?(-1.f) : 1.f;
    float surface = sgn / max(0.00000000000001192093f, abs(d));

    float dHdx = ddx(In);
    float dHdy = ddy(In);
    float3 surfGrad = surface * (dHdx*crossY + dHdy*crossX);
    Out = normalize(TangentMatrix[2].xyz - (Strength * surfGrad));
}

```

