
Rotate 节点
=========


描述
--


围绕输入 **Center** 定义的参考点将输入 **UV** 的值旋转输入 **Rotation** 的大小。可以通过参数 **Unit** 选择旋转角度单位。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入 UV 值 |
| Center | 输入 | Vector 2 | 无 | 旋转中心点 |
| Rotation | 输入 | Float | 无 | 要应用的旋转量 |
| Out | 输出 | Vector 2 | 无 | 输出 UV 值 |


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
void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
{
    UV -= Center;
    float s = sin(Rotation);
    float c = cos(Rotation);
    float2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix * 2 - 1;
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;
    Out = UV;
}

```
**Degrees**



```
void Unity_Rotate_Degrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
{
    Rotation = Rotation * (3.1415926f/180.0f);
    UV -= Center;
    float s = sin(Rotation);
    float c = cos(Rotation);
    float2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix * 2 - 1;
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;
    Out = UV;
}

```
