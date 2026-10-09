# 在现有项目中安装 URP

你可以通过 [**Package Manager（包管理器）**](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html) 下载并安装最新版本的 **通用渲染管线（URP）**，然后在项目中启用它。  
如果你是从零开始创建 URP 项目，请参考 [如何从模板创建 URP 项目](creating-a-new-project-with-urp.md)。


## 开始之前

URP 采用集成的后处理解决方案，不兼容 Post Processing Version 2（PPv2）。  
如果你的项目安装了 PPv2，在安装 URP 之前必须先删除该包。  
安装 URP 后，你需要重新创建后处理效果。

> **注意**：  
> 目前 URP 不支持自定义后处理效果，如果你的项目使用了自定义后处理，则暂时无法在 URP 中重新创建。未来版本将提供支持。


## 安装 URP

1. 在 Unity 编辑器 中，打开你的项目。
2. 在顶部菜单栏，选择 **Window > Package Manager**，打开 **Package Manager 窗口**。
3. 在 Packages 下拉菜单中，选择 **Unity Registry**，此时会显示当前 Unity 版本 可用的所有包。
4. 在列表中找到 **Universal RP** 并选中。
5. 在 Package Manager 窗口 右下角，点击 **Install**。Unity 将 URP 直接安装到你的项目中。


## 配置 URP

安装 URP 后，你需要进行配置，包括**创建 URP 资源（URP Asset）** 并调整 **Graphics 设置**。

### 创建 URP 资源（URP Asset）

URP 资源（URP Asset）包含全局渲染和质量设置，并负责创建渲染管线实例，  
渲染管线实例管理中间资源和渲染管线实现。

创建 URP 资源 的步骤：

1. 在 Project 窗口 中，右键点击空白处，选择 **Create > Rendering > URP Asset (with Universal Renderer)**。  
   或者，在 顶部菜单栏 选择 **Assets > Create > Rendering > URP Asset (with Universal Renderer)**。
2. 你可以保留默认名称，或者输入新的名称。


### 设置 URP 为默认渲染管线

要启用 URP 作为项目的默认渲染管线：

1. 在项目中找到你要使用的 URP 资源（URP Asset）。  
   提示：在 搜索栏 输入 `t:universalrenderpipelineasset` 可快速查找所有 URP 资源。
2. 打开 **Edit > Project Settings > Graphics**。
3. 在 **Scriptable Render Pipeline Settings** 字段中，选择 **URP 资源**。  
   选择后，Graphics 设置会立即应用 URP。

可选：为不同质量级别设置 URP 资源

1. 打开 **Edit > Project Settings > Quality**。
2. 选择一个 **质量级别（Quality Level）**，然后在 **Render Pipeline Asset** 字段中选择 **URP 资源**。


## 升级你的 Shader

如果你的项目使用了 **Standard Shader**（标准着色器）或 **Built-in 渲染管线的自定义 Shader**，  
你需要将它们**转换为 URP 兼容的 Shader**。  
请参考 [升级你的 Shader](upgrading-your-shaders.md) 进行转换。


## 从 Built-in 渲染管线升级到 URP

如果你从 **Built-in 渲染管线（BiRP）** 升级到 **URP**，会涉及大量更改，  
你需要执行 额外的配置和转换，不仅仅是安装 URP。

以下页面提供了详细的升级指南和后续步骤：

- [转换你的 Shader](upgrading-your-shaders.md)
- [使用 Render Pipeline Converter 进行转换](features/rp-converter.md)**
- [升级自定义 Shader 以兼容 URP](urp-shaders/birp-urp-custom-shader-upgrade-guide.md)
- [URP 质量设置的位置](birp-onboarding/quality-settings-location.md)
- [更新 URP 质量预设](birp-onboarding/quality-presets.md)