高清采样缓冲区节点
=====================

[](#description)描述
---------------------------

高清采样缓冲区节点（HD Sample Buffer Node）直接从摄像机采样缓冲区。

[](#render-pipeline-compatibility)渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线 (URP)** | **高清渲染管线 (HDRP)** |
| --- | --- | --- |
| **HD Sample Buffer** | 否 | 是 |

[](#ports)端口
---------------

| **名称** | **方向** | **类型** | **绑定** | **描述** |
| --- | --- | --- | --- | --- |
| **UV** | 输入 | Vector 2 | UV | 输入UV值。 |
| **Sampler** | 输入 | SamplerState | 无 | 确定团结引擎用于采样缓冲区的采样器。 |
| **Output** | 输出 | Float | 无 | 输出值。 |

[](#controls)控件
---------------------

| **名称** | **类型** | **选项** | **描述** |
| --- | --- | --- | --- |
| Source Buffer | 下拉菜单 | World Normal, Roughness, Motion Vectors, PostProcess Input, Blit Source | 确定要采样的缓冲区。 |

[](#generated-code-example)生成代码示例
-------------------------------------------------

以下示例代码代表了此节点的一个可能输出：

```
float4 Unity_HDRP_SampleBuffer_float(float2 uv, SamplerState samplerState)
{
    return SAMPLE_TEXTURE2D_X_LOD(_CustomPostProcessInput, samplerState, uv * _RTHandlePostProcessScale.xy, 0);
}
```