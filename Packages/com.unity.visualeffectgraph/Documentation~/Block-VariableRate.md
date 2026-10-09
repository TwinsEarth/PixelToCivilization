# Variable Rate

菜单路径：**Spawn > Variable Rate**

Variable Rate 代码块使用比 [Constant Rate 代码块](Block-ConstantRate.md) 更高级的方法。此代码块应用的生成速率是在周期所定义间隔内两个速率之间的线性插值。

## 代码块兼容性

此代码块兼容于以下上下文：

- [Spawn](Context-Spawn.md)

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Rate** | Vector2 | 此代码块用于计算生成率的两个值。当前速率始终是此 Vector2 的两个维度之间的值。 |
| **Period** | Vector2 | 此代码块计算新的生成率之前的最小和最大时间段。周期越长，产生的速率变化越平滑；周期越短，产生的速率变化频率越高。 |

## 备注

您可以使用以下等效的自定义生成器回调实现来模拟此代码块：

```C#
class VariableRateEquivalent : VFXSpawnerCallbacks
{
    public class InputProperties
    {
        public Vector2 Rate;
        public Vector2 Period;
    }

    static private readonly int rateID = Shader.PropertyToID("Rate");
    static private readonly int periodID = Shader.PropertyToID("Period");

    public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }

    float m_PrevRate;
    float m_NextRate;
    float m_PrevTime;
    float m_NextTime;

    void AdvanceRate(float totalTime, Vector2 rate, Vector2 period)
    {
        m_PrevRate = m_NextRate;
        m_PrevTime = totalTime;

        m_NextRate = Random.Range(rate.x, rate.y);
        m_NextTime = totalTime + Random.Range(period.x, period.y);
    }

    public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
        if (state.newLoop)
            AdvanceRate(state.totalTime, vfxValues.GetVector2(rateID), vfxValues.GetVector2(periodID));

        if (state.playing)
        {
            float range = m_NextTime - m_PrevTime;
            float ratio = 1.0f;
            if (range > 0.0f)
                ratio = Mathf.Clamp01((state.totalTime - m_PrevTime) / range);

            float rate = Mathf.Lerp(m_PrevRate, m_NextRate, ratio);
            if (ratio == 1.0f)
                AdvanceRate(state.totalTime, vfxValues.GetVector2(rateID), vfxValues.GetVector2(periodID));

            state.spawnCount += rate * state.deltaTime;
        }
    }

    public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
    }
}
```
