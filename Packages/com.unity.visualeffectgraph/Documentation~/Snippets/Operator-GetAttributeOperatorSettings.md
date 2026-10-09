# 运算符设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Location** | [Enum](Attributes.md#attribute-locations) | 属性的位置。选项：  <br/>• **Current**：从当前系统数据容器获取属性值。例如，来自粒子系统的粒子数据。  <br/>• **Source**：从读取的之前的系统数据容器中获取属性值。在系统数据更改后，您只能从系统的第一个上下文中的此 **Location** 读取数据。例如，在 Initialize Particle 上下文中。 |
