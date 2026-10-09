# Total Time

菜单路径：**Operator > BuiltIn > Total Time**

**Total Time** 运算符输出自效果开始以来的总时间。这是每一帧 [deltaTime](Operator-DeltaTime.md) 的累积，这意味着它会考虑到 [timeScale](https://docs.unity3d.com/ScriptReference/Time-timeScale.html) 和 [playRate](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect-playRate.html)。即使渲染器被剔除，此值也会递增。

当 [VisualEffect](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.html) 组件复位时，此值重置为 0。如果您调用 [VisualEffect.Reinit](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.Reinit.html) 或禁用然后启用游戏对象，则会发生这种情况。

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **totalTime** | float | 效果运行的总时间。 |
