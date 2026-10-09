# V9.8.3 密钥扫描报告

- 扫描对象：本轮 6 个修改源码
  - Assets/Scripts/Systems/WarBroadcastSystem.cs
  - Assets/Scripts/Systems/RallySystem.cs
  - Assets/Scripts/Systems/GroundWarfareSystem.cs
  - Assets/Scripts/Buildings/BuildingMeshFactory.cs
  - Assets/Scripts/Editor/WebGLBuilder.cs
  - ProjectSettings/ProjectSettings.asset
- 扫描模式：`ark-[a-zA-Z0-9-]{20,}` / `sk-[a-zA-Z0-9]{20,}` / `ghp_[a-zA-Z0-9]{20,}` / `github_pat_[a-zA-Z0-9_]{20,}`
- 结果：**0 命中（CLEAN）**

## 历史密钥纪律
- 历次版本（v980/v981/v982）源码+构建产物扫描均 0 命中。
- 开源版本不含任何密钥；九神 AI 联网密钥由用户游戏内「帮助 / Debug / 九神面板」自行填写，不随包分发。
- 用户提供的测试 Key（ark-*/sk-*）绝不写入源码、配置、文档或 zip。
