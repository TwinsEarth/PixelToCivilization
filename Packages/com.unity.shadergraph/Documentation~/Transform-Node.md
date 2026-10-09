
Transform 节点
============


描述
--


返回将输入值（**In**）从一个坐标空间变换为另一个坐标空间的结果。选择节点上的下拉选项来定义变换的起始空间和目标空间。


端口
--




| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | Vector 3 | 输入值 |
| Out | 输出 | Vector 3 | 输出值 |


控件
--

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| From | 下拉选单 | Object、View、World、Tangent、Absolute World | 选择变换的起始空间 |
| To | 下拉选单 | Object、View、World、Tangent、Absolute World | 选择变换的目标空间 |
| Type | 下拉选单 | Position、Direction、Normal | 选择处理转换的方式 |

节点设置控制
---

当您为变换节点选择 **Direction** 或 **Normal** 转换类型时，以下控制项会出现在图形检查器的节点设置选项卡上。**标准化输出**（Normalize Output）设置有助于提升性能。如果输出已标准化，或者不需要保持标准化输出，则可以禁用此设置。

| 名称 | 类型 | 描述 |
| --- | --- | --- |
| Normalize Output | 复选框 | 将输出向量的长度缩减为 1。 |

World 和 Absolute World
----------------------

使用 **World** 和 **Absolute World** 空间选项来变换[位置](Position-Node.md)值的坐标空间。**World** 空间选项使用可编程渲染管线的默认世界空间来变换位置值。**Absolute World** 空间选项在所有可编程渲染管线中使用绝对世界空间来变换位置值。

如果使用 **Transform 节点** 变换的坐标空间不是用于位置值的坐标空间，建议使用 **World** 空间选项。对不代表位置的值使用 **Absolute World** 可能会导致意外行为。

转换类型
------

选择 **Position** 类型以对变换应用平移。选择 **Direction**，如果输入值不表示表面法线（即表面朝向的方向）。选择 **Normal**，如果输入值表示表面法线（即表面朝向的方向）。



生成的代码示例
-------


以下示例代码表示此节点在每个 **Base** 模式下的一种可能结果。


**World > World**

```
float3 _Transform_Out = In;

```

**World > Object**

```
float3 _Transform_Out = TransformWorldToObject(In);

```

**World > Tangent**

```
float3x3 tangentTransform_World = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
float3 _Transform_Out = TransformWorldToTangent(In, tangentTransform_World);

```

**World > View**

```
float3 _Transform_Out = TransformWorldToView(In);

```

**World > Absolute World**

```
float3 _Transform_Out = GetAbsolutePositionWS(In);

```

**World > Screen**

```
float4 hclipPosition = TransformWorldToHClipDir(In);
float3 screenPos = hclipPosition.xyz / hclipPosition.w;
float3 _Transform_Out = float3(screenPos.xy * 0.5 + 0.5, screenPos.z);

```

**Object > World**

```
float3 _Transform_Out = TransformObjectToWorld(In);

```

**Object > Object**

```
float3 _Transform_Out = In;

```

**Object > Tangent**

```
float3x3 tangentTransform_World = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
float3 _Transform_Out = TransformWorldToTangent(TransformObjectToWorld(In), tangentTransform_World);

```

**Object > View**

```
float3 _Transform_Out = TransformWorldToView(TransformObjectToWorld(In));

```

**Object > Absolute World**

```
float3 _Transform_Out = GetAbsolutePositionWS(TransformObjectToWorld(In));

```

**Object > Screen**

```
float4 hclipPosition = TransformObjectToHClip(In);
float3 screenPos = hclipPosition.xyz / hclipPosition.w;
float3 _Transform_Out = float3(screenPos.xy * 0.5 + 0.5, screenPos.z);

```

**Tangent > World**

```
float3x3 transposeTangent = transpose(float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal));
float3 _Transform_Out = mul(In, transposeTangent).xyz;

```

**Tangent > Object**

```
float3x3 transposeTangent = transpose(float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal));
float3 _Transform_Out = TransformWorldToObject(mul(In, transposeTangent).xyz);

```

