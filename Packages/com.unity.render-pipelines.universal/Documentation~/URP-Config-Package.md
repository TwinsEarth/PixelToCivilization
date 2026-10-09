# 使用URP Config包配置设置

您可以使用Universal Render Pipeline（URP）Config包来控制URP的一些设置。Unity会自动将包文件添加到包缓存中，因为它们是URP的依赖项，但您必须先将它们复制到项目中，才能使用该包。

URP Config包当前只更改一个设置，即当您使用Forward+渲染路径时，URP渲染的最大可见光源数量。有关更多信息，请参阅[更改最大可见光源数量](rendering/forward-plus-rendering-path.md#change-the-maximum-number-of-visible-lights)。

## 设置URP Config包

要在项目中创建URP Config包的可用副本，请执行以下操作：

1. 在**Project**窗口中，右键单击**Assets**并选择**Show in Explorer**（MacOS: **Reveal in Finder**）。
2. 转到`/Library/PackageCache/`。
3. 将`com.unity.render-pipelines.universal-config@[versionnumber]`文件夹复制到`Packages`文件夹。
4. 将复制的文件夹重命名为`com.unity.render-pipelines.universal-config`。

现在，URP Config包已准备好在项目中使用。

## 使用URP Config包配置URP

您可以编辑`ShaderConfig.cs`文件来配置URP项目的属性。如果您编辑此文件，还必须更新等效的`ShaderConfig.cs.hlsl`头文件，以便它与您在`ShaderConfig.cs`中设置的定义保持一致。

您可以通过两种方式更新`ShaderConfig.cs.hlsl`文件：

* 手动编辑`ShaderConfig.cs.hlsl`文件，使其与`ShaderConfig.cs`文件一致。这种方法较快，但更容易因错误而出问题。
* 使用编辑器从`ShaderConfig.cs`文件生成`ShaderConfig.cs.hlsl`文件，这可能比手动编辑更耗时，但可以确保两个文件保持同步。

要使用编辑器生成`ShaderConfig.cs.hlsl`文件，请按照以下步骤操作：

1. 在**Project**窗口中，转到**Packages** > **Universal RP Config** > **Runtime**并打开**ShaderConfig.cs**。
2. 编辑您要更改的属性值，然后保存并关闭文件。
3. 在编辑器中，选择**Edit** > **Rendering** > **Generate Shader Includes**。
4. Unity会自动配置您的项目和着色器，以使用新的配置。

### 更新URP Config包

当您使用包管理器更新URP包时，包管理器会将最新版本的URP Config包下载到`/Library/PackageCache/`文件夹，但不会自动更新`Packages`文件夹中的URP Config包文件。相反，您需要手动更新`Packages`文件夹中URP Config包的副本，并重新应用您的更改。为此，请按照以下步骤操作：

1. 从`Packages`文件夹中复制`com.unity.render-pipelines.universal-config`。您可以在稍后重新应用更改时引用它。
2. 删除`Packages`文件夹中的`com.unity.render-pipelines.universal-config`文件夹。
3. 如上所述，再次将`com.unity.render-pipelines.universal-config@[versionnumber]`文件夹从`/Library/PackageCache/`文件夹复制到`Packages`文件夹中。
4. 将复制的文件夹重命名为`com.unity.render-pipelines.universal-config`。
5. 手动将您的修改重新应用到更新后的URP Config包副本中。