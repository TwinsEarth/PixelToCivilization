# 快速上手

使用 **通用渲染管线（URP）** 你可以选择**新建项目**或**升级现有项目**。  
以下是两种方式：

- [从模板创建新的 URP 项目](creating-a-new-project-with-urp.md)  
  如果你是从零开始创建 URP 项目，这是最佳选择。  
  Unity 会自动安装并配置 URP，无需手动设置。

- [在现有 Unity 项目中安装 URP](InstallURPIntoAProject.md)  
  如果你的项目基于 Built-in 渲染管线，可以安装 URP 并手动配置项目。  
  你需要手动转换或重建部分资源（如 光照 Shader 或 后处理效果）以兼容 URP。


> **注意：**  
> URP 目前不支持自定义后处理效果。  
> 如果你的项目使用了自定义后处理，暂时无法在 URP 中重新创建。未来版本将提供支持。

> **注意：**  
> URP 项目 不兼容 高画质渲染管线（HDRP） 或 Built-in 渲染管线。  
> 在开发之前，你必须决定使用哪种渲染管线。  
> 有关选择渲染管线的信息，请参考 [Unity 手册的渲染管线部分](https://docs.unity.cn/cn/tuanjiemanual/Manual/render-pipelines.html)。
