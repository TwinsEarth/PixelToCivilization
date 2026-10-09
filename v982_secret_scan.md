# 《从像素到文明》V9.8.2 密钥扫描报告（Secret Scan）

> 版本：V9.8.2 ｜ 扫描方式：正则文本扫描 ｜ 扫描时间：2026-10-09

---

## 一、扫描范围

| 范围 | 路径 |
|---|---|
| 源码 | `E:\DB\pixel_to_civilization_win\PxC982\Assets`（全部 *.cs） |
| 构建产物 | `E:\DB\pixel_to_civilization_win\PxC982\BuildWebGL`（index.html / js / wasm / TemplateData） |
| 发布包 | `PixelToCivilization_V9.8.2_HTML5.zip`（含上述产物） |

## 二、扫描模式

- 历史泄漏密钥锚点：`ark-d727a76f`（火山方舟）、`sk-fc6aad92`（DeepSeek）。
- 通用密钥模式：`sk-[A-Za-z0-9]{16,}`、`ark-[A-Za-z0-9-]{16,}`（大小写不敏感）。

## 三、扫描结果

| 范围 | 命中数 |
|---|---|
| Assets 源码 | **0** |
| BuildWebGL 产物 | **0** |
| 结论 | ✅ 通过——V9.8.2 包不含任何 API 密钥 |

## 四、密钥管理约定

1. 开源版本**不含**任何密钥；九神 AI 默认离线即可完整运行。
2. 在线大模型密钥由用户在游戏内 帮助 / Debug / 九神面板 自行填写（不随包分发、不入库）。
3. 环境变量方式（可选）：Windows `setx PXC_ARK_API_KEY "..."`；macOS/Linux `export PXC_ARK_API_KEY="..."`。
4. 历史泄漏密钥已失效，不得再次提交入库；本次发布前已确认未复现。
