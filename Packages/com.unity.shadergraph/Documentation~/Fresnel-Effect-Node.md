
Fresnel Effect 节点
=================

描述
--

**菲涅耳效应 (Fresnel Effect)** 是根据视角不同而在表面上产生不同反射率（接近掠射角时的反射光增多）的效果。**Fresnel Effect** 节点通过计算表面法线和视图方向之间的角度来模拟这一点。该角度越宽，返回值越大。这种效果通常用于实现在许多艺术风格中很常见的边缘光照。


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| Normal | 输入 | Vector 3 | 法线方向。默认情况下绑定到世界空间法线 |
| View Dir | 输入 | Vector 3 | 视图方向。默认情况下绑定到世界空间视图方向 |
| Power | 输入 | Float | 强度计算指数 |
| Out | 输出 | Float | 输出值 |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。

```
void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
{
    Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
}

```

