# Visual Effect 组件 API

为了在场景中创建 [Visual Effect Graph](VisualEffectGraphAsset.md) 的实例，Unity 使用  [Visual Effect 组件](VisualEffectComponent.md)。Visual Effect 组件附加到场景中的游戏对象，并引用定义视觉效果的 Visual Effect Graph。这允许您在不同的位置和方向创建不同的效果实例，并独立控制每个效果。为了在运行时控制效果，Unity 提供了 C# API，可用于修改 Visual Effect 组件和设置 [Property](Properties.md)。

本文档介绍了常见使用案例，并介绍了使用 [组件 API](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VisualEffect.html)时要考虑的良好实践。

## 设置 Visual Effect Graph

要在运行时更改 [Visual Effect Graph](VisualEffectGraphAsset.md) ，请为`effect.visualEffectAsset`属性分配新的 Visual Effect Graph 资源。当您更改 Visual Effect Graph 时，该组件会重置其某些属性的值。

重置的值为：

* **Total Time**: 当您更改图表时，API 会调用将此值设置为 0.0f 的 `Reset()` 函数。
* **Event Attributes**: 该组件丢弃所有 事件[属性](Attributes.md).

**不重置**的值是：

* **Exposed Property Overrides**: 如果新的 Visual Effect Graph 资源公开的属性与上一个资源中的属性具有相同的名称和类型，则此属性的值不会重置。
* **Random Seed** 和 **Reset Seed On Play Value**
* **Default Event Override**
* **Rendering Settings overrides**

## 控制播放状态

你可以使用 API 来控制效果器播放。

### 常用控制

* **Play** : `effect.Play()` 或 `effect.Play(eventAttribute)` ，如果需要事件属性。
* **Stop** : `effect.Stop()` 或 `effect.Stop(eventAttribute)` ，如果需要事件属性。
* **Pause** : `effect.pause = true` 或  `effect.pause = false`* **Play Rate** : `effect.playRate = value`。Unity 不会序列化此更改。
* **Step** : `effect.AdvanceOneFrame()`。如果 `effect.pause` 被设置为 `true`时，它才会运行。
* **Reset Effect** : `effect.Reinit()` this also :
  * 重置 `TotalTime` 为 0.0f。
  * 重新发送 **Default Event** 到 Visual Effect Graph。
* **Play Rate** : `effect.playRate = value`。Unity 不会序列化此更改。

### 默认事件

