# System Seed

菜单路径：**Operator > BuiltIn > System Seed**

**System Seed** 运算符输出内部视觉效果系统种子。Visual Effect Graph 使用此值来初始化每个组件的随机数生成器。系统种子通常是恒定的，但是，如果您在 Visual Effect 组件中启用了 **Reseed on Play**，则当新的播放事件触发时可以重新生成种子。

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **systemSeed** | uint | 当前系统种子。 |
