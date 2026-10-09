Additional Lighting 节点
================  

**描述**  
----  
获取系统主光源以外的光照计算结果。通过输入多个属性贴图和空间信息，可以动态调整附加光源的照明效果。此节点主要用于细化材质的光照表现，例如添加环境光遮蔽、透明涂层或特殊光照效果。

#### 支持的渲染管线

* 通用渲染管线

高清渲染管线**不**支持此节点。

**端口**  
------  
| 名称                     | 方向  | 类型       | 绑定          | 描述                                        |  
|--------------------------|-------|------------|---------------|---------------------------------------------|  
| Albedo                  | 输入  | Vector 3   | 无            | 输入反照率贴图                              |  
| Metallic                | 输入  | Float      | 无            | 输入金属度贴图                              |  
| Specular                | 输入  | Float/Vector 3 | 无         | 输入物质光学特性，非金属为 float，金属为 Vector 3 |  
| Smoothness              | 输入  | Float      | 无            | 输入光泽度贴图                              |  
| Occlusion               | 输入  | Float      | 无            | 输入环境遮蔽信息                            |  
| Alpha                   | 输入  | Float      | 无            | 输入透明信息                                |  
| Shadow Mask             | 输入  | Vector 4   | 无            | 输入烘焙阴影数据                            |  
| Normal WS               | 输入  | Vector 3   | World Space   | 输入世界空间的法线信息                      |  
| Position WS             | 输入  | Vector 3   | World Space   | 输入世界空间的位置信息                      |  
| View Direction WS       | 输入  | Vector 3   | World Space   | 输入世界空间的视口方向                      |  
| Clear Coat Mask         | 输入  | Float      | 无            | 输入透明涂层遮罩                            |  
| Clear Coat Smoothness   | 输入  | Float      | 无            | 输入透明涂层光泽度                          |  
| Specular Highlights Off | 输入  | Boolean    | 无            | 关闭高光显示选项                            |  
| Out                     | 输出  | Vector 3   | 无            | 输出附加光照的信息                  |  


**注意事项**  
- **Specular** 需要输入物质的光学特性（Substance Optical Properties）：  
  - 非金属：一般使用小 float 值（如 0.04）代替。  
  - 金属：通过物理光学性质数据提供的 Vector 3 值表示。  
- 此节点的输出为主光源之外的光照信息，可用于增强材质的环境光效果或特定的光照调整。  


