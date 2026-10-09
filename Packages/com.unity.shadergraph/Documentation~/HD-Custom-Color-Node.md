Custom Color 节点（HDRP）
========================

自定义颜色节点（Custom Color Node）用于访问HDRP分配的自定义通道颜色缓冲区。

[](#render-pipeline-compatibility)渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线 (URP)** | **高清渲染管线 (HDRP)** |
| --- | --- | --- |
| Custom Color Node | 否 | 是 |

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| **UV** | 输入 | Vector 4 | 屏幕位置（Screen Position） | 设置用于采样的标准化屏幕坐标。 |
| **Output** | 输出 | Vector 4 | 无 | 采样坐标位置的自定义通道颜色缓冲区中的值。 |

[](#generated-code-example)生成的代码示例
-------------------------------------------------

以下代码示例展示了该节点的可能输出效果之一。

```
void Unity_CustomDepth_LinearEye_float(float4 UV, out float Out)
{
    Out = SampleCustomColor(UV.xy);
}
```