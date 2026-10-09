# Visual Effect Graph 示例内容

Visual Effect Graph 附带了一组示例，可帮助您入门。

示例是一组资源，您可以将其导入到项目中，并用作构建或了解如何使用功能的基础。Visual Effect Graph 还包括一些有用的节点

要查找这些示例，请先安装 [install the Visual Effect Graph](GettingStarted.md)，然后：

1.转到 **Windows > Package Manager.**

2. 从 [Package列表视图](https://docs.unity3d.com/Manual/upm-ui-list.html)中, 选择 **Visual Effect Graph**。如果不存在：
   1. 从 [Packages 下拉菜单](https://docs.unity3d.com/Manual/upm-ui.html)中，选择 **Unity Registry** 或 **In Project**。
   2. 转到 **Edit** > **Project Settings** > **Package Manager**
   3. 在 **Advanced Settings** 下拉列表中，启用 **Show Dependencies**。Visual Effect Graph 现在应该显示在 Packages 列表视图中。

3. 在显示包详细信息的主窗口中，找到 **Samples** 部分。

4. 要将示例导入到项目中，请单击 **Import**。这将在项目中创建一个 **Samples** 文件夹，并将您选择的样本导入到其中。这也是 Unity 将任何未来样本导入到其中的位置。


## 输出事件处理程序

此示例包括帮助程序 MonoBehaviour 脚本，您可以将其附加到具有 [VisualEffect](VisualEffectComponent.md) 组件的游戏对象。这些脚本侦听给定名称的 Output Events，并通过执行各种操作来做出反应。有些脚本支持 Editor 中的预览，有些则不支持。对于执行此操作的用户，Inspector 包含一个 **Execute in Editor** 切换选项。否则，请进入 Play Mode 以查看行为。

此示例包含的帮助程序脚本包括：

- **VFXOutputEventCMCameraShake**: ：当它收到具有您指定名称的输出事件时，此帮助程序脚本会通过 [Cinemachine Impulse Sources](https://docs.unity3d.com/Packages/com.unity.cinemachine@latest?subfolder=/manual/CinemachineImpulseSourceOverview.html) 系统触发摄像机抖动。
- **VFXOutputEventPlayAudio**: 当它收到具有您指定名称的 Output Event 时，此帮助程序脚本会播放来自 AudioSource 的声音。
- **VFXOutputEventPrefabSpawn**: 当它收到具有您指定名称的 Output Event 时，此帮助程序脚本会从预制件池中生成一个不可见的预制件。它会在给定的位置和旋转处生成它们。它还根据 Event 的 [lifetime属性](Reference-Attributes.md)管理预制件的生命周期。生成预制件时，您可以使用 **VFXOutputEventPrefabAttributeHandler** 脚本来配置预制件的子元素。有关更多信息，请参阅 [Using VFXOutputEventPrefabSpawn ](#using-vfxoutputeventprefabspawn) 应用一个力。
- **VFXOutputEventRigidBody**: 当它收到具有您指定名称的 Output Event 时，此帮助程序脚本会向 [RigidBody](https://docs.unity3d.com/ScriptReference/Rigidbody.html).
- **VFXOutputEventRigidBody**: 当它收到具有您指定名称的 Output Event 时，此帮助程序脚本会触发 [UnityEvent](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html)。

### 使用 VFXOutputEventPrefabSpawn

**VFXOutputEventPrefabSpawn** MonoBehaviour 组件从池中生成预制件。当它实例化这些预制件时，它会使它们不可见。启用该组件时，该组件将禁用  ([SetActive(false)](https://docs.unity3d.com/ScriptReference/GameObject.SetActive.html)) 每个预制件。最后，当您禁用该组件时，该组件会销毁每个预制件实例。销毁组件附加到的游戏对象时，也会发生这种情况。

当此组件收到具有您指定名称的 Output Event 时，它会查找免费（禁用）预制件，如果有可用：

1. 它将启用 Prefab。
2. 如果启用 **Use Position**，它将使用 [position属性](Reference-Attributes.md) 设置预制件的位置。
3. 如果启用 **Use Rotation**, 则会根据 [angle属性](Reference-Attributes.md) 设置预制件的旋转。
4. 如果启用 **Use Scale**, 则会从 [scale属性](Reference-Attributes.md) 设置预制件的缩放。
5. 如果启用 **Use Lifetime**，它将根据 [lifetime属性](Reference-Attributes.md) 启动具有延迟的协程，从而在延迟后禁用（释放）预制件。这使得它可以在将来的 OutputEvent 期间生成。
6. 它搜索预制实例的任何 `VFXOutputEventPrefabAttributeHandler` 脚本，并调用每个脚本以执行属性绑定。

`VFXOutputEventPrefabAttributeHandler` s脚本根据生成预制件的事件配置预制件的各个部分。此示例包含两个`VFXOutputEventPrefabAttributeHandler` 示例脚本:

- **VFXOutputEventPrefabAttributeHandler_Light**: 生成预制件时，这会根据 OutputEvent 的 [color属性](Reference-Attributes.md) 和脚本的 **Brightness Scale** 属性分别设置附加的 Light 组件的颜色和亮度。
- **VFXOutputEventPrefabAttributeHandler_RigidBodyVelocity**: 生成预制件时，这会根据 OutputEvent 的 [velocity 属性](Reference-Attributes.md) 设置附加的 RigidBody 的速度。

## Visual Effect Graph 新增功能

此示例包括可帮助您开始使用 Visual Effect Graph 的资源和示例graph。例如，此示例包括：

- 一组 flipbook 纹理。
- 演示各种 [节点](GraphLogicAndPhilosophy.md) 的示例Graph。
- 可在项目中使用的着色器和子图。
- 可用于项目中视觉效果的纹理集（根据 CC0 授权）。

此示例使用这些资源和示例来重现许多[内置粒子系统](https://docs.unity.cn/cn/tuanjiemanual/Manual/Built-inParticleSystem.html) 行为。例如，它提供了一个用于复制软粒子的辅助对象，以及一个用于允许您使用线性剪切或运动矢量对 Flipbook 进行采样的辅助对象。
