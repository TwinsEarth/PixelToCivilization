Fresnel Equation 节点
=====================

描述
---------------------------

菲涅耳方程节点（Fresnel Equation Node）为菲涅耳组件添加影响材质交互的方程。您可以在 **Mode** 下拉菜单中选择方程。

可在 [refractiveindex.info](https://refractiveindex.info/) 上查找折射率的数值。

端口（Schlick）
---------------------------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| f0 | 输入 | Vector{1, 2, 3} | 无 | 表示表面正面的反射率，通常为介电材料的 0.02-0.08。 |
| DotVector | 输入 | Float | 无 | 表面法线与光线的点积。 |
| Fresnel | 输出 | 与 f0 相同 | 无 | 菲涅耳系数，描述反射或透射的光量。 |

端口（Dielectric）
---------------------------------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| IOR Source | 输入 | Vector | 无 | 光源所在介质的折射率。 |
| IOR Medium | 输入 | Vector | 无 | 光线折射进入的介质的折射率。 |
| DotVector | 输入 | Float | 无 | 表面法线与光线的点积。 |
| Fresnel | 输出 | 与 f0 相同 | 无 | 菲涅耳系数，描述反射或透射的光量。 |

端口（DielectricGeneric）
-----------------------------------------------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| IOR Source | 输入 | Vector | 无 | 光源所在介质的折射率。 |
| IOR Medium | 输入 | Vector | 无 | 光线折射进入的介质的折射率。 |
| IOR MediumK | 输入 | Vector | 无 | 折射介质的虚数部分折射率，或导致折射的介质。 |
| DotVector | 输入 | Float | 无 | 表面法线与光线的点积。 |
| Fresnel | 输出 | 与 f0 相同 | 无 | 菲涅耳系数，描述反射或透射的光量。 |

控制
---------------------

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| 模式 | 下拉菜单 | • **Schlick**: 基于 [Schlick 近似](https://en.wikipedia.org/wiki/Schlick%27s_approximation) 的近似模型。适用于空气和介电材料之间的交互。  <br>• **Dielectric**: 适用于两个介电材料之间的交互。例如，空气与玻璃、玻璃与水或水与空气。<br>• **DielectricGeneric**: 计算介电材料与金属之间的交互的 [菲涅耳方程](https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations)。例如，清漆与金属、玻璃与金属或水与金属。 <br>**注意**：如果 **IORMediumK** 值为 0，**DielectricGeneric** 将表现为 **Dielectric** 模式。 |

生成代码示例
-------------------------------------------------

以下示例代码表示该节点的一个可能输出：

```
void Unity_FresnelEquation_Schlick(out float Fresnel, float cos0, float f0)
{
    Fresnel = F_Schlick(f0, cos0);
}

void Unity_FresnelEquation_Dielectric(out float3 Fresnel, float cos0, float3 iorSource, float3 iorMedium)
{
    FresnelValue = F_FresnelDielectric(iorMedium/iorSource, cos0);
}

void Unity_FresnelEquation_DielectricGeneric(out float3 Fresnel, float cos0, float3 iorSource, float3 iorMedium, float3 iorMediumK)
{
    FresnelValue = F_FresnelConductor(iorMedium/iorSource, iorMediumK/iorSource, cos0);
}

```