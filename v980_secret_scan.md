# V9.8.0 密钥扫描记录

- 日期：2026-10-08
- 扫描对象：
  1. 发布包目录 `E:\DB\pixel_to_civilization_win\v980_web`（含 wasm/framework/data/js/html/服务器脚本）
  2. 源码工程 `E:\DB\pixel_to_civilization_win\TuanjieCivilization_V9\Assets`（*.cs/js/json/html/txt/md/jslib）

## 扫描规则（正则）

| 规则 | 目标 |
|---|---|
| `ark-[a-f0-9]{8}` | 火山方舟 Ark Key |
| `sk-[a-f0-9]{20,}` | DeepSeek / OpenAI 风格 Key |
| `fc6aad92462b4e9fb926bb5da8c9122f` | 历史提供的 DeepSeek Key 明文 |
| `d727a76f-75cc-4a46-9493-28497603edc7` | 历史提供的 Ark Key 明文片段 |

## 结果

```
发布包 HIT_COUNT = 0
源码   SOURCE_HIT_COUNT = 0
RESULT: CLEAN
```

## 说明

- 所有 AI 密钥均为运行时由玩家在游戏内智能体面板 / 帮助 / Debug 通过按钮弹窗填写，存于 PlayerPrefs（WebGL 为 IndexedDB），**不编译进包、不随包分发**。
- 开源版本不含任何密钥；默认离线即可完整运行。
