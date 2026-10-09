<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>实验性:</b> 此功能目前是试验性的，在以后的主要版本中可能会发生变化。要使用此功能，请在工程 Preferences 的  <b>Visual Effects</b> 选项卡中启用  <b>Experimental Operators/Blocks</b>。</div>

# 公开属性类

`ExposedProperty` 类是一个帮助程序类，它根据属性的名称存储属性 ID。分配给 `ExposedProperty` 的值是 Shader 属性的字符串名称。该类自动调用 `shader.propertytoid` 函数用Shader属性名称为参数，并存储函数返回的整数ID。当您在[组件 API](ComponentAPI.md) 的 Property、Event 或 EventAttribute 方法中使用此类时，它会隐式使用此整数。

当您想要访问 Shader 属性时，可以使用该属性的名称或其 ID。使用属性的名称通常更容易，但使用属性的整数 ID 效率更高。此类非常有用，因为它结合了使用属性名称的便利性和使用属性 ID 的效率。


## 代码示例

```C#
ExposedProperty m_MyProperty;
VisualEffect m_VFX;

void Start()
{
    m_VFX = GetComponent<VisualEffect>();
    m_MyProperty = "My Property"; // Assign a string.
}

void Update()
{
    m_VFX.SetFloat(m_MyProperty, someValue); // Uses the property ID.
}
```
