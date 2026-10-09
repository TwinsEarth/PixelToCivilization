# URP 中的相机

通用渲染管道（URP）中的摄影机基于Unity的标准摄影机功能，但存在一些显著差异。例如，URP摄像机使用以下内容：

- 该[通用附加相机数据](../universal-additional-camera-data.md)组件扩展了相机组件的功能，并允许URP存储其他与相机相关的数据。
- 该[渲染类型](../camera-types-and-render-type.md)设置定义了URP中的两种相机类型：基础相机和叠加相机。
- 该[相机堆叠](../camera-stacking.md)系统允许你将多个相机的输出分层为单个组合输出。
- [Volume](../Volumes.md)系统，允许你根据场景中变换的位置应用[后期处理效果](../integration-with-post-processing.md)于摄影机。
- [相机组件](../camera-component-reference.md)，它在检查器中公开特定于URP的属性。

有关相机如何在Unity中工作的一般介绍以及常见相机工作流的示例，请参阅[Unity手册中关于相机的部分](https://docs.unity.cn/cn/tuanjiemanual/Manual/CamerasOverview.html)。
