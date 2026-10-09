
Emission 节点
====

描述
---

Emission 节点允许在 Shader Graph 中应用 emission。


渲染管线兼容性
-------


| **节点** | **通用渲染管线 (URP)** | **高清渲染管线 (HDRP)** |
| --- | --- | --- |
| Emission | 否 | 是 |


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| **color** | 输入 | LDR Color(RGB) | 设置发射的低动态范围 (LDR) 颜色。 |
| **intensity** | 输入 | Float | 设置发射颜色的强度。 |
| **output** | 输出 | HDR Color(RGB) | 输出此节点产生的高动态范围 (HDR) 颜色。 |


注意
--


### 发射单位


可以使用两个[物理光单位](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Physical-Light-Units.html)来控制发射的强度：


* [Nits](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Physical-Light-Units.html)。
* [$EV_{100}$](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Physical-Light-Units.html)。


### 曝光权重

可以使用曝光权重 (Exposure Weight) 来确定曝光对发射的影响。此值介于 **0** 到 **1** 之间。值为 **0** 表示曝光不影响发射的此部分。值为 **1** 表示曝光完全影响发射的此部分。
