Main Light Realtime Shadow 节点 
================  

**描述**  
----  
获取场景中主光源的实时阴影信息。此节点仅支持实时计算的阴影，不包含 ShadowMask 烘焙阴影信息，用于实现动态光源的阴影效果。

#### 支持的渲染管线

* [通用渲染管线（URP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest) 

[高清渲染管线（HDRP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest) 暂**不**支持此着色器。

**端口**  
------  
| 名称      | 方向  | 类型       | 绑定          | 描述                                |  
|-----------|-------|------------|---------------|-------------------------------------|  
| Position  | 输入  | Vector 3   | World Space   | 输入世界空间的顶点位置信息          |  
| Out       | 输出  | Float      | 无            | 输出实时阴影信息值（范围 0-1）       |  
