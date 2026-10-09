# 输出事件处理程序

VFXOutputEventAbstractHandler 是一个 API 帮助程序，它与 [Output Event](Contexts.md#output-events) 挂钩，允许您根据事件执行脚本。

Visual Effect Graph 包含一组示例脚本作为示例。有关如何安装示例的信息，请参阅安装示例脚本。

## 输出事件处理程序 API

要创建自己的输出事件处理程序，请编写扩展 `UnityEngine.VFX.Utility.VFXOutputEventAbstractHandler` 类的脚本。

当你编写一个扩展这个类的 MonoBehaviour 时，它会减少执行一个钩子所需的代码量。这是因为 base class 执行筛选事件的工作并调用以下方法：

`override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)`

实现此方法时，Unity 会在事件每次触发并传入事件的属性时调用该方法。

### 例子

以下示例在接收到事件时将 Game Object 传送到 Given 位置。

```c#
[RequireComponent(typeof(VisualEffect))]
public class VFXOutputEventTeleportObject : VFXOutputEventAbstractHandler
{
    public Transform target;

    static readonly int kPosition = Shader.PropertyToID("position");

    public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
    {
        if(target != null)
            target.position = eventAttribute.GetVector3(kPosition);
    }
}
```


## 使用输出事件帮助程序

为了帮助您创建自己的 VFXOutputEventAbstractHandler，Visual Effect Graph 包包含一组示例脚本，您可以通过 Package Manager 安装这些脚本。有关如何使用这些示例脚本的信息，请参阅[示例内容](sample-content.md)。
