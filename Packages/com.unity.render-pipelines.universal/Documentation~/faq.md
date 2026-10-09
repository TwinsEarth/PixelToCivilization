# 常见问题（FAQ）

本部分回答了关于 通用渲染管线（URP） 的常见问题。  
这些问题来自 [Unity 论坛](https://forum.unity.com/forums/general-graphics.76/)、[Unity Discord 频道](https://discord.gg/unity) 以及官方支持团队。

如果你需要了解 高画质渲染管线（HDRP），请参考 [HDRP 文档](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html)。



## URP 和 HDRP 可以同时使用吗？

不可以。  
虽然它们都基于 Scriptable Render Pipeline（SRP），但它们的渲染路径和光照模型不同，无法共存。



## 我可以在不同的渲染管线之间转换吗？

- 可以从 Built-in 渲染管线转换到 URP，但你需要重写资源并重新配置光照。  
  参考 [在现有项目中安装 URP](InstallURPIntoAProject.md) 进行转换。
- 你可以使用 Shader 升级工具 [将 Built-in Shaders 升级为 URP Shaders](upgrading-your-shaders.md)。  
  自定义 Shader 需要手动升级。
- 不支持 在运行时切换渲染管线资产（Pipeline Asset）。
- 不提供 URP 与 HDRP 之间的升级工具。



## 如何更新 URP 资源包？

建议通过 Package Manager 更新 URP：
1. 在 Unity 编辑器 中，进入 Window > Package Manager。
2. 找到 Universal RP 包，选择 更新。

如果你手动从 GitHub 下载了 SRP 代码或 Shader Graph，请确保它们的版本与 URP 版本匹配。



## 动态批处理（Dynamic Batching）在哪里？

动态批处理 选项已从 Player Settings 移动到了 [URP 资源](universalrp-asset.md) 选项中。



## 如何在编辑器中启用 双面全局光照（Double Sided Global Illumination）？

在 材质 Inspector 中：
1. 找到 Render Face 选项。
2. 选择 Both（双面渲染）。

这使得几何体的正反面都能贡献全局光照（URP 不剔除任何面）。



## URP 适用于桌面应用和游戏吗？

是的。  
URP 的图形质量和性能是可扩展的，可以用于 PC、主机和移动设备。



## URP 目前不支持 Built-in 渲染管线的某个功能，以后会支持吗？

查看 [URP 和 Built-in 渲染管线的功能对比表](universalrp-builtin-feature-comparison.md)，  
URP 不会支持 被标记为 "Not Supported"（不支持） 的功能。



## URP 有公开的开发路线图吗？

有，你可以 [在这里查看](https://portal.productboard.com/8ufdwj59ehtmsvxenjumxo82/tabs/3-Universal-render-pipeline)。  
你还可以提交建议（需要输入 电子邮件，但不需要注册账号）。



## 我发现了一个 Bug，该如何报告？

可以通过 [Bug 报告系统](https://unity3d.com/unity/qa/bug-reporting) 提交问题。  
URP Bug 处理流程与 Unity 其他 Bug 相同，你还可以在 [问题追踪器](https://issuetracker.unity3d.com/product/unity/issues?utf8=%E2%9C%93&package=2&unity_version=&status=1&category=&view=hottest) 查看当前已知的 URP Bug。



## 我已将项目从 Built-in 渲染管线升级到 URP，但性能没有提升，为什么？

URP 和 Built-in 渲染管线的默认质量设置不同。  
Built-in 渲染管线 的设置分布在 Quality Settings、Graphics Settings 和 Player Settings，而 URP 的所有设置都存储在 URP 资源（URP Asset） 中。

### 检查以下设置：
- 确保 URP 资源 的设置与你的 Built-in 渲染管线项目 设置匹配。
- 例如：如果你在 Built-in 渲染管线 项目中禁用了 MSAA 或 HDR，请在 URP 资源 中同样禁用它们。

如果在 匹配设置后，性能仍然较差，请 [提交 Bug 报告](https://unity3d.com/unity/qa/bug-reporting) 并附上你的项目。



## URP 不能在某个设备或平台上运行，是否正常？

不正常。  
请 [提交 Bug 报告](https://unity3d.com/unity/qa/bug-reporting)。



## 项目构建时间太长，如何优化？

Unity 正在优化 Shader 关键字剥离（Shader Stripping） 以减少构建时间。  
你可以手动禁用不需要的功能，以减少 Shader 变体数量。

详细优化方法请参考 [Shader Stripping 文档](shader-stripping.md)。



## 在 URP 中如何设置相机清除标志（Clear Flags）？

可以在 Camera Inspector 中设置 Background Type 来控制相机的颜色缓冲区初始化方式。



## URP 使用的渲染颜色空间是什么？

URP 默认使用 线性颜色空间（Linear Color Space） 进行渲染。  
你也可以在 Player Settings 中切换为 Gamma 颜色空间（非线性）。
