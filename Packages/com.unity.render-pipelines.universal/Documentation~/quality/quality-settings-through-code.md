# 通过代码控制 URP 质量设置

Unity 提供了多个预设的 [质量设置](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-QualitySettings.html)，您可以根据需要为项目添加更多质量级别。为了适应不同的硬件规格，您可以在 C# 脚本中切换这些质量级别以及相关的 URP 资源 (URP Asset)。以下示例演示如何使用 API 更改质量设置级别和当前的 URP 资源，并在运行时修改 URP 资源的特定设置。

**注意**：仅在性能不关键的阶段（如加载屏幕或静态菜单）更改质量设置和 URP 资源设置，因为这些更改会导致短暂但显著的性能影响。

## 在运行时更改 URP 资源

每个质量级别都使用一个 URP 资源来控制多个特定的图形设置。您可以为不同的质量级别分配不同的 URP 资源，并在运行时切换它们。

### 配置项目质量设置

要使用质量设置在 URP 资源之间切换，请确保项目的质量级别已配置为使用不同的 URP 资源。URP 3D 示例场景默认具有此配置。

1. 为每个质量级别创建一个 URP 资源。右键点击 **Project** 窗口，选择 **Create** > **Rendering** > **URP Asset (with Universal Renderer)**。

    > **注意**：这些说明同样适用于使用 2D 渲染器的 URP 资源。

2. 根据需要配置并命名新的 URP 资源。
3. 打开 **Project Settings** 下的 **Quality** 部分 (**Edit** > **Project Settings** > **Quality**)。
4. 为每个质量级别分配 URP 资源。选择质量级别后，在 **Rendering** > **Render Pipeline Asset** 选项中选择适用于该质量级别的 URP 资源。

现在，项目的质量级别已准备好在运行时切换不同的 URP 资源。

### 更改质量级别

您可以使用 [QualitySettings API](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/QualitySettings.html) 在运行时更改 Unity 使用的质量级别。当质量级别按照上述方式设置后，切换质量级别不仅可以更改 URP 资源，还可以应用对应的质量设置预设。

以下示例展示了一个 C# 脚本，它在用户启动构建项目时，根据系统的图形内存大小选择适当的质量级别。

1. 创建一个新的 C# 脚本，命名为 `QualityControls`。
2. 打开 `QualityControls` 脚本，并在 `QualityControls` 类中添加 `SwitchQualityLevel` 方法。

    ```C#
    using UnityEngine;

    public class QualityControls : MonoBehaviour
    {
        void Start()
        {
        }
        
        private void SwitchQualityLevel()
        {
        }
    }
    ```

3. 在 `SwitchQualityLevel` 方法中添加 `switch` 语句，使用 `QualitySettings.SetQualityLevel()` 方法选择质量级别。

    > **注意**：每个质量级别都有一个索引，对应 **Project Settings** 窗口 **Quality** 部分中的顺序。列表顶部的质量级别索引为 0，索引仅计算针对目标平台启用的质量级别。

    ```C#
    private void SwitchQualityLevel()
    {
        switch (SystemInfo.graphicsMemorySize)
        {
            case <= 2048:
                QualitySettings.SetQualityLevel(1);
                break;
            case <= 4096:
                QualitySettings.SetQualityLevel(2);
                break;
            default:
                QualitySettings.SetQualityLevel(0);
                break;
        }
    }
    ```

4. 在 `Start` 方法中调用 `SwitchQualityLevel`，确保质量级别仅在场景首次加载时更改。

    ```C#
    void Start()
    {
        SwitchQualityLevel();
    }
    ```

5. 在场景中创建一个空 GameObject，并命名为 `QualityController`。
6. 在 **Inspector** 窗口中，向 `QualityController` 物体添加 `QualityControls` 组件。

当此场景加载时，Unity 将运行 `SwitchQualityLevel` 方法，检测系统的图形内存并设置相应的质量级别，该质量级别将自动应用相应的 URP 资源。

## 运行时更改 URP 资源设置

您可以在运行时通过 C# 脚本更改 URP 资源的某些属性。这有助于在质量级别之间进行更精细的性能调整，尤其是当设备的硬件规格与任何预设质量级别不完全匹配时。

> **注意**：要通过 C# 脚本更改 URP 资源的属性，该属性必须具有 `set` 方法。有关可访问的属性，请参阅 [可修改的属性](#可修改的属性)。

以下示例扩展了 `QualityControls` 脚本的功能，新增一个方法用于定位当前 URP 资源，并根据硬件性能动态调整其部分设置。

1. 在 `QualityControls` 脚本顶部添加 `using UnityEngine.Rendering` 和 `using UnityEngine.Rendering.Universal`。
2. 在 `QualityControls` 类中添加 `ChangeAssetProperties` 方法。

    ```C#
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    public class QualityControls : MonoBehaviour
    {
        void Start()
        {
            SwitchQualityLevel();
            ChangeAssetProperties();
        }

        private void SwitchQualityLevel()
        {
            // 代码与前例相同
        }

        private void ChangeAssetProperties()
        {
            UniversalRenderPipelineAsset data = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (!data) return;

            switch (SystemInfo.graphicsMemorySize)
            {
                case <= 1024:
                    data.renderScale = 0.7f;
                    data.shadowDistance = 50.0f;
                    break;
                case <= 3072:
                    data.renderScale = 0.9f;
                    data.shadowDistance = 150.0f;
                    break;
                default:
                    data.renderScale = 1.0f;
                    data.shadowDistance = 200.0f;
                    break;
            }
        }
    }
    ```

此方法在场景加载时运行，它会根据设备的图形内存大小动态调整 `renderScale` 和 `shadowDistance` 等属性，以优化性能。

## 可修改的属性

URP 资源的以下属性在运行时可以通过 C# 进行修改：

- `cascadeBorder`
- `colorGradingLutSize`
- `colorGradingMode`
- `conservativeEnclosingSphere`
- `fsrOverrideSharpness`
- `fsrSharpness`
- `hdrColorBufferPrecision`
- `maxAdditionalLightsCount`
- `msaaSampleCount`
- `numIterationsEnclosingSphere`
- `renderScale`
- `shadowCascadeCount`
- `shadowDepthBias`
- `shadowDistance`
- `shadowNormalBias`
- `storeActionsOptimization`
- `supportsCameraDepthTexture`
- `supportsCameraOpaqueTexture`
- `supportsDynamicBatching`
- `supportsHDR`
- `upscalingFilter`
- `useAdaptivePerformance`
- `useSRPBatcher`

有关这些属性的详细信息，请参考 [Universal Render Pipeline Asset API](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest/api/UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.html#properties)。

通过结合质量级别切换和属性调整，您可以在不需要为每种硬件配置单独创建质量级别的情况下，对不同设备的性能进行微调。
