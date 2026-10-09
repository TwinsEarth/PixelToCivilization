# Constant Rate

菜单路径：**Spawn > Constant Rate**

Constant Rate 代码块以恒定速率随时间增加生成计数。例如，如果速率为 10，则该代码块每秒为其 Spawn Context 触发 10 个生成事件。低于 1 的速率也是有效的，如果速率为 0.5，则速率为每两秒一次。

## 代码块兼容性

此代码块兼容于以下上下文：

- [Spawn](Context-Spawn.md)

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Rate** | float | 每秒生成速率。 |

## 备注

您可以使用以下等效的自定义生成器回调实现来模拟此代码块：

```C#

class ConstantRateEquivalent : VFXSpawnerCallbacks
{
    public class InputProperties
    {
        [Min(0), Tooltip("Sets the number of particles to spawn per second.")]
        public float Rate = 10;
    }

    static private readonly int rateID = Shader.PropertyToID("Rate");

    public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }

    public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
        if (state.playing)
        {
            float currentRate = vfxValues.GetFloat(rateID);
            state.spawnCount += currentRate * state.deltaTime;
        }
    }

    public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }
}
```
