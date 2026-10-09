# 理解相机渲染顺序

本页描述了在 **Universal Render Pipeline (URP)** 中，相机何时执行以下操作：

- [理解相机渲染顺序](#理解相机渲染顺序)
  - [清除颜色和深度缓冲区](#清除颜色和深度缓冲区)
    - [Base Camera](#base-camera)
      - [Color buffer](#color-buffer)
      - [Depth buffer](#depth-buffer)
    - [Overlay Camera](#overlay-camera)
      - [Color buffer](#color-buffer-1)
      - [Depth buffer](#depth-buffer-1)
  - [剔除和渲染](#剔除和渲染)
  - [渲染顺序优化](#渲染顺序优化)

## 清除颜色和深度缓冲区

在 **Universal Render Pipeline (URP)** 中，相机的清除行为取决于相机的 [Render Type](camera-types-and-render-type.md)。

### Base Camera

#### Color buffer

在渲染循环开始时，具有 [Base Render Type](camera-types-and-render-type.md) 的相机可以将其颜色缓冲区清除为天空盒，清除为纯色，或使用未初始化的颜色缓冲区。您可以通过在 **Render Type** 设置为 **Base** 时，在 [Camera Inspector](camera-component-reference.md) 中使用 __Background Type__ 属性来定义此行为。

请注意，未初始化的颜色缓冲区内容因平台而异。在某些平台上，未初始化的颜色缓冲区将包含来自上一帧的数据。在其他平台上，未初始化的颜色缓冲区将包含未初始化的内存。只有当您的相机绘制每个像素到颜色缓冲区，并且不希望进行不必要的清除操作时，才应选择使用未初始化的颜色缓冲区。

#### Depth buffer

Base Camera 在每个渲染循环开始时总是清除其深度缓冲区。

### Overlay Camera

#### Color buffer

在渲染循环开始时，[Overlay Camera](camera-types-and-render-type.md#overlay-camera) 会接收一个颜色缓冲区，该缓冲区包含来自相机堆栈中先前相机的颜色数据。它不会清除颜色缓冲区的内容。

#### Depth buffer

在渲染循环开始时，Overlay Camera 会接收一个深度缓冲区，该缓冲区包含来自相机堆栈中先前相机的深度数据。您可以通过在 [Camera Inspector](camera-component-reference.md) 中，当 **Render Type** 设置为 **Overlay** 时，使用 __Clear Depth__ 属性来定义此行为。

当 __Clear Depth__ 设置为 true 时，Overlay Camera 会清除深度缓冲区，并将其视图绘制到颜色缓冲区，覆盖任何现有的颜色数据。当 __Clear Depth__ 设置为 false 时，Overlay Camera 在将视图绘制到颜色缓冲区之前，会先在深度缓冲区进行测试。

## 剔除和渲染

如果您的 URP 场景包含多个相机，Unity 会以可预测的顺序执行它们的剔除和渲染操作。

每帧，Unity 执行以下操作：

1. Unity 获取场景中所有激活的 [Base Cameras](camera-types-and-render-type.md#base-camera) 列表。
2. Unity 将激活的 Base Cameras 分为两组：渲染到渲染纹理的相机和渲染到屏幕的相机。
3. Unity 将渲染到渲染纹理的 Base Cameras 按 **Priority** 顺序进行排序，以便具有较高 **Priority** 值的相机最后绘制。
4. 对于每个渲染到渲染纹理的 Base Camera，Unity 执行以下步骤：
    1. 剔除 Base Camera
    2. 将 Base Camera 渲染到渲染纹理
    3. 对于 Base Camera 的 [Camera Stack](camera-stacking.md) 中的每个 [Overlay Camera](camera-types-and-render-type.md#overlay-camera)，按照 Camera Stack 中定义的顺序：
        1. 剔除 Overlay Camera
        2. 将 Overlay Camera 渲染到渲染纹理
5. Unity 将渲染到屏幕的 Base Cameras 按 **Priority** 顺序进行排序，以便具有较高 **Priority** 值的相机最后绘制。
6. 对于每个渲染到屏幕的 Base Camera，Unity 执行以下步骤：
    1. 剔除 Base Camera
    2. 将 Base Camera 渲染到屏幕
    3. 对于 Base Camera 的 Camera Stack 中的每个 Overlay Camera，按照 Camera Stack 中定义的顺序：
        1. 剔除 Overlay Camera
        2. 将 Overlay Camera 渲染到屏幕

Unity 可以在一帧内多次渲染 Overlay Camera 的视图——要么是因为 Overlay Camera 出现在多个 Camera Stack 中，要么是因为 Overlay Camera 在同一个 Camera Stack 中出现多次。当这种情况发生时，Unity 不会重用任何剔除或渲染操作。操作会按照上面详细的顺序完全重复。

> [!NOTE]
> 在此版本的 URP 中，Overlay Cameras 和 Camera Stacking 仅在使用 Universal Renderer 时受支持。如果使用 2D Renderer，Overlay Cameras 将不会执行其渲染循环的任何部分。

## 渲染顺序优化

URP 在相机内执行多个优化操作，包括渲染顺序优化，以减少过度绘制。然而，当您使用 Camera Stack 时，您实际上是定义了这些相机的渲染顺序。因此，您必须小心不要按顺序排列这些相机，以避免造成过度绘制。

当 Camera Stack 中的多个相机渲染到相同的渲染目标时，Unity 会为 Camera Stack 中的每个相机绘制渲染目标中的每个像素。此外，如果多个 Base Camera 或 Camera Stack 渲染到渲染目标的同一区域，Unity 会根据每个 Base Camera 或 Camera Stack 的需求多次重新绘制重叠区域中的像素。

您可以使用 Unity 的 [Frame Debugger](https://docs.unity.cn/cn/tuanjiemanual/Manual/FrameDebugger.html)，或者使用特定平台的帧捕获和调试工具，来了解场景中哪里发生了过度绘制。然后，您可以相应地优化您的 Camera Stacks。