启用 Visual Effect 组件（或它所附加到的游戏对象）时，它会向图形发送一个 [Event](Events.md)。默认情况下，此 Event 是 `OnPlay`， 也就是[Spawn Contexts](Contexts.md#spawn) 的标准开始。

您可以通过以下方式更改默认事件：

* 在 [Visual Effect Inspector](VisualEffectComponent.md)上，更改 **Initial Event Name** 字段。
* 在组件 API 中：  `initialEventName = "MyEventName";`
* 在组件 API 中： `initialEventID = Shader.PropertyToID("MyEventName");`
* 使用 [ExposedProperty Helper Class](ExposedPropertyHelper.md)

## 随机种子控制

每个 effect 实例都有其随机种子的设置和控件。您可以修改种子以影响 Visual Effect Graph 使用的随机值。

* `resetSeedOnPlay = true/false`: 每当您调用`ReInit()`函数时，统一是否都会计算新的随机种子。这会导致每个随机值视觉效果图所用的图与以前的模拟中的不同。
* `startSeed = intSeed`: 设置一个手动种子，**随机数**操作或用来创建随机值f或此视觉效果。如果`resetSeedOnPlay`设置为`true`，则unity会忽略此值。

<a name="PropertyInterface"></a>

## Property 接口

要访问 Exposed Properties 的状态和值，您可以在 [Visual Effect 组件](VisualEffectComponent.md)中使用多种方法。大多数 API 方法都允许通过以下方法访问属性：

* 一个 `string` 属性名称。这很容易使用，但却是优化程度最低的方法。
* 一个 `int` 属性 ID。使用 `Shader.PropertyToID(string name)`从字符串属性名称生成 ID。这是最优的方法。
* [ExposedProperty Helper Class](ExposedPropertyHelper.md)。这将字符串属性 name 提供的易用性与整数属性 ID 的效率相结合。

### 检查公开的属性

您可以检查组件的 Visual Effect Graph 是否包含特定的公开属性。为此，您可以使用以下组中与属性类型对应的方法：

* `HasInt(property)`
* `HasUInt(property)`
* `HasBool(property)`
* `HasFloat(property)`
* `HasVect或2(property)`
* `HasVect或3(property)`
* `HasVect或4(property)`
* `HasGradient(property)`
* `HasAnimationCurve(property)`
* `HasMesh(property)`
* `HasTexture(property)`
* `HasMatrix4x4(property)`

对于每种方法，如果 Visual Effect Graph包含带有相同名称或ID的正确类型的公开属性，则函数返回 `true`。否则函数返回 `false`。

### 获取公开属性的值

组件 API 允许您在组件的 Visual Effect Graph 中获取公开属性的值。为此，您可以使用以下组中与属性类型对应的方法：

* `GetInt(property)`
* `GetUInt(property)`
* `GetBool(property)`
* `GetFloat(property)`
* `GetVect或2(property)`
* `GetVect或3(property)`
* `GetVect或4(property)`
* `GetGradient(property)`
* `GetAnimationCurve(property)`
* `GetMesh(property)`
* `GetTexture(property)`
* `GetMatrix4x4(property)`

对于每个方法，如果 Visual Effect Graph 包含正确类型的公开属性，并且该属性与您传入的名称或 ID 相同，则该方法将返回该属性的值。否则，该方法将返回属性类型的默认值。

### 设置公开属性的值

组件 API 允许您在组件的 Visual Effect Graph 中设置公开属性的值。为此，您可以使用以下组中与属性类型对应的方法：

* `SetInt(property,value)`
* `SetUInt(property,value)`
* `SetBool(property,value)`
* `SetFloat(property,value)`
* `SetVect或2(property,value)`
* `SetVect或3(property,value)`
* `SetVect或4(property,value)`
* `SetGradient(property,value)`
* `SetAnimationCurve(property,value)`
* `SetMesh(property,value)`
* `SetTexture(property,value)`
* `SetMatrix4x4(property,value)`

每个方法都会用您传入的值覆盖相应属性的值。

### 重置属性覆盖和默认值

组件 API 允许您将属性覆盖重置回其原始值。为此，请使用 `ResetOverride(property)` 方法。

## 事件

### 发送事件

组件 API 允许您在运行时将[事件](Events.md)发送到组件的 Visual Effect Graph。为此，请使用以下任一方法：

* `SendEvent(eventNameOrId)`
* `SendEvent(eventNameOrId, eventAttribute)`

`eventNameOrId` 参数可以是以下类型之一：

* 一个 `string` 属性名称。这很容易使用，但却是优化程度最低的方法。
* 一个 `int` 属性 ID。使用 `Shader.PropertyToID(string name)`从字符串属性名称生成 ID。这是最优的方法。
* [ExposedProperty Helper Class](ExposedPropertyHelper.md)。这将字符串属性 name 提供的易用性与整数属性 ID 的效率相结合。

可选 `eventAttribute` 参数将 **Event Attribute Payload** 附加到事件中。它们有效负载提供 Graph 与 Event 一起处理的数据。

**注意**: 当你发送一个 [事件](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.SendEvent.html) (或使用 [`.Simulate`](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.Simulate.html)方法)中，Visual Effect 组件会处理下一个`VisualEffect.Update`中发生的所有推送命令，该命令发生在 [`LateUpdate`](https://docs.unity3d.com/Manual/Execution或der.html) 之后。

### Event Attributes 事件属性

事件[属性](Attributes.md) 是附加到事件的属性，可由 Visual Effect Graph 处理。要创建和存储事件属性，请使用 `VFXEventAttribute` 类。Visual Effect 组件负责创建 `VFXEventAttribute` 类的实例，并根据当前分配的 Visual Effect Graph 创建这些实例。

#### 创建 Event Attributes

要创建 `VFXEventAttribute`，请使用 Visual Effect 组件的 `CreateVFXEventAttribute()` 方法。如果要使用相同的属性多次发送同一事件，请存储`VFXEventAtrribute`，而不是在每次发送事件时都创建一个新事件。将事件发送到 Visual Effect Graph 时，Unity 会创建当前状态的 EventAttribute 副本并发送该副本。这意味着，在发送 Event 后，您可以安全地修改 EventAttribute，而不会影响发送到 Visual Effect Graph 的信息。

#### 设置 Attribute 的有效负载

创建事件属性后，您可以使用类似于 [Property 接口](#property-接口) 部分中描述的 Has/Get/Set 属性方法的 API 来设置属性负载。

* Has : `HasBool`, `HasVect或3`, `HasFloat`,... 检查 Attribute 是否存在。
* Get : `GetBool`, `GetVect或3`, `GetFloat`,... 获取 Attribute 的值。
* Set : `SetBool`, `SetVect或3`, `SetFloat`,... 设置 Attribute 的值。

有关完整的 Attribute API 文档，请参阅 Unity Script Reference 中的 [VFXEventAttribute](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXEventAttribute.html)。

属性名称或 ID 可以是以下类型之一：

* 一个 `string` 属性名称。这很容易使用，但却是优化程度最低的方法。
* 一个 `int` 属性 ID。使用 `Shader.PropertyToID(string name)`从字符串属性名称生成 ID。这是最优的方法。
* [ExposedProperty Helper Class](ExposedPropertyHelper.md)。这将字符串属性 name 提供的易用性与整数属性 ID 的效率相结合。

#### Life cycle and compatibility

创建 Event Attribute 时，它与当前分配给 Visual Effect 组件的 Visual Effect Graph 资源兼容。这意味着您可以使用相同的`VFXEventAttribute` 方法将 Event 发送到同一图表的其他实例。如果您将 Visual Effect 组件的 `visualEffectAsset` 属性更改为另一个图形，则不能再使用 `VFXEventAttribute` 向其发送事件。

如果要管理同一场景中的多个 Visual Effect 实例并希望共享 Event 负载，则可以存储一个 `VFXEventAttribute` 并在所有实例上使用它。

#### 示例（在 MonoBehaviour 中）

```c#
VisualEffect visualEffect;
VFXEventAttribute eventAttribute;

static readonly ExposedProperty positionAttribute = "position"
static readonly ExposedProperty enteredTriggerEvent = "EnteredTrigger"

void Start()
{
    visualEffect = GetComponent<VisualEffect>();
    // Caches an Event Attribute matching the
    // visualEffect.visualEffectAsset graph.
    eventAttribute = visualEffect.CreateVFXEventAttribute();
}

void OnTriggerEnter()
{
    // Sets some Attributes
    eventAttribute.SetVect或3(positionAttribute, player.transf或m.position);
    // Sends the Event
    visualEffect.SendEvent(enteredTriggerEvent, eventAttribute);
}
```

## 调试

每个 Visual Effect 组件都包含以下调试属性：

* `aliveParticleCount`: 整个效果中活动粒子的数量。<br/>**注意**: 组件每秒异步计算一次此值，这意味着结果可能是在您访问此属性之前一秒渲染的帧中活动粒子的数量。
* `culled`: 指示是否有任何摄像机在上一帧中剔除效果。
  