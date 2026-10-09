# Age Over Lifetime

菜单路径：**Operator > Attribute > Age over Lifetime**

**Age Over Lifetime** 运算符返回粒子相对于其生命周期的年龄比值，值介于 0.0 和 1.0 之间。

```
t = age / lifetime

```

运算符属性
-----

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **t** | float | 粒子相对于其生命周期的年龄比值。 |

Details
-------

如果您使用此运算符的系统不包含年龄或生命周期，则 Unity 将使用默认属性值。

*   年龄默认为 **0**。
*   生命周期默认为 **1**。
