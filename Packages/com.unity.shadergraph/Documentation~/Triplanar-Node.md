Triplanar 节点
==============

描述
---------------------------

生成基于世界空间投影的 UV，并进行纹理采样。此方法通常用于对大模型（如地形）进行纹理映射，因为手动编写 UV 坐标会面临一些问题，或者在性能上不理想。此节点会在世界坐标的 x、y、z 三个轴上分别对输入的 **Texture** 进行三次采样。生成的信息会平面投影到模型上，并通过法线或表面角度进行混合。您可以使用 **Tile** 输入来缩放生成的 UV，并且可以通过 **Blend** 输入来控制最终的混合强度。

**Blend** 控制法线对每个平面样本的混合影响，并且应该大于或等于 0。**Blend** 的值越大，法线最对齐的平面对样本的贡献越大。（最大混合指数会根据平台和节点精度介于 17 和 158 之间。）**Blend** 为 0 时，各个平面会平等地获得权重，不受法线方向的影响。

通过更改 **Input Space** 来选择投影方式。您也可以通过输入 **Position** 和 **Normal** 来修改投影。

使用 **Type** 下拉框来更改输入 **Texture** 的预期类型。如果设置为 **Normal**，则 **Out** 端口会返回 **Normal Output Space** 中混合后的法线。

如果在使用此节点的图形中包含自定义功能节点或子图时遇到纹理采样错误，请升级到 10.3 或更高版本。

**注意**：此 **Triplanar Node** 只能在 **Fragment** 着色器阶段使用。

端口  
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Texture | 输入 | 纹理 | 无 | 输入纹理值 |
| Sampler | 输入 | 采样器状态 | 无 | 输入 **Texture** 的采样器 |
| Position | 输入 | Vector 3 | 输入空间位置 | 片段位置 |
| Normal | 输入 | Vector 3 | 输入空间法线 | 片段法线 |
| Tile | 输入 | Float | 无 | 生成 UV 的平铺量 |
| Blend | 输入 | Float | 无 | 各平面之间的混合因子 |
| Out | 输出 | Vector 4 | 无 | 输出值 |

控件 
---------------------

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Type | 下拉框 | 默认（Default）、法线（Normal） | 输入 **Texture** 的类型 |

节点设置控件 
-------------------------------------------------

在选择 **Triplanar Node** 时，**Graph Inspector** 的节点设置标签下会显示以下控件：

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Input Space | 下拉框 | Object, View, World, Tangent, AbsoluteWorld | 控制输入端口 **Position** 和 **Normal** 使用的坐标空间。当更改 **Input Space** 值时，**Position** 和 **Normal** 端口的绑定会更新为指定的空间。默认值为 **AbsoluteWorld**。 |
| Normal Output Space | 下拉框 | Object, View, World, Tangent, AbsoluteWorld | 控制 **Out** 端口使用的坐标空间。只有在 **Type** 设置为 **Normal** 时才会显示 **Normal Output Space** 控件。默认值为 **Tangent**。 |

生成的代码示例  
-------------------------------------------------

以下示例代码表示此节点的可能输出。

**Default**

```
float3 Node_UV = Position * Tile;
float3 Node_Blend = pow(abs(Normal), Blend);
Node_Blend /= dot(Node_Blend, 1.0);
float4 Node_X = SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.zy);
float4 Node_Y = SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xz);
float4 Node_Z = SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xy);
float4 Out = Node_X * Node_Blend.x + Node_Y * Node_Blend.y + Node_Z * Node_Blend.z;
```

**Normal**

```
float3 Node_UV = Position * Tile;
float3 Node_Blend = max(pow(abs(Normal), Blend), 0);
Node_Blend /= (Node_Blend.x + Node_Blend.y + Node_Blend.z ).xxx;
float3 Node_X = UnpackNormal(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.zy));
float3 Node_Y = UnpackNormal(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xz));
float3 Node_Z = UnpackNormal(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xy));
Node_X = float3(Node_X.xy + Normal.zy, abs(Node_X.z) * Normal.x);
Node_Y = float3(Node_Y.xy + Normal.xz, abs(Node_Y.z) * Normal.y);
Node_Z = float3(Node_Z.xy + Normal.xy, abs(Node_Z.z) * Normal.z);
float4 Out = float4(normalize(Node_X.zyx * Node_Blend.x + Node_Y.xzy * Node_Blend.y + Node_Z.xyz * Node_Blend.z), 1);
float3x3 Node_Transform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
Out.rgb = TransformWorldToTangent(Out.rgb, Node_Transform);
```