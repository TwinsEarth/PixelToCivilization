Ambient Occlusion 节点
===

**描述**
---
该节点用于计算场景中的**环境光遮蔽信息（Ambient Occlusion，AO）**，通过分析模型表面几何关系和屏幕空间深度，生成直接和间接光照的遮蔽强度数据。

#### 支持的渲染管线

* [通用渲染管线（URP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest) 

[高清渲染管线（HDRP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest) 暂**不**支持此着色器。

**端口**  
------  

| 名称      | 方向  | 类型       | 绑定          | 描述                                |  
|-----------|-------|------------|---------------|-------------------------------------| 
| Screen Space UV | 输入 | Vector4 | 无 | 输入屏幕空间 UV |
| Occlusion | 输入 | Float | 无 | 输入模型贴图的 Occlusion |
| Indirect Ambient Occlusion | 输出 | Float | 无 | 输出间接AO |
|Direct Ambient Occlusion | 输出 | Float | 无 | 输出直接AO |