
Time 节点
=======


描述
--


允许访问着色器中的各种 **Time** 参数。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Time | 输出 | Float | 无 | 时间值 |
| Sine Time | 输出 | Float | 无 | 时间正弦值 |
| Cosine Time | 输出 | Float | 无 | 时间余弦值 |
| Delta Time | 输出 | Float | 无 | 当前帧时间 |
| Smooth Delta | 输出 | Float | 无 | 已平滑的当前帧时间 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
float Time_Time = _Time.y;
float Time_SineTime = _SinTime.w;
float Time_CosineTime = _CosTime.w;
float Time_DeltaTime = unity_DeltaTime.x;
float Time_SmoothDelta = unity_DeltaTime.z;

```

