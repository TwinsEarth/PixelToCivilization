# 了解相机堆叠

在通用渲染管道（URP）中，可以使用摄影机堆叠对多个摄影机的输出进行分层，并创建单个组合输出。相机堆叠允许你创建效果，例如2D UI中的3D模型或车辆的驾驶舱。

相机堆栈由[基地摄像机](../camera-types-and-render-type.md#base-camera)和一个或多个[覆盖摄像机](../camera-types-and-render-type.md#overlay-camera)组成。相机堆栈使用相机堆栈中所有相机的组合输出覆盖基础相机的输出。因此，你可以对基础相机的输出执行任何操作，也可以对相机堆栈的输出执行任何操作。例如，可以将摄影机堆栈渲染到渲染目标，或应用后期处理效果。

有关详细信息，请参阅[设置相机堆栈](../camera-stacking.md)。要下载URP中的摄像机堆叠示例，请安装[相机堆叠样本](../package-sample-urp-package-samples.md#camera-stacking)。

## 相机堆叠和渲染顺序

URP在摄影机内执行多个优化，包括渲染顺序优化以减少过度绘制。但是，使用摄影机堆栈时，可以定义URP渲染摄影机的顺序。你必须小心，不要以导致过度透支的方式订购相机。有关URP中透支的详细信息，请参阅[渲染顺序优化](../cameras-advanced.md#rendering-order-optimizations)。

## 相机堆叠和后期处理

你应该只对堆栈中的最后一个相机应用后期处理，因此以下内容适用：

* URP仅渲染一次后期处理效果，而不是为每个摄影机重复渲染。
* 视觉效果是一致的，因为堆栈中的所有相机都接收相同的后期处理。

## 额外资源

* [设置相机堆栈](../camera-stacking.md)
* [相机组件参考](../camera-component-reference.md)
