Custom Diffuse Term 节点
===  

**描述**  
---  
该节点用于计算 Diffuse Term（漫反射项），根据输入的光照、观察方向、法线等参数，生成不同光照模型下的漫反射计算结果。  

**端口**  
------  

| 名称                  | 方向  | 类型    | 绑定 | 描述 |  
|-----------------------|------|---------|---|--------------------------------|  
| Diffuse              | 输入 | Vector3 | 无 | 输入 Diffuse 颜色 |  
| Light Direction WS   | 输入 | Vector3 | 无 | 输入光源方向（世界空间） |  
| View Direction WS    | 输入 | Vector3 | World Space | 输入观察方向（世界空间） |  
| Normal WS           | 输入 | Vector3 | World Space | 输入法线方向（世界空间） |  
| Perceptual Roughness | 输入 | Float   | 无 | 输入感知粗糙度 |  
| Diffuse Term        | 输出 | Vector3 | 无 | 输出 Diffuse Term（漫反射项） |  

**控件**  
------  

| 名称               | 类型  | 描述 |  
|--------------------|------|----------------------------------------------------|  
| Diffuse Term Mode | 选项 | 选择输出的漫反射模型，可选 Lambertian、Disney Diffuse、Fabric，所需输入数据会根据所选模型有所变化。 |  