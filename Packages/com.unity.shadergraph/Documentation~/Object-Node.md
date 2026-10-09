
Object 节点
=========


描述
--


允许访问当前渲染**对象**的各种参数。


注意：可以根据渲染管线定义位置[端口](Port)的行为。不同的渲染管线可能会产生不同的结果。如果要在一个渲染管线中构建希望在两个渲染管线中使用的着色器，请在实际应用之前尝试在这两个管线中对其进行检查。


#### 支持的渲染管线

* 通用渲染管线（URP）
* 高清渲染管线（HDRP）


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Position | 输出 | Vector 3 | 无 | 对象在世界空间中的位置 |
| Scale | 输出 | Vector 3 | 无 | 对象在世界空间中的缩放 |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。

```
float3 _Object_Position = SHADERGRAPH_OBJECT_POSITION;
float3 _Object_Scale = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)));

```

