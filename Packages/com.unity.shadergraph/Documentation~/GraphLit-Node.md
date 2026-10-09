GraphLit 节点  
================  

**描述**  
----  
该节点是一个**复合节点**，用节点连接生成的 Lit 节点，用于生成光照效果。您可以双击查看内部详情。

通过输入世界空间信息、材质特性和光学属性，计算并输出标准化的颜色信息。该节点支持直接将输出值传递给 Fragment 的 Emission，同时与 [**Scaleble Lit**](./ScalableLit.md) 兼容，用于自定义光照模型。

**端口**  
------  
| 名称                     | 方向  | 类型       | 绑定          | 描述                                        |  
|--------------------------|-------|------------|---------------|---------------------------------------------|  
| Position                | 输入  | Vector 3   | World Space   | 输入世界空间的位置信息                      |  
| View Direction          | 输入  | Vector 3   | World Space   | 输入世界空间的视口方向                      |  
| Albedo                  | 输入  | Vector 4   | 无            | 输入反照率贴图                              |  
| Smoothness              | 输入  | Float      | 无            | 输入光泽度贴图                              |  
| Metallic                | 输入  | Float      | 无            | 输入金属度贴图                              | 
| Normal                  | 输入  | Vector 3   | World Space   | 输入世界空间的法线信息                      |  
| Alpha                   | 输入  | Float      | 无            | 输入透明信息                                |  
| Occlusion               | 输入  | Float      | 无            | 输入环境遮蔽信息                            |  
| ClearCoatMask         | 输入  | Float      | 无            | 输入透明涂层遮罩                            |  
| ClearCoatSmoothness   | 输入  | Float      | 无            | 输入透明涂层光泽度                          |  
| Specular Highlights Off | 输入  | Boolean    | 无            | 关闭高光显示选项                            |  
| Out                     | 输出  | Vector 3   | 无            | 输出材质光照颜色信息                        |  


**附加特性设置**  
- 支持在 **Shading Quality** 中调整以下属性：  
  - **Receive Global Illumination**：设置为 Off  
  - **Diffuse Quality**：设置为 None  
  - **Specular Quality**：设置为 None  

**注意事项**  
- 此节点仅支持输出到 **Scalable Lit 节点**，适合高效实现标准化光照模型。  
