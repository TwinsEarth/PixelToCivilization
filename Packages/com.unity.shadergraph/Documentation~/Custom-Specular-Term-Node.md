Custom Specular Term 节点 
===  

**描述**  
---  
该节点用于计算 Specular Term（镜面反射项），根据输入的光照、观察方向、法线等参数，生成不同光照模型下的镜面反射计算结果。  

**端口**  
------  

| 名称                 | 方向  | 类型    | 绑定 | 描述 |  
|----------------------|------|---------|---|--------------------------------|  
| Specular            | 输入 | Vector3 | 无 | 输入 Specular 颜色 |  
| Roughness           | 输入 | Float   | 无 | 输入粗糙度 |  
| Light Direction WS  | 输入 | Vector3 | 无 | 输入光源方向（世界空间） |  
| View Direction WS   | 输入 | Vector3 | World Space | 输入观察方向（世界空间） |  
| Normal WS          | 输入 | Vector3 | World Space | 输入法线方向（世界空间） |  
| Tangent WS         | 输入 | Vector3 | World Space | 输入切线方向（世界空间） |  
| Bitangent WS       | 输入 | Vector3 | World Space | 输入斜切线方向（世界空间） |  
| Anisotropy         | 输入 | Float   | 无 | 输入各向异性数值 |  
| Specular Term      | 输出 | Vector3 | 无 | 输出 Specular Term（镜面反射项） |  

**控件**  
------  

| 名称                | 类型  | 描述 |  
|---------------------|------|----------------------------------------------------|  
| Specular Term Mode | 选项 | 选择输出的镜面反射模型，可选 Approx GGX、Blinn Phong、Anisotropy、CottonWool、Silk，所需输入数据会根据所选模型有所变化。 |  