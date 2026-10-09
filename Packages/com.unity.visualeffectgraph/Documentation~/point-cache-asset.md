# Point Cache 资源

Point Cache 资产遵循开源 [Point Cache](https://github.com/peeweek/pcache/blob/master/README.md) 规范并使用 `.pCache`  文件扩展名。在内部，这些资源是嵌套的 [Scriptable Objects](https://docs.unity3d.com/Manual/class-ScriptableObject.html)，包含表示粒子属性映射的所有各种纹理。

Point Cache 资源是只读的，因此，如果选择一个资源并在 Inspector 中查看它，则无法编辑其任何属性。但是，Inspector 会显示每个只读属性的值。有关每个只读属性的含义的信息，请参阅[属性](#属性)。

## 属性

Point Cache 资源显示只读信息，例如它包含的粒子数和表示粒子属性的纹理。

| **属性**    | **描述**                                              |
| --------------- | ------------------------------------------------------------ |
| **Script**      | 指定 Unity 用于导入 `.pCache` 文件的导入程序。 |
| **Point Count** | 此 Point Cache 表示的粒子数。         |
| **Surface**     | 表示粒子的属性贴图的纹理列表。数组的每个索引中的纹理名称与映射所针对的属性的名称相同。 |
