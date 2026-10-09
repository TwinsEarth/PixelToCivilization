Refract 节点
============

[](#description)描述
---------------------------

可以使用折射节点为着色器添加折射效果。折射节点利用以下元素生成一个新的折射向量：

* 一个标准化的入射向量。
* 表面的标准化法向量。
* 光源和介质的折射率。

折射节点基于[斯涅尔定律](https://en.wikipedia.org/wiki/Snell%27s_law)中的原理。介质的折射率有一个角度，使表面表现得像一面完美的镜子。此角度称为全内反射角。为避免产生 NaN 结果，将折射节点的 **Mode** 设置为 **Safe**。这会使折射节点在达到临界角之前生成一个空向量，以避免全内反射。

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Incident | 输入 | Vector | 无 | 从光源到表面的标准化向量。例如，可以是从光源到像素或从摄像机到表面。 |
| Normal | 输入 | Vector | 无 | 引起折射的表面的标准化法向量。 |
| IOR Source | 输入 | Float | 无 | 光源所处介质的折射率。 |
| IOR Medium | 输入 | Float | 无 | 光线折射进入的介质的折射率。 |
| Refracted | 输出 | Vector | 无 | 折射后的向量。 |
| Intensity | 输出 | Float | 无 | 折射的强度。 |

[](#controls)控制
---------------------

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Mode | 下拉菜单 | Safe、CriticalAngle  |• **Safe**：在达到临界角时返回空向量结果，以避免 NaN 结果。  <br>• **CriticalAngle**：忽略 **Safe** 检查，可能产生 NaN 结果。  |

[](#generated-code-example)生成的代码示例
-------------------------------------------------

以下示例代码表示此节点的一种可能输出。

```
void Unity_RefractCriticalAngle(float3 Incident, float3 Normal, float IORInput, float IORMedium, out float Out)
{
    $precision internalIORInput = max(IORInput, 1.0);
    $precision internalIORMedium = max(IORMedium, 1.0);
    $precision eta = internalIORInput/internalIORMedium;
    $precision cos0 = dot(Incident, Normal);
    $precision k = 1.0 - eta*eta*(1.0 - cos0*cos0);
    Refracted = k >= 0.0 ? eta*Incident - (eta*cos0 + sqrt(k))*Normal : reflect(Incident, Normal);
    Intensity = internalIORSource <= internalIORMedium ?;
        saturate(F_Transm_Schlick(IorToFresnel0(internalIORMedium, internalIORSource), -cos0)) :
        (k >= 0.0 ? saturate(F_FresnelDielectric(internalIORMedium/internalIORSource, -cos0)) : 0.0);
}

void Unity_RefractSafe(float3 Incident, float3 Normal, float IORInput, float IORMedium, out float Out)
{
    $precision internalIORInput = max(IORInput, 1.0);
    $precision internalIORMedium = max(IORMedium, 1.0);
    $precision eta = internalIORInput/internalIORMedium;
    $precision cos0 = dot(Incident, Normal);
    $precision k = 1.0 - eta*eta*(1.0 - cos0*cos0);
    Refracted = eta*Incident - (eta*cos0 + sqrt(max(k, 0.0)))*Normal;
    Intensity = internalIORSource <= internalIORMedium ?;
        saturate(F_Transm_Schlick(IorToFresnel0(internalIORMedium, internalIORSource), -cos0)) :
        (k >= 0.0 ? saturate(F_FresnelDielectric(internalIORMedium/internalIORSource, -cos0)) : 1.0);
}
```