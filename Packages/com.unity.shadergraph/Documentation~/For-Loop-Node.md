For Loop 节点
===

**描述**  
---- 
该节点用于进行迭代计算，包括 **For Loop Start** 与 **For Loop End** 两个节点。

> 注意：该节点为组合节点，Start 与 End 节点需一起使用。

**端口**  
------  

### For Loop Start 节点

| 名称      | 方向  | 类型       | 绑定          | 描述                                |  
|-----------|-------|------------|---------------|-------------------------------------|  
| Iterations | 输入 | Int | 无 | 输入需要迭代的次数 |
| Loop Index | 输出 | Int | 无 | 输出当前迭代的计数 |
| History | 输出 | Vector4/Vector3/Vector2/Float | 无 | 输出上一次迭代的结果 |

### For Loop End 节点

| 名称      | 方向  | 类型       | 绑定          | 描述                                |  
|-----------|-------|------------|---------------|-------------------------------------|
| In | 输入 | Vector4/Vector3/Vector2/Float | 无 | 输入当前迭代的结果 |
| Continue | 输入 | Boolean | 无 | 输入是否需要跳过当前循环 |
| Break | 输入 | Boolean | 无 | 输入是否需要结束循环 |
| Out | 输出 | Vector4/Vector3/Vector2/Float | 无 | 最终输出结果 |

**控件**
---
**For Loop End** 节点可在 **Graph Inspector > Node Settings** 中进行以下设置：

| 名称 | 描述 |
| -- | -- |
| Output Slots Count | 调整需要对多少个数据进行迭代, 最多五个数据。 |
| Slot | 为每一个迭代的数据选择数据类型，默认为 Vector4， 可选择范围为 Vector4, Vector3, Vector2, Float。|