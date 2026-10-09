# 包示例

通用渲染管线（URP）提供了一系列示例，帮助你快速上手。

一个示例是一组可以导入到 Unity 项目中的资源，可用作基础来扩展项目，或用于学习特定功能的使用方式。一个包示例（Package Sample）可能包含单个 C# 脚本，也可能包含多个场景。

## 导入包示例

在导入 URP 的包示例之前，请确保你的项目已兼容 URP。一个项目若符合以下条件，即为 URP 兼容项目：

- 通过[URP 模板](creating-a-new-project-with-urp.md)创建
- 手动[安装并设置了 URP](InstallURPIntoAProject.md)

如果项目不兼容 URP，导入包示例时可能会出现错误。

要导入包示例，请使用 [Unity Package Manager 窗口](https://docs.unity.cn/cn/tuanjiemanual/Manual/upm-ui.html)：

1. 进入 **Window** > **Package Manager**，然后在[包列表视图](https://docs.unity.cn/cn/tuanjiemanual/Manual/upm-ui-list.html)中选择 **Universal RP**。
2. 在[包详情视图](https://docs.unity.cn/cn/tuanjiemanual/Manual/upm-ui-details.html)中，找到 **Samples** 部分。
3. 找到要导入的示例，点击旁边的 **Import** 按钮。

Unity 会将 URP 包示例导入到 `Assets/Samples/Universal RP/<包版本>/<示例名称>` 目录中。

## 打开包示例

要打开包示例，请执行以下步骤：

1. 进入 `Assets/Samples/Universal RP/<包版本>/` 目录。此处包含你导入的每个 URP 包示例的文件夹。
2. 找到包含所需包示例的文件夹并打开。文件夹名称与 **Unity Package Manager** 窗口中对应的包示例名称相同。

## 包示例列表

URP 提供的包示例如下：

- **URP Package Samples**：包含示例 Shader、C# 脚本和其他资源，可用于扩展项目或直接在应用程序中使用。详细信息请参考 [URP 包示例](package-sample-urp-package-samples.md)。
