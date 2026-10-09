Is Front Face 节点
=====

描述
---

如果当前渲染的是正面，则返回 true，如果渲染的是背面，则返回 false。除非在 Material Options 中将 Master Node 的 Two Sided 值设置为 true，否则此值始终为 true。这对于[分支](Branch-Node.md)（Branching）很有用。

注意：此 [节点](Node.md) 仅可用于 **片段** [着色器阶段](Shader-Stage.md)。

端口
---
|名称|方向|类型|绑定|描述|
|--|--|--|--|--|
|Out|输出|布尔值（Boolean）|无|输出值|