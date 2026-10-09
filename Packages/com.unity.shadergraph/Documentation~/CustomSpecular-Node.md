CustomSpecular 节点 
================  

**描述**  
----  
用于计算用户自定义的高光（Specular）光照效果。通过输入材质光学特性、光泽度、法线、光源方向和视口方向，用户可以灵活定义高光的直接光照表现，适配不同的自定义材质需求。

**端口**  
------  
| 名称                  | 方向  | 类型       | 绑定          | 描述                                        |  
|-----------------------|-------|------------|---------------|---------------------------------------------|  
| Specular             | 输入  | Float / Vector 3   | 无            | 输入物质光学特性，非金属为 float，金属为 Vector 3 |  
| Smoothness           | 输入  | Float      | 无            | 输入光泽度信息                              |  
| Normal WS            | 输入  | Vector 3   | World Space   | 输入世界空间的法线信息                      |  
| Light Direction WS   | 输入  | Vector 3   | World Space   | 输入世界空间的灯光方向                      |  
| View Direction WS    | 输入  | Vector 3   | World Space   | 输入世界空间的视口方向                      |  
| Out                  | 输出  | Vector 3   | 无            | 输出自定义的高光照明信息                    |  


**注意事项**  
- **Specular** 需要输入物质的光学特性（Substance Optical Properties）：    
  - 非金属：一般使用小 float 值（如 0.04）代替。  
  - 金属：通过物理光学性质数据提供的 Vector 3 值表示。  

