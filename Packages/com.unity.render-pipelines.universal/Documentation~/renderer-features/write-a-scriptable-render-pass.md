# 编写 Scriptable Render Pass

以下示例创建一个 `ScriptableRenderPass` 实例，并执行以下步骤：

1. 使用 `RenderTextureDescriptor` API 创建临时渲染纹理。
2. 通过 `RTHandle` 和 `Blit` API，对相机输出应用两次 [自定义 Shader](#example-shader) 处理。

完成 Scriptable Render Pass 后，可以使用以下方法注入该 Pass：

- [使用 `RenderPipelineManager` API](../customize/inject-render-pass-via-script.md)
- [使用 Scriptable Renderer Feature](scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.md)


## 创建 Scriptable Render Pass

本节介绍如何创建一个 Scriptable Render Pass。

1. 创建新的 C# 脚本，命名为 `RedTintRenderPass.cs`。
2. 在脚本中删除 Unity 自动生成的代码，并添加以下 `using` 指令：

    ```C#
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    ```

3. 创建 `RedTintRenderPass` 类，并继承 **ScriptableRenderPass**：

    ```C#
    public class RedTintRenderPass : ScriptableRenderPass
    ```

4. 在类中添加 `Execute` 方法。Unity 每帧为每个相机调用此方法，用于实现 Scriptable Render Pass 的渲染逻辑。

    ```C#
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    { }
    ```

完整代码如下：

```C#
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RedTintRenderPass : ScriptableRenderPass
{
    public override void Execute(ScriptableRenderContext context,
        ref RenderingData renderingData)
    {
        
    }
}
```

## 实现自定义渲染 Pass 的设置

1. 添加 `Material` 字段，并在构造函数中使用该字段：

    ```C#
    private Material material;

    public RedTintRenderPass(Material material)
    {
        this.material = material;
    }
    ```

2. 添加 `RenderTextureDescriptor` 字段，并在构造函数中初始化：

    ```C#
    private RenderTextureDescriptor textureDescriptor;

    public RedTintRenderPass(Material material)
    {
        this.material = material;

        textureDescriptor = new RenderTextureDescriptor(Screen.width,
            Screen.height, RenderTextureFormat.Default, 0);
    }
    ```

3. 声明 `RTHandle` 字段，以存储临时红色渲染纹理的引用：

    ```C#
    private RTHandle textureHandle;
    ```

4. 实现 `Configure` 方法。Unity 在执行渲染 Pass 之前调用该方法。

    ```C#
    public override void Configure(CommandBuffer cmd,
        RenderTextureDescriptor cameraTextureDescriptor)
    {
        // 使红色渲染纹理大小与相机目标大小相同
        textureDescriptor.width = cameraTextureDescriptor.width;
        textureDescriptor.height = cameraTextureDescriptor.height;

        // 检查描述符是否变化，并在必要时重新分配 RTHandle
        RenderingUtils.ReAllocateIfNeeded(ref textureHandle, textureDescriptor);
    }
    ```

5. 使用 `Blit` 方法，对相机输出应用自定义 Shader 的两个 Pass 处理：

    ```C#
    public override void Execute(ScriptableRenderContext context,
        ref RenderingData renderingData)
    {
        // 从池中获取 CommandBuffer
        CommandBuffer cmd = CommandBufferPool.Get();

        RTHandle cameraTargetHandle =
            renderingData.cameraData.renderer.cameraColorTargetHandle;

        // 先使用第一个 Shader Pass 进行红色渲染
        Blit(cmd, cameraTargetHandle, textureHandle, material, 0);
        // 再使用第二个 Shader Pass 还原处理后的颜色
        Blit(cmd, textureHandle, cameraTargetHandle, material, 1);

        // 执行命令缓冲区并释放回池
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    ```

6. 实现 `Dispose` 方法，在渲染 Pass 执行完成后销毁材质和临时渲染纹理：

    ```C#
    public void Dispose()
    {
        #if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        #else
            Object.Destroy(material);
        #endif
        
        if (textureHandle != null) textureHandle.Release();
    }
    ```

### <a name="code-render-pass"></a>完整的自定义 Render Pass 代码

```C#
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RedTintRenderPass : ScriptableRenderPass
{
    private Material material;
    private RenderTextureDescriptor textureDescriptor;
    private RTHandle textureHandle;

    public RedTintRenderPass(Material material)
    {
        this.material = material;

        textureDescriptor = new RenderTextureDescriptor(Screen.width,
            Screen.height, RenderTextureFormat.Default, 0);
    }

    public override void Configure(CommandBuffer cmd,
        RenderTextureDescriptor cameraTextureDescriptor)
    {
        // 设置纹理大小与相机目标大小一致
        textureDescriptor.width = cameraTextureDescriptor.width;
        textureDescriptor.height = cameraTextureDescriptor.height;

        // 检查是否需要重新分配 RTHandle
        RenderingUtils.ReAllocateIfNeeded(ref textureHandle, textureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context,
        ref RenderingData renderingData)
    {
        // 获取 CommandBuffer
        CommandBuffer cmd = CommandBufferPool.Get();

        RTHandle cameraTargetHandle =
            renderingData.cameraData.renderer.cameraColorTargetHandle;

        // 先使用 Shader Pass 0 进行红色渲染
        Blit(cmd, cameraTargetHandle, textureHandle, material, 0);
        // 再使用 Shader Pass 1 还原颜色
        Blit(cmd, textureHandle, cameraTargetHandle, material, 1);

        // 执行命令缓冲区并释放
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        #if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        #else
            Object.Destroy(material);
        #endif

        if (textureHandle != null) textureHandle.Release();
    }
}
```

## <a name="example-shader"></a>用于红色渲染效果的自定义 Shader

```c++
Shader "CustomEffects/RedTint"
{
    HLSLINCLUDE
    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // Blit.hlsl 提供了顶点着色器 (Vert)、输入结构 (Attributes) 和输出结构 (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    
        float4 RedTint (Varyings input) : SV_Target
        {
            float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
            return float4(1, color.gb, 1);
        }

        float4 SimpleBlit (Varyings input) : SV_Target
        {
            float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
            return float4(color.rgb, 1);
        }
    
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            Name "RedTint"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment RedTint
            
            ENDHLSL
        }
        
        Pass
        {
            Name "SimpleBlit"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment SimpleBlit
            
            ENDHLSL
        }
    }
}
```
