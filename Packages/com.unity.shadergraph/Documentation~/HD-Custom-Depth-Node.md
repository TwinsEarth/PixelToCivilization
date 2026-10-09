Custom Depth 节点（HDRP）
========================

自定义深度节点（Custom Depth Node）用于访问HDRP分配的自定义通道深度缓冲区。

[](#render-pipeline-compatibility)渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线 (URP)** | **高清渲染管线 (HDRP)** |
| --- | --- | --- |
| Custom Depth Node | 否 | 是 |

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| **UV** | 输入 | Vector 4 | 屏幕位置（Screen Position） | 设置此节点采样的标准化屏幕坐标。 |
| **输出** | 输出 | Vector 4 | 无 | 该节点的输出值。 |

[](#depth-sampling-modes)深度采样模式
---------------------------------------------

| 名称 | 描述 |
| --- | --- |
| Linear01 | 线性深度值，范围在0到1之间。 |
| Raw | 原始深度值。 |
| Eye | 转换为视空间单位的深度值。 |

[](#generated-code-example)生成的代码示例
-------------------------------------------------

以下代码示例展示了该节点的可能输出效果之一。

```
void Unity_CustomDepth_LinearEye_float(float4 UV, out float Out)
{
    Out = LinearEyeDepth(SampleCustomDepth(UV.xy), _ZBufferParams);
}
```