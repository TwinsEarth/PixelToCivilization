# Sawtooth Wave

菜单路径： **Operator > Math > Wave > Sawtooth Wave**

 **Sawtooth Wave**操作员评估输入以生成从最小值到最大值线性增加的值。当输出值达到最大值时，它将重置为最小值。

 ![](Images/Operator-SawtoothWaveAnimation.gif)

如果将 **Frequency** 设置为 **1**，则当 **Input** 从 **0** 变为接近 **1** 时，输出值将从 **Min** 变为 **Max**。当 **Input** 达到 **1** 时，Output 值将返回 **Min** 并再次开始循环。

你可以使用此运算符在两个值之间逐渐移动，并在结束时重置该值。如果将设置**Min**为**0**和**Max**设置为**1**，则可以将其与[Lerp](Operator-Lerp.md)操作符一起使用，以在任意两个值（如位置或颜色）之间进行插值。

## 运算符属性

| **Input**     | **Type**                                | **描述**|
| ------------- | --------------------------------------- | ------------------------------------------------------------ |
| **Input**     | [Configurable](#operator-configuration) |此运算符计算以生成输出值的值。|
| **Frequency** | [Configurable](#operator-configuration) | **Input** 值在 **Min** 和 **Max** 之间移动的速率。较大的值会使波形重复得更多。|
| **Min**       | [Configurable](#operator-configuration) |Out 可以是的最小值。|
| **Max**       | [Configurable](#operator-configuration) |Out 可以是的最大值。|
| **Out**       | Matches **Input**                       |此运算符基于**投入**和**频率**在和**麦克斯**之间生成**分钟**的值。|

| **Output** | **Type**          | **描述**|
| ---------- | ----------------- | ------------------------------------------------------------ |
| **Out**    | Matches **Input** | 此 Operator 根据 **Input** 和 **Frequency** 在 **Min** 和 **Max** 之间生成的值。|

## 操作员配置

要查看操作员的配置，请单击**齿轮**操作员标题中的图标。

| **Property**  | **描述**|
| ------------- | ------------------------------------------------------------ |
| **Input**     | **Input**端口的值类型。有关此属性支持的类型的列表，请参见[可用类型](#available-types)。|
| **Frequency** | **Frequency**端口的值类型。有关此属性支持的类型的列表，请参见[可用类型](#available-types)。|
| **Min**       | **Min**端口的值类型。有关此属性支持的类型的列表，请参见[可用类型](#available-types)。|
| **Max**       | **Max**端口的值类型。有关此属性支持的类型的列表，请参见[可用类型](#available-types)。|



### 可用类型

你可以将以下类型用于**Input**、**Min**和端口**Max**：

-  **float**
-  **Vector**
-  **Vector2**
-  **Vector3**
-  **Vector4**
-  **Position**
-  **Direction**