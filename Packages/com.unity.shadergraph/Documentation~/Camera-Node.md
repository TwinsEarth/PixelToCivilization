
Camera 节点
=========


描述
--


允许访问当前用于渲染的**摄像机 (Camera)** 的各种参数。这包含**摄像机**游戏对象的值，例如 Position 和 Direction，以及各种投影参数。


#### 支持的渲染管线

* 通用渲染管线（URP）

高清渲染管线（HDRP）**不**支持此节点。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Position | 输出 | Vector 3 | 无 | 摄像机的游戏对象在世界空间中的位置 |
| 方向 | 输出 | Vector 3 | 无 | 摄像机的前向矢量方向 |
| Orthographic | 输出 | Float | 无 | 如果摄像机是正交摄像机，则返回 1，否则返回 0 |
| Near Plane | 输出 | Float | 无 | 摄像机的近平面距离 |
| Far Plane | 输出 | Float | 无 | 摄像机的远平面距离 |
| Z Buffer Sign | 输出 | Float | 无 | 使用反转的 Z 缓冲区时返回 -1，否则返回 1 |
| Width | 输出 | Float | 无 | 摄像机的宽度（如果是正交摄像机） |
| Height | 输出 | Float | 无 | 摄像机的高度（如果是正交摄像机） |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。

```
float3 _Camera_Position = _WorldSpaceCameraPos;
float3 _Camera_Direction = -1 * mul(UNITY_MATRIX_M, transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V)) [2].xyz);
float _Camera_Orthographic = unity_OrthoParams.w;
float _Camera_NearPlane = _ProjectionParams.y;
float _Camera_FarPlane = _ProjectionParams.z;
float _Camera_ZBufferSign = _ProjectionParams.x;
float _Camera_Width = unity_OrthoParams.x;
float _Camera_Height = unity_OrthoParams.y;

```

