# 高动态范围 (HDR) 输出

[高动态范围](https://docs.unity.cn/cn/tuanjiemanual/Manual/HDR.html) 内容相比标准动态范围 (SDR) 内容具有更广的色域和更大的亮度范围。

URP 可以为支持该功能的显示器输出 HDR 内容。

## 如何启用 HDR 输出

按照以下步骤激活 HDR 输出：

1. 在 **Project** 窗口下的 **Assets** > **Settings** 位置找到 [URP 资源](./../universalrp-asset.md)。
2. 进入 **Quality** > **HDR**，勾选 **HDR** 复选框以启用 HDR。
3. 进入 **Edit** > **Project Settings** > **Player** > **Other Settings** 并启用以下设置：

    * **Allow HDR Display Output**
    * **Use HDR Display Output**

  > **注意**：仅在需要主显示器使用 HDR 输出时启用 **Use HDR Display Output**。

如果切换到未启用 HDR 的 URP 资源，URP 将禁用 HDR 输出，直到切换回已启用 HDR 的 URP 资源。

> **注意**：如果 HDR 输出处于激活状态，则色彩分级模式将回退到 HDR，即使 URP 资源中存在其他色彩分级模式。

## URP 中的 HDR 色调映射

启用 **Allow HDR Display Output** 后，需要配置 [色调映射](./../post-processing-tonemapping.md) 设置以匹配 HDR 输入。

为了有效配置这些设置，需了解与色调映射相关的某些值，它们决定了 HDR 输出的视觉特性。

### 重要的色调映射值

为了充分利用 HDR 显示器的能力，**Tonemapping** 配置必须考虑目标显示器的特性，特别是以下三个亮度值 (单位：nits)：

- **最低支持亮度**
- **最高支持亮度**
- **纸白值 (Paper White Value)**：此值表示显示器上呈现的纸张白色表面的亮度，决定了显示器的整体亮度。

**注意**：低动态范围 (LDR) 和高动态范围 (HDR) 内容在相同的纸白值下不会呈现相同的亮度。这是因为显示器会对 LDR 内容进行额外处理，使其亮度增加。因此，最佳实践是为应用程序实现一个校准菜单。

### 可用的用户界面取决于准确的纸白值

[Unlit](./../unlit-shader.md) 材质不受光照影响，因此在用户界面中通常使用 Unlit 材质。在不专门针对 HDR 显示器的情况下，Unlit 材质的亮度定义在 0 到 1 之间，其中 1 代表白色，0 代表黑色。

然而，在 HDR 模式下，URP 使用 **纸白值** 来决定 Unlit 材质的亮度，因为 HDR 亮度值可能超过 0 到 1 的范围。

因此，在 HDR 模式下，纸白值决定了 UI 元素的亮度，特别是白色元素的亮度，它们的亮度与纸白值匹配。

## 配置 HDR 色调映射设置

可以在 [Volume](./../Volumes.md) 组件设置中选择和调整色调映射模式。也可以使用脚本调整 HDR 色调映射的某些配置 (请参考 [HDROutputSettings API](#the-hdroutputsettings-api))。

启用 **Allow HDR Display Output** 后，HDR 色调映射选项将在 Volume 组件中可见。

### 色调映射模式

URP 提供两种 **Tonemapping** 模式：**Neutral** 和 **ACES**。每种色调映射模式都有其独特特性。

- **Neutral** 模式适用于不希望色调映射影响内容颜色的情况。
- **ACES** 模式使用电影工业标准的 ACES 参考色彩空间，提供具有电影感的高对比度效果。

### Neutral

| 属性 | 描述 |
| ---- | ---- |
| **Neutral HDR Range Reduction Mode** | 控制色调映射使用的曲线，选项包括：<ul><li>**BT2390** (默认)：基于 [BT.2390](https://www.itu.int/pub/R-REP-BT.2390) 广播标准。</li><li>**Reinhard**：一种简单的色调映射算子。</li></ul>此选项仅在启用 **Show Additional Properties** 时可见。 |
| **Hue Shift Amount** | 该值决定应用 HDR 设置后内容保留原始色调的程度。值为 0 时，色调映射仅影响亮度，尽量保持原始色相。 |
| **Detect Paper White** | 启用此选项后，URP 将使用显示器传递给引擎的纸白值。在某些情况下，该值可能不准确，建议为应用程序实现校准菜单，以确保用户在不同显示器上正确显示内容。 |
| **Paper White** | 显示器的纸白值。如果未启用 **Detect Paper White**，需要在此手动指定值。 |
| **Detect Brightness Limits** | 启用此选项后，URP 将使用显示器传递的最小和最大亮度值。但这些值可能不准确，因此建议实现校准菜单。 |
| **Min Nits** | 显示器的最低亮度值。如果未启用 **Detect Brightness Limits**，需要在此手动指定。 |
| **Max Nits** | 显示器的最高亮度值。如果未启用 **Detect Brightness Limits**，需要在此手动指定。 |

### ACES

ACES 模式提供针对 1000、2000 和 4000 nits 亮度的固定预设。建议实现校准菜单，让用户选择合适的预设。

| 属性 | 描述 |
| ---- | ---- |
| **ACES Preset** | 色调映射预设，选项包括：<ul><li>**ACES 1000 Nits** (默认)：适用于 1000 nits 显示器。</li><li>**ACES 2000 Nits**：适用于 2000 nits 显示器。</li><li>**ACES 4000 Nits**：适用于 4000 nits 显示器。</li></ul> |
| **Detect Paper White** | 启用此选项后，URP 使用显示器传递的纸白值。建议实现校准菜单，以便用户调整显示效果。 |
| **Paper White** | 纸白值。如果未启用 **Detect Paper White**，需要手动指定值。 |

### HDROutputSettings API

[HDROutputSettings](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/HDROutputSettings.html) API 允许启用或禁用 HDR 模式，并查询某些值 (例如纸白值)。

## 离屏渲染 (Offscreen Rendering)

当使用离屏渲染技术时，并非所有相机都直接输出到显示器，例如渲染到 Render Texture。在这些情况下，相机的输出会在应用后处理前使用。

引擎不会对使用离屏渲染的相机应用 HDR 输出处理，以防止 HDR 处理被应用两次。

## SDR 渲染

HDR 输出依赖于 HDR 渲染，以提供适用于色调映射和色彩编码的像素值。这些值通常大于 1，而 SDR 渲染的像素值在 0 到 1 之间。因此，在 HDR 输出中使用 SDR 渲染可能会导致图像曝光不足或饱和度过高。

## HDR 调试视图

URP 提供三种 HDR 调试视图，可在 **Window** > **Analysis** > **Render Pipeline Debugger** > **Lighting** > **HDR Debug Mode** 中访问。

### 兼容性

URP 仅在以下平台支持 HDR 输出：

* Windows (DirectX 11、DirectX 12、Vulkan)
* macOS (Metal)
* 游戏主机
* 具有 HDR 支持的 XR 设备

> **注意**：DirectX 11 仅在 Player 中支持 HDR 输出，不支持 Editor HDR 输出。
