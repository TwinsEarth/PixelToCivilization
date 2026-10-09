
Blackbody 节点
============

描述
--

对模拟黑体辐射的**渐变**进行采样。
此节点中的计算基于 Mitchell Charity 收集的数据。
此节点输出线性 RGB 空间的颜色，并使用一个 D65 白点和一个 CIE 1964 10 度的颜色空间执行转换。
有关更多信息，请参阅 [What color is a blackbody?](http://www.vendian.org/mncharity/dir3/blackbody/)


端口
--


| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Temperature | 输入 | Float | 无 | 进行采样的温度或温度贴图（以开尔文为单位）。 |
| Out | 输出 | Vector 3 | 无 | 颜色表示的强度（矢量 3 形式）。 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_Blackbody_float(float Temperature, out float3 Out)
{
    float3 color = float3(255.0, 255.0, 255.0);
    color.x = 56100000. * pow(Temperature,(-3.0 / 2.0)) + 148.0;
    color.y = 100.04 * log(Temperature) - 623.6;
    if (Temperature > 6500.0) color.y = 35200000.0 * pow(Temperature,(-3.0 / 2.0)) + 184.0;
    color.z = 194.18 * log(Temperature) - 1448.6;
    color = clamp(color, 0.0, 255.0)/255.0;
    if (Temperature < 1000.0) color *= Temperature/1000.0;
    Out = color;
}

```

