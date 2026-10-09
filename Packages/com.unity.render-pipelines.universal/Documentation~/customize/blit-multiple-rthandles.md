# 在屏幕上绘制多个 RTHandle 纹理

本页描述了涉及多个 `RTHandle` 纹理和自定义着色器效果的 blit 操作。描述使用了 URP 包示例中的 [DistortTunnel](../package-sample-urp-package-samples.md#renderer-features) 场景作为示例。

本页的代码示例来自 [URP 包示例](../package-sample-urp-package-samples.md) 中的以下场景：

    * Assets > Samples > Universal RP > 14.0.9 > URP Package Samples > RendererFeatures > DistortTunnel

示例场景使用以下资源来执行 blit 操作：

* 一个 [可编程渲染器特性](xref:UnityEngine.Rendering.Universal.ScriptableRendererFeature) 将三个 [渲染通道](xref:UnityEngine.Rendering.Universal.ScriptableRenderPass) 加入执行队列。

* 两个 [渲染通道](xref:UnityEngine.Rendering.Universal.ScriptableRenderPass) 创建中间纹理。最终的渲染通道绑定纹理并执行 blit 操作。

导入 [URP 包示例](../package-sample-urp-package-samples.md) 以访问完整的源代码和场景。

有关 blit 操作的一般信息，请参阅 [URP blit 最佳实践](../customize/blit-overview.md)。

## 在可编程渲染器特性中定义渲染通道

[渲染器特性](xref:UnityEngine.Rendering.Universal.ScriptableRendererFeature) 定义了渲染中间纹理和执行 blit 操作所需的渲染通道。

```C#
private DistortTunnelPass_CopyColor m_CopyColorPass;
private DistortTunnelPass_Tunnel m_TunnelPass;
private DistortTunnelPass_Distort m_DistortPass;
```

## RTHandle 变量

三个渲染通道和渲染器特性都声明了 `RTHandle` 变量，用于创建和处理示例效果的纹理。

例如，`DistortTunnelRendererFeature` 类声明了 `RTHandle` 变量，首先将它们作为参数传递给渲染通道，然后在最终的渲染通道（`DistortTunnelPass_Distort`）中使用生成的纹理。

示例中用于最终效果的 `Distort` 着色器使用了以下代码示例中的纹理。

```C#
private RTHandle m_CopyColorTexHandle;
private const string k_CopyColorTexName = "_TunnelDistortBgTexture";
private RTHandle m_TunnelTexHandle;
private const string k_TunnelTexName = "_TunnelDistortTexture";
```

为了在 RTHandle 系统中创建临时渲染纹理，渲染器特性中的 `SetupRenderPasses` 方法使用了 `ReAllocateIfNeeded` 方法：

```C#
RenderingUtils.ReAllocateIfNeeded(ref m_CopyColorTexHandle, desc, FilterMode.Bilinear,
    TextureWrapMode.Clamp, name: k_CopyColorTexName );
RenderingUtils.ReAllocateIfNeeded(ref m_TunnelTexHandle, desc, FilterMode.Bilinear,
    TextureWrapMode.Clamp, name: k_TunnelTexName );
```

## 配置输入和输出纹理

在此示例中，渲染通道中的 `SetRTHandles` 方法包含配置输入和输出纹理的代码。

例如，以下是 `DistortTunnelPass_Distort` 渲染通道中的 `SetRTHandles` 方法：

```C#
public void SetRTHandles(ref RTHandle copyColorRT, ref RTHandle tunnelRT, RTHandle dest)
{
    if (m_Material == null)
        return;
    
    m_OutputHandle = dest;
    m_Material.SetTexture(copyColorRT.name,copyColorRT);
    m_Material.SetTexture(tunnelRT.name,tunnelRT);
}
```

## 执行 blit 操作

此示例中有两个 blit 操作。

第一个 blit 操作在 `DistortTunnelPass_CopyColor` 渲染通道中。该通道将相机渲染的纹理 blit 到名为 `_TunnelDistortBgTexture` 的 `RTHandle` 纹理。

```C#
using (new ProfilingScope(cmd, m_ProfilingSampler))
{
    Blitter.BlitCameraTexture(cmd, m_Source, m_OutputHandle, 0);
}
```

第二个 blit 操作在 `DistortTunnelPass_Distort` 渲染通道中。

在执行 blit 之前，该通道在 `SetRTHandles` 方法中直接将两个源纹理绑定到材质：

```C#
m_Material.SetTexture(copyColorRT.name,copyColorRT);
m_Material.SetTexture(tunnelRT.name,tunnelRT);
```

然后该通道执行 blit 操作：

```C#
using (new ProfilingScope(cmd, m_ProfilingSampler))
{
    Blitter.BlitCameraTexture(cmd, m_OutputHandle, m_OutputHandle, m_Material, 0);
}
```

## 其他资源

* [在 URP 中执行全屏 blit](../renderer-features/how-to-fullscreen-blit.md)

    本页描述了基本的 blit 操作，并提供了完整的逐步实现说明。