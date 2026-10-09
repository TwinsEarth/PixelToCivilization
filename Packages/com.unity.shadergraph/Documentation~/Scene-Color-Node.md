
Scene Color 节点
==============


描述
--


允许使用输入 **UV**（应该是标准化的屏幕坐标）访问当前**摄像机**的颜色缓冲区。


注意：此[节点](Node.md)的行为未在全局范围内定义。此节点执行的 HLSL 代码是根据**渲染管线**定义的，不同的**渲染管线**可能会产生不同的结果。希望支持此节点的自定义**渲染管线**也需要显式定义其行为。如果未定义，此节点将返回 0（黑色）。


注意：在 **通用渲染管线（URP）** 中，此节点返回 **Camera Opaque Texture** 的值。请参阅 [通用渲染管线](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest) 以了解此功能的更多文档信息。此纹理的内容仅适用于**透明**对象。将 [Graph Inspector](Internal-Inspector.md) 的 [Graph Settings 选项卡](Graph-Settings-Tab.md)上的 **Surface Type** 下拉选单设置为 **Transparent** 可以从此节点接收正确的值。


>注意：此节点只能在**片元**[着色器阶段](Shader-Stage.md)中使用。


#### 支持的渲染管线

下表列出了哪些渲染管线支持 Scene Color 节点。当与不支持的渲染管线一起使用时，场景颜色节点会返回 0（黑色）。

| 管线 | 是否支持|
|--|--|
|内置渲染管线|No|
|通用渲染管线（URP）|Yes|
|高清渲染管线（HDRP）|Yes|


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 4 | 屏幕位置（Screen Position） | 标准化的屏幕坐标 |
| Out | 输出 | Vector 3 | 无 | 输出值 |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。



```
void Unity_SceneColor_float(float4 UV, out float3 Out)
{
    Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV);
}

```

