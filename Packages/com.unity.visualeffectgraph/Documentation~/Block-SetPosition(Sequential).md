# Set Position (Sequential)

菜单路径 : **Position > Set Position (Sequential : \<SequentialMode\>)**

**Set Position （Sequential）** Block 根据算术序列计算位置，并将结果存储在 **position** 属性中。（可选）它还可以根据序列中的偏移量索引计算位置，并将结果存储在 **targetPosition** 属性中。

有不同的模式可用于确定 sequence 使用哪个索引，是否写入位置和/或目标位置，以及 sequence 在达到其限制时如何换行。

此 Block 还计算采样位置的方向，并根据合成将其存储到 [direction 属性](Reference-Attributes.md)。此 Block 计算方向的方式根据序列类型而变化。可用的选择类型包括：

* **Line Sequencer**: 方向等于线路从起点到终点的方向。
![](Images/Block-SetPosition(Sequential)Line.gif)

* **Circle Sequencer**: 方向是计算位置处圆的法线。
![img](Images/Block-SetPosition(Sequential)Circle.gif)

* **Three Dimensional Sequencer**: 方向等于从原点到计算位置的归一化向量。
![img](Images/Block-SetPosition(Sequential)3D.gif)

## 块兼容性

此 Block 与以下上下文兼容：

- [Initialize](Context-Initialize.md)

## 块设置

| **设置**                     | **类型** | **描述**                                              |
| ------------------------------- | -------- | ------------------------------------------------------------ |
| **Composition Position**        | Enum     | **(检查器)** 指定此 Block 如何组合 position 属性。选项包括:<br/>&#8226; **Set**: 使用新值覆盖 position 属性。<br/>&#8226; **Add**: 将新值添加到 position 属性值。<br/>&#8226; **Multiply**: 将 position 属性值乘以新值。<br/>&#8226; **Blend**: 在 position 属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |
| **Composition Direction**       | Enum     | **(检查器)** 指定此 Block 如何组成 direction 属性。选项包括：<br/>&#8226; **Set**: 使用新值覆盖 direction 属性。<br/>&#8226; **Add**: 将新值添加到 direction 属性值。<br/>&#8226; **Multiply**: 将 direction 属性值乘以新值。<br/>&#8226; **Blend**:在 direction 属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |
| **Composition Target Position** | Enum     | **(检查器)** 指定此 Block 如何组合 targetPosition 属性。选项包括：<br/>&#8226; **Set**: 指定此 Block 如何组合 targetPosition 属性。选项包括：<br/>&#8226; **Add**: 将新值添加到 targetPosition 属性值。<br/>&#8226; **Multiply**: 将 targetPosition 属性值乘以新值。<br/>&#8226; **Blend**: 在 targetPosition 属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。<br/>仅当启用 **Write Target Position**  时，才会显示此设置。 |
| **Index**                       | Enum     | 用于对序列进行采样的索引。选项包括：<br/>&#8226; **ParticleID**: 使用 particleID 属性。<br/>&#8226; **Custom**: 使用您在 **Index** 属性中提供的自定义。 |
| **Write Position**              | Bool     | 切换序列是否写入 [position 属性](Reference-Attributes.md)。 |
| **Write Target Position**       | Bool     | 切换序列是否写入 [targetposition 属性](Reference-Attributes.md)。 |
| **Mode**                        | Enum     | 用于序列的 wrap 模式。选项包括：<br/>&#8226; **Clamp**: 索引大于序列最后一个元素的元素会重复序列的最后一个元素。<br/>&#8226; **Wrap**: 索引大于最后一个元素的元素从第一个元素重复。<br/>&#8226; **Mirror**: 索引大于最后一个元素的元素以相反的顺序重复，然后在达到零后恢复为正确的顺序。 |

## 块属性

| **Input**                 | **类型** | **描述**                                              |
| ------------------------- | -------- | ------------------------------------------------------------ |
| **Index**                 | int      | 确定自定义提供的索引以对序列进行采样。<br/>仅当将 **Index** 设置为 **Custom** 时，才会显示此属性。 |
| **Offset Index**          | int      | 将偏移量应用于采样索引，以确定序列中的位置。 |
| **Blend Position**        | Float    | 当前方向属性值与新计算的方向值之间的混合百分比。<br/>仅当将 **Composition Position** 设置为 **Blend**  时，才会显示此属性。 |
| **Blend Direction**       | Float    | 当前位置属性值与新计算的位置值之间的混合百分比。<br/>仅当将 **Composition Direction** 设置为 **Blend**  时，才会显示此属性。 |
| **Offset Target Index**   | int      | 将偏移量应用于采样索引，以确定序列中的 targetPosition。 |
| **Blend Target Position** | Float    | 当前 targetPosition 属性值与新计算的 targetPosition 值之间的混合百分比。<br/>仅当将 **Composition Target Position** 设置为 **Blend**  时，才会显示此属性。 |
