# 在相机堆叠中添加和移除相机

相机堆叠包含一个 Base Camera 和一个或多个 Overlay Cameras，后者叠加在前者之上。在编辑器中，您可以根据需要随时添加、移除和重新排序这些相机，以实现所需的效果。

本页面分为以下几个部分：

* [向相机堆叠中添加相机](#add-a-camera-to-a-camera-stack)
* [从相机堆叠中移除相机](#remove-a-camera-from-a-camera-stack)
* [重新排序相机堆叠中的相机](#reorder-cameras-in-a-camera-stack)

## 向相机堆叠中添加相机

要向相机堆叠中添加相机，请按照以下步骤操作：

1. 在场景中选择一个 **Render Type** 设置为 **Base** 的相机，将其设置为 Base Camera。如果场景中没有 Base Camera，请创建一个。
2. 在场景中创建另一个相机，并选择它。
3. 在相机 Inspector 窗口中，将 **Render Type** 设置为 **Overlay**。
4. 再次选择 Base Camera。在相机 Inspector 窗口中，找到 **Stack** 部分，选择 **Add** （**+**），然后选择 Overlay Camera 的名称。

此时，Overlay Camera 已成为 Base Camera 相机堆叠的一部分。Unity 将在 Base Camera 的输出之上渲染 Overlay Camera 的输出。

> [!NOTE]
> 当为相机堆叠创建多个相机时，请考虑这些相机是否都必要。每添加一个相机都会使渲染变得更慢，因为即使相机不渲染任何内容，活跃的相机也会经历整个渲染循环。

<a name="add-a-camera-with-a-script"></a>

### 使用 C# 脚本向相机堆叠中添加相机

您还可以使用 C# 脚本向相机堆叠中添加相机。使用 Base Camera 的 [Universal Additional Camera Data](xref:UnityEngine.Rendering.Universal.UniversalAdditionalCameraData) 组件的 `cameraStack` 属性，如下所示：

```c#
var cameraData = camera.GetUniversalAdditionalCameraData();
cameraData.cameraStack.Add(myOverlayCamera);
```

## 从相机堆叠中移除相机

要从相机堆叠中移除相机，请按照以下步骤操作：

1. 创建一个包含至少一个 Overlay Camera 的相机堆叠。有关说明，请参阅 [向相机堆叠中添加相机](#add-a-camera-to-a-camera-stack)。
2. 选择相机堆叠的 Base Camera。
3. 在相机 Inspector 窗口中，找到 **Stack** 部分，选择要移除的 Overlay Camera 的名称，然后选择 **Remove** （**-**）。

Overlay Camera 仍然存在于场景中，但不再是相机堆叠的一部分。

<a name="remove-a-camera-with-a-script"></a>

### 使用 C# 脚本从相机堆叠中移除相机

您还可以使用 C# 脚本从相机堆叠中移除相机。使用 Base Camera 的 [Universal Additional Camera Data](xref:UnityEngine.Rendering.Universal.UniversalAdditionalCameraData) 组件的 `cameraStack` 属性，如下所示：

```c#
var cameraData = camera.GetUniversalAdditionalCameraData();
cameraData.cameraStack.Remove(myOverlayCamera);
```

## 重新排序相机堆叠中的相机

要重新排序相机堆叠中的相机，请按照以下步骤操作：

1. 创建一个包含多个 Overlay Camera 的相机堆叠。有关说明，请参阅 [向相机堆叠中添加相机](#add-a-camera-to-a-camera-stack)。
2. 选择相机堆叠中的 Base Camera。
3. 在 Camera Inspector 中，找到 **Stack** 部分。
4. 使用位于 Overlay Camera 名称旁边的控件来重新排序 Overlay Camera 的列表。

Base Camera 渲染相机堆叠的基本层，堆叠中的 Overlay Camera 会按列表中的顺序，从上到下，叠加在 Base Camera 渲染的输出之上。

<a name="reorder-a-camera-stack-with-a-script"></a>

### 使用 C# 脚本重新排序相机堆叠

您还可以使用 C# 脚本重新排序相机堆叠。使用 Base Camera 的 [Universal Additional Camera Data](xref:UnityEngine.Rendering.Universal.UniversalAdditionalCameraData) 组件的 `cameraStack` 属性。`cameraStack` 是一个 `List`，可以像其他 `List` 一样进行重新排序。
