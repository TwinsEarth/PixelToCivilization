**Voronoi 节点**  
=================

描述
---  
生成基于输入 **UV** 的 Voronoi（或 [Worley](https://en.wikipedia.org/wiki/Worley_noise)）噪声。Voronoi 噪声是通过计算像素与点网格之间的距离生成的。通过使用输入 **角度偏移（Angle Offset）** 对这些点进行伪随机偏移，可以生成一组细胞簇。细胞的规模以及生成的噪声由输入 **细胞密度（Cell Density）** 控制。输出 **Cells** 包含原始的细胞数据。

您还可以选择使用两种不同的哈希方法来计算噪声。团结引擎的 Voronoi 节点默认使用 **Deterministic** 哈希，以确保跨平台的噪声生成结果一致性。

端口  
---
| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入的 UV 值 |
| Angle Offset | 输入 | Float | 无 | 点的偏移值 |
| Cell Density | 输入 | Float | 无 | 生成细胞的密度 |
| Out | 输出 | Float | 无 | 噪声输出值 |
| Cells | 输出 | Float | 无 | 原始细胞数据 |

控件
---  
| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Hash Type | 下拉菜单 | Deterministic, LegacySine | 选择用于生成随机数的哈希函数 |

生成的代码示例  
------
以下示例代码展示了此节点的可能实现结果：

```
inline float2 unity_voronoi_noise_randomVector(float2 UV, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
}

void Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);

    for(int y=-1; y<=1; y++)
    {
        for(int x=-1; x<=1; x++)
        {
            float2 lattice = float2(x,y);
            float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset);
            float d = distance(lattice + offset, f);
            if(d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                Out = res.x;
                Cells = res.y;
            }
        }
    }
}
```