**Tangent > Tangent**

```
float3 _Transform_Out = In;

```

**Tangent > View**

```
float3x3 transposeTangent = transpose(float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal));
float3 _Transform_Out = TransformWorldToView(mul(In, transposeTangent).xyz);

```

**Tangent > Absolute World**

```
float3x3 transposeTangent = transpose(float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal));
float3 _Transform_Out = GetAbsolutePositionWS(mul(In, transposeTangent)).xyz;

```

**Tangent > Screen**

```
float3x3 transposeTangent = transpose(float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal));
float4 hclipPosition = TransformWorldToHClipDir(mul(In, transposeTangent).xyz);
float3 screenPos = hclipPosition.xyz / hclipPosition.w;
float3 _Transform_Out = float3(screenPos.xy * 0.5 + 0.5, screenPos.z);

```

**View > World**

```
float3 _Transform_Out = mul(UNITY_MATRIX_I_V, float4(In, 1)).xyz;

```

**View > Object**

```
float3 _Transform_Out = TransformWorldToObject(mul(UNITY_MATRIX_I_V, float4(In, 1) ).xyz);

```

**View > Tangent**

```
float3x3 tangentTransform_World = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
float3 _Transform_Out = TransformWorldToTangent(mul(UNITY_MATRIX_I_V, float4(In, 1) ).xyz, tangentTransform_World);

```

**View > View**

```
float3 _Transform_Out = In;

```

**View > Absolute World**

```
float3 _Transform_Out = GetAbsolutePositionWS(mul(UNITY_MATRIX_I_V, float4(In, 1))).xyz;

```

**View > Screen**

```
float4 hclipPosition = TransformWViewToHClip(In);
float3 screenPos = hclipPosition.xyz / hclipPosition.w;
float3 _Transform_Out = float3(screenPos.xy * 0.5 + 0.5, screenPos.z);

```

**Absolute World > World**

```
float3 _Transform_Out = GetCameraRelativePositionWS(In);

```

**Absolute World > Object**

```
float3 _Transform_Out = TransformWorldToObject(In);

```

**Absolute World > Object (in the High Definition Render Pipeline)**

```
float3 _Transform_Out = TransformWorldToObject(GetCameraRelativePositionWS(In));

```

**Absolute World > Tangent**

```
float3x3 tangentTransform_World = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
float3 _Transform_Out = TransformWorldToTangent(In, tangentTransform_World);

```

**Absolute World > View**

```
float3 _Transform_Out = GetCameraRelativePositionWS(In)

```

**Absolute World > Absolute World**

```
float3 _Transform_Out = In;

```

**Absolute World > Screen**

```
float4 hclipPosition = TransformWorldToHClip(GetCameraRelativePositionWS(In));
float3 screenPos = hclipPosition.xyz / hclipPosition.w;
float3 _Transform_Out = float3(screenPos.xy * 0.5 + 0.5, screenPos.z);

```

**Screen > World**

```
float3 _Transform_Out = ComputeWorldSpacePosition(In.xy, In.z, UNITY_MATRIX_I_VP);

```

**Screen > Object**

```
float3 worldPos = ComputeWorldSpacePosition(In.xy, In.z, UNITY_MATRIX_I_VP);
float3 _Transform_Out = TransformWorldToObject(worldPos);

```

**Screen > Tangent**

```
float3 worldPos = ComputeWorldSpacePosition(In.xy, In.z, UNITY_MATRIX_I_VP);
float3x3 tangentTransform_World = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
float3 _Transform_Out = TransformWorldToTangent(worldPos, tangentTransform_World);

```

**Screen > View**

```
float4 positionCS  = ComputeClipSpacePosition(In.xy, In.z);
float4 result = mul(UNITY_MATRIX_I_V, positionCS);
float3 _Transform_Out = result.xyz / result.w;

```

**Screen > Absolute World**

```
float3 _Transform_Out = GetAbsolutePositionWS(ComputeWorldSpacePosition(In.xy, In.z, UNITY_MATRIX_I_VP));

```

