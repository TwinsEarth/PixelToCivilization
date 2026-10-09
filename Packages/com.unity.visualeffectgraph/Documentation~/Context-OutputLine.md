# Output Particle Line

菜单路径 : **Context > Output Particle Line**

**Output Particle Line** 上下文使用线条来渲染粒子系统。线条由两个端点定义，并且无论粒子距离摄像机多远或粒子的大小和比例属性如何，始终为单个像素宽度。

有两种模式可用于设置线的端点。第一个点总是在粒子位置：

*   在粒子空间中使用目标偏移。通过在粒子空间中定义的偏移指定第二个点。
*   使用目标位置属性。使用目标位置属性指定第二个点。

此输出不支持纹理或  [Shader Graph](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest)。

以下是特定于 Output Particle Line 上下文的设置和属性列表。有关此上下文与所有其他上下文共享的通用输出设置的信息，请参阅 [全局输出设置和属性](Context-OutputSharedSettings.md)。


## 上下文设置

| 设置 | 类型 | 描述 |
| ------- | ---- | ----------- |
|**Use Target Offset**|bool|指示此上下文是否将线的端点派生为粒子空间中粒子位置的偏移量。如果禁用此属性，则上下文使用粒子的目标位置属性作为端点。|

## 上下文属性

| 输入 | 类型 | 描述 |
| ----- | ---- | ----------- |
|**Target Offset**|Vector3|此上下文应用于粒子位置以派生第二个点的粒子空间内偏移量。<br/>此属性仅在启用 Use Target Offset 时可用。|
