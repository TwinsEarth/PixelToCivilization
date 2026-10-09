CustomLighting 节点 
================  

**描述**  
----  
该节点是一个**复合节点**，通过节点连接生成的简单 PBR 光照模型。它为物体提供基础的 PBR 光照计算，适用于快速且高效的材质表现。该节点计算包括反照率、金属度、光泽度、法线和环境遮蔽信息。


**端口**  
------  
| 名称          | 方向  | 类型       | 绑定          | 描述                                          |  
|---------------|-------|------------|---------------|-----------------------------------------------|  
| Albedo        | 输入  | Vector 3   | 无            | 输入反照率贴图                                |  
| Metallic      | 输入  | Float      | 无            | 输入金属度贴图                                |  
| Smoothness    | 输入  | Float      | 无            | 输入光泽度贴图                                |  
| NormalMap        | 输入  | Vector 3   | World Space   | 输入世界空间的法线信息                        |  
| Occlusion     | 输入  | Float      | 无            | 输入环境遮蔽信息                              |  
| Out           | 输出  | Vector 3   | 无            | 输出计算后的颜色信息，适用于 Fragment Emission  |  


**注意事项**  
- 此节点适用于需要简单 PBR 光照的场景，计算快速且高效。  
- 输出的颜色信息可以直接传入到 Fragment 的 Emission 中进行使用。  

**附加特性设置**  
- 支持在 **Shading Quality** 中调整以下属性：  
  - **Receive Global Illumination**：设置为 Off  
  - **Diffuse Quality**：设置为 None  
  - **Specular Quality**：设置为 None  

 