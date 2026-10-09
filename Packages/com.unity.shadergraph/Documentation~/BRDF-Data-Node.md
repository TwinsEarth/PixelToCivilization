BRDF Data 节点
===

**描述**
---

该节点用于获取基于物理渲染（PBR）的 **BRDF（双向反射分布函数）** 数据，方便开发者进行自定义光照计算。

#### 支持的渲染管线

* [通用渲染管线（URP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest)，且仅作为 [Sub Graph](Sub-graph.md) 使用。

[高清渲染管线（HDRP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest) 暂**不**支持此着色器。

**端口**  
------  

| 名称               | 方向  | 类型    | 绑定 | 描述 |  
|--------------------|------|---------|---|-----------------------------|  
| Albedo            | 输入 | Vector3 | 无 | 输入 Albedo 颜色 |  
| Specular          | 输入 | Vector3 | 无 | 输入 Specular 反射率 |  
| Smoothness        | 输入 | Float   | 无 | 输入表面光滑度 |  
| Metallic          | 输入 | Float   | 无 | 输入金属度 |  
| BRDFDiffuse       | 输出 | Vector3 | 无 | 输出用于光照计算的 Diffuse |  
| BRDFSpecular      | 输出 | Vector3 | 无 | 输出用于光照计算的 Specular |  
| Reflectivity      | 输出 | Float   | 无 | 输出反射率 |  
| PerceptualRoughness | 输出 | Float   | 无 | 输出感知粗糙度 |  
| Roughness         | 输出 | Float   | 无 | 输出用于大部分光照计算的粗糙度 |  
| GrazingTerm       | 输出 | Float   | 无 | 输出 Grazing Term |  