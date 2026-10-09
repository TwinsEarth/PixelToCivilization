# 要求和兼容性
本页包含有关 Visual Effect Graph 包的系统要求和兼容性的信息。

## Unity 编辑器兼容性

下表显示了 Visual Effect Graph 版本与不同 Unity Editor 版本的兼容性。

| **package版本** | **最低 Unity 版本**	| **最高 Unity 版本** |
| ------------------- | ------------------------- | ------------------------- |
| 14.x                | 2022.2                    | 2022.2                    |
| 13.x                | 2022.1                    | 2022.1                    |
| 12.x                | 2021.2                    | 2021.2                    |
| 11.x                | 2021.1                    | 2021.1                    |
| 10.x                | 2020.2                    | 2020.3                    |
| 8.x / 9.x-preview   | 2020.1                    | 2020.1                    |
| 7.x                 | 2019.3                    | 2019.4                    |
| 6.x                 | 2019.2                    | 2019.2                    |

## 渲染管线兼容性

高清渲染管线 （HDRP） 和通用渲染管线 （URP） 之间的 Visual Effect Graph 兼容性各不相同。本节介绍了 Visual Effect Graph 版本与不同渲染管道的兼容性。

| **Package 版本** | **HDRP**       | **URP**       |
| ------------------- | -------------- | ------------- |
| 11.x                | Out of preview | In preview    |
| 10.x                | Out of preview | In preview    |
| 8.x / 9.x-preview   | Out of preview | In preview    |
| 7.x                 | Out of preview | In preview    |
| 6.x                 | In preview     | Not supported |

Visual Effect Graph 支持 Unity 2018.3 中的[高清渲染管道 （HDRP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html)，并已通过 Unity 2019.3 中的 HDRP 验证。Visual Effect Graph 支持 HDRP 支持的所有平台。有关这包括哪些平台的信息，请参阅 HDRP 的[系统要求](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/System-Requirements.html)。

**注意**：从 Package Manager 下载 HDRP 包时，Unity 会自动安装 Visual Effect Graph 包。

Visual Effect Graph 支持 Unity 2019.3 中的[通用渲染管道（URP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest/index.html) 。但是，URP 的预览版尚未结束，这意味着它仅支持 URP 支持的平台子集。它也不支持 HDRP 的所有功能，也只支持无光照粒子。

**注意**：在 URP 中，Visual Effect Graph 不支持 [Gamma 颜色空间](https://docs.unity3d.com/Manual/LinearRendering-LinearOrGammaWorkflow.html)。


## Unity Player 系统要求

+ Visual Effect Graph 的 Unity Player 系统要求取决于您使用的渲染管道：
    + Visual Effect Graph 在 HDRP 中已不再预览，这意味着它支持 HDRP 支持的所有平台。有关这包括哪些平台的信息，请参阅 HDRP 的[系统要求](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/System-Requirements.html)。
  + Visual Effect Graph 在 URP 中并未退出预览，这意味着它仅支持 URP 支持的部分平台。
+ 对于这两个渲染管道，最低硬件要求为：
    + 支持计算着色器。如果平台支持计算着色器，则返回 SystemInfo.[supportsComputeShaders](https://docs.unity3d.com/ScriptReference/SystemInfo-supportsComputeShaders.html).```true```
  + 支持 Shader Storage Buffer Objects （SSBO）。如果平台支持 SSBO，则它将为 [SystemInfo.maxComputeBufferInputsVertex](https://docs.unity3d.com/ScriptReference/SystemInfo-maxComputeBufferInputsVertex.html) 返回大于 0 的值。
+  Visual Effect Graph 并未停止在移动平台上的预览。
+ Visual Effect Graph 不支持 Open GL ES。

有关 Unity 播放器的一般系统要求的更多信息，请参阅 Unity 的[系统要求](https://docs.unity3d.com/Manual/system-requirements.html)。
