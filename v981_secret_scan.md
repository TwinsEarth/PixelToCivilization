# V9.8.1 密钥扫描报告

## 扫描对象
- `E:\DB\pixel_to_civilization_win\v981_web`（V9.8.1 构建部署目录，22 文件，88.9MB）
- 发布包 `PixelToCivilization_V9.8.1_HTML5.zip`（54.6MB，同内容）

## 扫描规则
| 模式 | 含义 | 命中数 |
|---|---|---|
| `sk-fc6aad92462b4e9fb926bb5da8c9122f` | 历史 DeepSeek Key（用户曾要求移除） | 0 |
| `ark-d727a76f` | 历史火山方舟 Key | 0 |
| `sk-[0-9a-f]{32}` | 通用 32 位十六进制 API Key | 0 |

## 结论
**CLEAN — 0 命中，无任何密钥随包分发。**

## 备注
- 初次扫描 `sk-` 宽泛模式命中 BuildWebGL.data 内 `sk-charactermaskCharactermatch…`，为 Unity 着色器/材质属性名（`_CharacterMask` 等）在二进制序列化中的巧合字符串，非 API Key。
- 游戏内 API Key 输入：九神面板 / Debug 控制台 / 帮助界面（按钮弹窗输入，支持剪贴板粘贴），不随包分发。
