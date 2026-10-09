# Single/Periodic Burst

菜单路径：

+   **Spawn > Single Burst**
+   **Spawn > Periodic Burst**

Single/Periodic Burst 代码块会即时一次性生成一系列粒子，或者通过延迟设置定期生成粒子。当此代码块触发生成一次粒子爆发时，它会立即递增 [spawnCount](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerState-spawnCount.html)。如果将 **Repeat** 设置更改为 **Periodic**，此代码块会将名称更改为 **Periodic Burst**，并在延迟时间过后生成粒子爆发。

## 代码块兼容性

此代码块兼容于以下上下文：

- [Spawn](Context-Spawn.md)

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Repeat** | Enum | **（检查器）** 此代码块使用的模式，用以确定是一次性触发，还是一天后重复触发粒子爆发。选项：  <br/>• **Single**：代码块生成单次粒子爆发。  <br/>• **Periodic**：代码块生成多次粒子爆发。生成一次粒子爆发后，它会等待您在 **Delay** 中指定的时间以后再次生成粒子爆发。 |
| **Spawn Mode** | Enum | 此代码块用于确定生成多少粒子的模式。选项：  <br/>• **Constant**：此代码块生成特定数量的粒子。您可以在 **Count** 属性中设置此数量。  <br/>• **Random**：此代码块在两个边界之间生成随机数量的粒子。如果选择此模式，则 **Count** 属性变为矢量 2，其中 x 轴代表一个边界值，y 轴代表另一个边界值。 |
| **Delay Mode** | Enum | 此代码块用于确定如何计算两次爆发之间所用延迟时间的模式。选项：  <br/>• **Constant**：延迟时间具有特定的持续时间。您可以在 **Delay** 属性中设置持续时间。  <br/>• **Random**：延迟时间是两个边界之间的随机持续时间。如果选择此模式，则 **Delay** 属性变为矢量 2，其中 x 轴代表一个边界值，y 轴代表另一个边界值。 |

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Count** | float/Vector2 | 要生成的粒子的数量。  <br/>如果将**生成模式**设置为 **Constant**，此属性是 float 类型，您设置的值是要生成的特定粒子数目。  <br/>如果将**生成模式**设置为 **Random**，此属性为矢量 2，其中 x 轴代表一个边界值，y 轴代表另一个边界值。当生成粒子爆发时，此代码块使用这两个值之间的随机数来确定生成多少粒子。 |
| **Delay** | float/Vector2 | 此代码块在生成粒子爆发之前等待的延迟持续时间。  <br/>如果将**延迟模式**设置为 **Constant**，此属性是一个 float 类型，您设置的值是此代码块在两次粒子生成之间等待的特定延迟持续时间。  <br/>如果将**延迟模式**设置为 **Random**，此属性是一个矢量 2，其中 x 轴代表一个边界值，y 轴代表另一个边界值。当此代码块启动延迟时，它使用这两个值之间的随机数来确定延迟持续时间。 |

## 备注

您可以使用以下等效的自定义生成器回调实现来模拟单次爆发行为：

```C#
class SingleBurstEquivalent : VFXSpawnerCallbacks
{
    public class InputProperties
    {
        public float Count = 1.0f;
        public float Delay = 2.0f;
    }

    static private readonly int countID = Shader.PropertyToID("Count");
    static private readonly int delayID = Shader.PropertyToID("Delay");

    public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }

    private float m_NextBurstTime;
    private bool m_Sleeping;

    public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
        if (state.newLoop)
        {
            m_NextBurstTime = vfxValues.GetFloat(delayID);
            m_Sleeping = false;
        }

        if (!m_Sleeping && state.playing && state.totalTime >= m_NextBurstTime)
        {
            state.spawnCount += vfxValues.GetFloat(countID);
            m_Sleeping = true;
        }
    }

    public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }
}
```

您可以使用以下等效的自定义生成器回调实现来模拟定期爆发行为：

```C#
class PeriodicBurstEquivalent : VFXSpawnerCallbacks
{
    public class InputProperties
    {
        public float Count = 1.0f;
        public float Delay = 2.0f;
    }

    static private readonly int countID = Shader.PropertyToID("Count");
    static private readonly int delayID = Shader.PropertyToID("Delay");

    public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }

    private float m_NextBurstTime;

    public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
        if (state.newLoop)
        {
            m_NextBurstTime = vfxValues.GetFloat(delayID);
        }

        if (state.playing && state.totalTime >= m_NextBurstTime)
        {
            m_NextBurstTime += vfxValues.GetFloat(delayID);
            state.spawnCount += vfxValues.GetFloat(countID);
        }
    }

    public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }
}
```
