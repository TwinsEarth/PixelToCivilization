# Scriptable Renderer Feature 介绍

Scriptable Renderer Feature 是可添加到渲染器中的组件，用于修改 URP 渲染项目的方式。

本指南涵盖 Scriptable Renderer Feature 的基本概念：

- [Scriptable Renderer Feature 介绍](#scriptable-renderer-feature-介绍)
  - [什么是 Scriptable Renderer Feature](#什么是-scriptable-renderer-feature)
  - [Scriptable Renderer Feature 与 Scriptable Render Pass 的区别](#scriptable-renderer-feature-与-scriptable-render-pass-的区别)
    - [何时使用 Scriptable Renderer Feature？](#何时使用-scriptable-renderer-feature)
    - [何时使用 Scriptable Render Pass？](#何时使用-scriptable-render-pass)
  - [其他资源](#其他资源)

Scriptable Render Pass 是 Scriptable Renderer Feature 的核心部分。有关详细信息，请参阅 [Scriptable Render Pass 基础](../intro-to-scriptable-render-passes.md)。

## <a name="scriptable-renderer-feature"></a>什么是 Scriptable Renderer Feature

Scriptable Renderer Feature 是一种可自定义的 [Renderer Feature](../../urp-renderer-feature.md)。它是一个可脚本化的组件，允许您向渲染器添加自定义渲染逻辑，以改变 Unity 渲染场景或场景中对象的方式。Scriptable Renderer Feature 通过管理和应用 Scriptable Render Pass 来创建自定义渲染效果。

Scriptable Renderer Feature 负责控制 Scriptable Render Pass 何时以及如何应用于特定的渲染器或相机，并且可以同时管理多个 Scriptable Render Pass。因此，当渲染效果需要多个 Render Pass 时，使用 Scriptable Renderer Feature 比单独插入多个 Scriptable Render Pass 更为便捷。

## <a name="renderer-feature-or-render-pass"></a>Scriptable Renderer Feature 与 Scriptable Render Pass 的区别

Scriptable Renderer Feature 和 Scriptable Render Pass 都可以实现类似的效果，但它们适用于不同的场景。两者的主要区别在于使用方式：Scriptable Renderer Feature 需要添加到渲染器中才能运行，而 Scriptable Render Pass 更灵活，但需要额外的工作才能应用到多个场景。

### 何时使用 Scriptable Renderer Feature？
如果您希望在多个相机、多个场景或整个项目中应用相同的渲染效果，Scriptable Renderer Feature 是更合适的选择。当您将其添加到渲染器时，所有使用该渲染器的对象都会应用此 Feature。这意味着您只需修改 Scriptable Renderer Feature 一次，所有相关的渲染效果都会自动更新。

### 何时使用 Scriptable Render Pass？
如果您只希望在特定场景或特定位置添加某个渲染效果，直接插入单独的 Scriptable Render Pass 会更加合适。这种方式避免了编写过于复杂的渲染器 Feature（如需要处理体积效果的 Feature），同时还能减少可能的性能影响。有关详细信息，请参阅 [场景中的 Scriptable Render Pass](../intro-to-scriptable-render-passes.md#scriptable-render-passes-in-scenes)。

## 其他资源

* [Scriptable Render Pass 介绍](../intro-to-scriptable-render-passes.md)
* [如何创建自定义 Renderer Feature](../create-custom-renderer-feature.md)
