# Point Cache

菜单路径：**Operator > Utility > Point Cache**

**Point Cache** 运算符公开存储在 [Point Cache 资源](point-cache-asset.md) 中的属性映射和点计数。

## 运算符设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Asset** | Point Cache 资源 | 此运算符引用的点缓存资源。 |

## 运算符属性

基于 **Asset**，AttributeMap 输出的数量发生更改以匹配存储在 Point Cache 资源中的属性数量

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **Point Count** | uint | 存储在 Point Ache 资源中的值的数量。 |
| **AttributeMap：<attribute>（多个）** | Texture2D | 包含值的属性映射 (Texture2D)。 |

## 备注

如果此运算符尝试读取的属性尚未写入，则返回其类型的默认标准值。
