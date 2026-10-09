Additional Lights Loop 节点
===

**描述**  
----  
该节点分为 **Additional Lights Loop Start** 节点与 **Additional Lights Loop End** 节点，开发者可搭配两者进行自定义的额外光源计算。

> 注意：该节点为组合节点，Start 与 End 节点需一起使用。

#### 支持的渲染管线

* [通用渲染管线（URP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest) 

[高清渲染管线（HDRP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest) 暂**不**支持此着色器。

**端口**  
------  

### Additional Lights Loop Start 节点

| 名称      | 方向  | 类型       | 绑定          | 描述                                |  
|-----------|-------|------------|---------------|-------------------------------------|  
| PositionWS | 输入 | Vector3 | World Space | 输入世界空间坐标 |
| ShadowMask | 输入 | Vector4 | 无 | 输入 ShadowMask |
| Additional Light Color | 输出 | Vector3 | 无 | 输出单个光源的颜色 |
| Additional Light Direction | 输出 | Vector3 | 无 | 输出单个光源的方向 |
| Additional Light Attenuation | 输出 | Float | 无 | 输出单个光源的衰减 |

### Additional Lights Loop End 节点

| 名称      | 方向  | 类型       | 绑定          | 描述                                |  
|-----------|-------|------------|---------------|-------------------------------------| 
| AdditionalLighting | 输入 | Vector3 | 无 | 输入单个光源的光照计算结果 |
| Additional Lighting | 输出 | Vector3 | 无 | 输出所有额外光源的光照计算结果 |