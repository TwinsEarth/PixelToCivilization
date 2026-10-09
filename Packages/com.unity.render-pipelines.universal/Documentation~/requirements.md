# 要求和兼容性

本文介绍此包的系统要求及兼容性信息。

## Editor 兼容性

以下表格显示了 URP 包版本与不同 Unity 版本的兼容性：

| 包版本           | 最低 Unity 版本         | 最高 Unity 版本         |
| ---------------- | --------------------- | --------------------- |
| 16.0.x           | 2023.2                | 2023.x                |
| 15.0.x           | 2023.1                | 2023.1                |
| 14.0.x           | 2022.2                | 2022.x                |
| 13.x.x           | 2022.1                | 2022.1                |
| 12.0.x           | 2021.2                | 2021.3                |
| 11.0.0           | 2021.1                | 2021.1                |
| 10.x             | 2020.2                | 2020.3                |
| 9.x-preview      | 2020.1                | 2020.2                |
| 8.x              | 2020.1                | 2020.1                |
| 7.x              | 2019.3                | 2019.4                |

自 Unity 2021.1 版本以来，图形包已成为 [Unity 核心包](https://docs.unity.cn/cn/tuanjiemanual/Manual/pack-core.html)。

对于 Unity 的每个版本（alpha、beta、补丁版），Unity 主安装程序中包含以下图形包的最新版本：SRP Core、URP、HDRP、Shader Graph 和 VFX Graph。从 Unity 2021.1 版本开始，包管理器仅显示图形包的主要修订版本（所有 Unity 2021.1.x 版本为 11.0.0，所有 Unity 2021.2.x 版本为 12.0.0）。

你可以使用包管理器从磁盘安装不同版本的图形包，或者修改 `manifest.json` 文件。

## 渲染管线兼容性

使用 URP 创建的项目与高画质渲染管线 (HDRP) 或内置渲染管线不兼容。在开始开发之前，必须决定在项目中使用哪种渲染管线。有关选择渲染管线的信息，请参阅 Unity 手册中的 [渲染管线](https://docs.unity.cn/cn/tuanjiemanual/Manual/render-pipelines.html) 部分。

## Unity Player 系统要求

此包不会添加任何额外的特定平台要求。Unity Player 的一般系统要求仍然适用。有关 Unity 系统要求的更多信息，请参阅 [Unity 的系统要求](https://docs.unity.cn/cn/tuanjiemanual/Manual/system-requirements.html)。
