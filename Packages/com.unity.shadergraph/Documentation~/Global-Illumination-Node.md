Global Illumination 节点  
================  

**描述**  
----  
获取当前环境的全局照明（Global Illumination，GI）信息，通过输入照明所需的数据生成合成后的 GI 结果，包括间接高光、间接漫反射以及菲涅尔信息。此节点适用于自定义光照中的环境光计算，提供精确的间接光照表现。

#### 支持的渲染管线

* [通用渲染管线（URP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest) 

[高清渲染管线（HDRP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest) 暂**不**支持此着色器。

**端口**  
------  
| 名称                     | 方向  | 类型       | 绑定          | 描述                                        |  
|--------------------------|-------|------------|---------------|---------------------------------------------|  
| Albedo                  | 输入  | Vector 3   | 无            | 输入反照率贴图                              |  
| Metallic                | 输入  | Float      | 无            | 输入金属度贴图                              |  
| Specular                | 输入  | Float/Vector 3 | 无         | 输入物质光学特性，非金属为 float，金属为 Vector 3 |  
| Smoothness              | 输入  | Float      | 无            | 输入光泽度贴图                              |  
| Clear Coat Mask         | 输入  | Float      | 无            | 输入透明涂层遮罩                            |  
| Clear Coat Smoothness   | 输入  | Float      | 无            | 输入透明涂层光泽度                          |  
| Shadow Mask             | 输入  | Vector 4   | 无            | 输入烘焙阴影信息                            |  
| Bake GI                 | 输入  | Vector 3   | 无            | 输入烘焙的全局光照信息                      |  
| Occlusion               | 输入  | Float      | 无            | 输入环境遮蔽信息                            |  
| Position WS             | 输入  | Vector 3   | World Space   | 输入世界空间的位置信息                      |  
| Normal WS               | 输入  | Vector 3   | World Space   | 输入世界空间的法线信息                      |  
| View Direction WS       | 输入  | Vector 3   | World Space   | 输入世界空间的视口方向                      |  
| Color                   | 输出  | Vector 3   | 无            | 输出合成后的全局光照信息                    |  
 

**注意事项**  
- **Specular** 需要输入物质的光学特性（Substance Optical Properties）：  
  - 非金属：一般使用小 float 值（如 0.04）代替。  
  - 金属：通过物理光学性质提供的 Vector 3 值表示。  
- 输出的 GI 信息包括：  
  - **间接高光**  
  - **间接漫反射**  
  - **菲涅尔信息**  
- 此节点适用于高精度材质光照模型的自定义环境光计算。  

