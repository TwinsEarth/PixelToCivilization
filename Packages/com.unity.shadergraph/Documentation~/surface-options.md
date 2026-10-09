在不更改图表的情况下修改 Surface 选项
==================================================

[](#description)描述
---------------------------

启用 **Allow Material Override** 可以在材质检查器中修改一组特定的属性，适用于通用渲染管线（URP）光照和非光照 Shader Graph，以及内置渲染管线的 Shader Graph。

<table class="table table-bordered table-striped table-condensed"><tbody><tr><td><b>属性</b></td><td><b>URP 光照</b></td><td><b>URP 非光照</b></td><td><b>内置渲染管线</b></td></tr><tr><td><b>Workflow Mode</b></td><td rowspan="2">详见 URP 的  <a href="https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest/manual/lit-shader.html" target="com.unity.render-pipelines.universal">Lit Shader</a> 文档。</td><td rowspan="2">不适用。</td><td rowspan="2">不适用。</td></tr><tr><td><b>Receive Shadows</b></td></tr><tr><td><b>Cast Shadows</b></td><td colspan="2">仅在此 Shader Graph 启用 <b>Allow Material Override</b> 时显示此属性。启用该属性可使使用此 Shader 的 GameObject 在自身和其他 GameObject 上投射阴影。详见 <a href="https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-SubShaderTags.html" target="unity_manual">ShaderLab: ForceNoShadowCasting</a> 子着色器标签。</td><td>不适用。</td></tr><tr><td><b>Surface Type</b></td><td colspan="2" rowspan="3">详见 URP 的 <a href="https://docs.unity3d.com/cn/Packages/com.unity.render-pipelines.universal@12.1/manual/lit-shader.html" target="com.unity.render-pipelines.universal">Lit</a> 和 <a href="https://docs.unity3d.com/cn/Packages/com.unity.render-pipelines.universal@12.1/manual/unlit-shader.html" target="com.unity.render-pipelines.universal">Unlit Shaders</a> 文档。</td><td>在内置渲染管线中，该功能与 URP 中的行为相同。请查阅 URP 文档。</td></tr><tr><td><b>Render Face</b></td><td>在内置渲染管道中，该功能与 URP 中的行为相同。请查阅 URP 文档。</td></tr><tr><td><b>Alpha Clipping</b></td><td>在内置渲染管道中，该功能与 URP 中的行为相同。请查阅 URP 文档。</td></tr><tr><td><b>Depth Write</b></td><td colspan="3">仅在启用 Allow Material Override 时显示此属性。<br>此属性用于设置 GPU 在使用此 Shader 渲染几何体时是否向<a href="https://en.wikipedia.org/wiki/Z-buffering" target="en.wikipedia.org">深度缓冲区</a>（depth buffer）写入像素。<br><br>选项：<ul><li><b>自动</b>（Default）：团结引擎对不透明材质写入深度缓冲区，而对透明材质不写入。</li><li><b>强制启用</b>：团结引擎始终写入深度缓冲区。</li><li><b>强制禁用</b>：团结引擎不写入深度缓冲区。</li></ul>此选项的功能对应于 <a href="https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-Reference.html" target="unity_manual">ShaderLab</a> 中的 <a href="https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-ZWrite.html" target="unity_manual">ZWrite</a> 命令。要在 <a href="https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Rendering.RenderStateBlock.html" target="unity_api">RenderStateBlock</a> 中覆盖此设置，请设置 <a href="https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Rendering.RenderStateBlock-depthState.html" target="unity_api">depthState</a> 属性。 

</td></tr><tr><td><b>Depth Test</b></td><td colspan="3">仅在启用 <b>Allow Material Override</b> 时显示此属性。<br>此属性用于设置深度测试条件，未通过深度测试的像素不会被绘制。如果选择 <b>LEqual</b> 以外的任何选项（此属性的默认设置），请考虑同时更改此材质的渲染顺序。
<br><br>选项：<ul><li><b>LEqual</b>（default）：绘制深度值小于或等于深度纹理值的像素。</li><li><b>Never</b>：U从不绘制此材质的像素。</li><li><b>Less</b>：绘制深度值小于当前深度缓冲区值的像素。</li><li><b>Greater</b>：绘制深度值大于当前深度缓冲区值的像素。</li><li><b>GEqual</b>：绘制深度值大于或等于当前深度缓冲区值的像素。</li><li><b>Equal</b>：绘制深度值等于当前深度缓冲区值的像素。</li><li><b>NotEqual</b>：绘制深度值不等于当前深度缓冲区值的像素。</li><li><b>Always</b>：无论z坐标为何都绘制此材质。</li></ul>此选项的功能对应于<a href="https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-Reference.html">ShaderLab</a>中的<a href="https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-ZTest.html"> ZTest</a> 命令。要在 <a href="https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Rendering.RenderStateBlock.html">RenderStateBlock</a> 中覆盖此设置，请设置 <a href="https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Rendering.RenderStateBlock-depthState.html">depthState</a> 属性。

</td></tr><tr><td><b>Support VFX Graph</b></td><td colspan="2">此属性仅在安装了 <a href="https://docs.unity.cn/cn/Packages-cn/com.unity.visualeffectgraph@latest/manual/">Visual Effect Graph</a> 包时可用。表示此Shader Graph是否支持视觉效果图表。启用此属性后，输出上下文可以使用此Shader Graph渲染粒子。当导入 Shader Graph 时，Shader Graph 会进行内部设置以支持视觉效果。这意味着如果启用此属性，但不在视觉效果中使用着色器图，则不会对性能产生影响。它只会影响 Shader Graph 的导入时间。
</td><td>不适用。</td></tr></tbody></table>

[](#how-to-use)使用方法
-------------------------

要使用材料覆盖（Material Override）功能 :

1. 创建一个新的 Shader Graph 图。
2. 保存该图。
3.  打开[图形检查器](Internal-Inspector.md)。
4.  将 **活动目标（Active Targets）** 设置为 **通用（Universal）** 或 **内置（Built In）**。
5.  在图形检查器的 **通用（Universal）** 或 **内置（Built In）** 部分中，启用 **允许材质覆盖（Allow Material Override）**。
6.  创建或选择一个使用该 Shader Graph 的材质或 GameObject。
7.  在材质检查器中，修改目标材质或 GameObject 的 **表面选项（Surface Options）**。

