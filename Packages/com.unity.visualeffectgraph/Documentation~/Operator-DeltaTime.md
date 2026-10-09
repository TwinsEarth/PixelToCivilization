# Delta Time

菜单路径：**Operator > BuiltIn > DeltaTime**

**Delta Time** 运算符输出按 [VisualEffect.playRate](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VisualEffect-playRate.html) 缩放的当前帧和前一帧之间的时间（以秒为单位）。此运算符输出的值不能大于为 [VFXManager.maxDeltaTime](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-maxDeltaTime.html) 设置的值。

在 Visual Effect Graph 资源中，如果您将 **Update Mode** 设置为 **Fixed Delta Time**，则此运算符按 VisualEffect.playRate 缩放的基值是 [VFXManager.fixedTimeStep](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-fixedTimeStep.html) 的倍数。该倍数介于 0 和最大迭代次数之间，Unity 从 [VFXManager.maxDeltaTime](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-maxDeltaTime.html) 和 [VFXManager.fixedTimeStep](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-maxDeltaTime.html) 计算得出。

总的来说，Unity 生成的输出值如下：

*deltaTime = max(VisualEffectAsset.fixedDeltaTime ? n \* VFXManager.fixedTimeStep : [Time.deltaTime](https://docs.unity3d.com/ScriptReference/Time-deltaTime.html), [VFXManager.maxDeltaTime](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-maxDeltaTime.html)) \* [VisualEffect.playRate](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VisualEffect-playRate.html);*

其中 **n** 是一个正整数（包括零）。

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **deltaTime** | Float | 视觉效果的 deltaTime 不大于 [VFXManager.maxDeltaTime](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-maxDeltaTime.html)。如果 Visual Effect Graph 资源的 **Update Mode** 设置为 **Fixed Delta Time**，则此值是 [VFXManager.fixedTimeStep](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-fixedTimeStep.html) 的倍数。 |
