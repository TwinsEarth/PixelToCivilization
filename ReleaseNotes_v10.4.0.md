# v10.4.0 发布说明 — 人口模型统一（治本）

## 核心目标

消除"势力人口"与"聚落人口"两套独立账本长期背离的问题（末态两者曾相差约 12 倍）。
本版本将人口的唯一权威存储改为 **聚落 × 分层（settlement × layer）**：

- 聚落人口 = 该聚落各分层（儿童/劳力/工匠/士兵/学者/老人）之和；
- 势力人口 = 该势力所有聚落人口之和（聚合得出，不再单独维护）；
- 移民改为聚落间的空间转移，**人口总量严格守恒**；
- 删除聚落自带的独立人口账本，从根上杜绝两套口径。

## 审计确认的三处背离根因

1. **初始对不上**：PopulationState 每势力 100 人，而首都 SettlementRecord 只有 50 人。
2. **增长率不同**：人口净增长约 0.02，聚落约 0.025 且受天气影响。
3. **移民不回流**：殖民移民在两个独立账本中各记各的，维度不同（faction×layer 6 层 vs 聚落单个 int），移民永不从 PopulationState 回流。

## 主要变更

### 人口唯一权威 = settlement × layer
- `PopulationState` 重写为按聚落 slot 索引（与 `SettlementRegistry` slot 对齐），每聚落 6 个分层，连续 SoA 布局 `[slot * 6 + layer]`；含倍增扩容、槽位清理、按 raw 长度反推恢复。
- `IPopulationService` 保留势力聚合接口（Total/ByLayer/BirthRate/DeathRate/StarvationRisk/TryAssign/ApplyExcessDeaths），使 Military/Economy/Disaster/Victory 签名不变、自动生效；新增聚落粒度接口（SettlementTotal/SettlementByLayer/ClearSettlement/SeedCapital/SendMigrants）；删除旧的 Emigrate。

### 年度结算集中到聚落
- 出生采用 logistic 饱和模型（每聚落容量 550，饱和时出生率最低系数 0.30）；
- 儿童成年（Children → Laborer，比例 0.12）；
- 自然死亡（0.015，按 老人→儿童→劳力→工匠→学者→士兵 顺序）；
- 聚落人口硬安全阀 5000（正常 550 先饱和，不会触发）；
- 政策系数（IPolicyService，可选）逐势力缓存，缺失/非法值安全兜底为 1；
- 饥荒死亡按 老人→儿童→士兵→工匠→学者→劳力 顺序（优先保住劳力）。

### 聚落不再独立持有/保存人口
- 删除 `SettlementRecord.Population` 字段与 `Found` 的 initialPopulation 参数；
- `SettlementProfile` 删除人口口径，仅保留发展度与领土半径；
- `SettlementService` 人口查询全部委托 `IPopulationService`，年度只做发展度累积与升级。

### 殖民移民聚落间转移（守恒）
- 殖民移民检查改为查首都劳力；
- `TryFoundSettlement` 流程：扣资源 → 建立空聚落 → 贴地 → 从首都向新聚落转移移民；
- 任一环节失败则清理新聚落、回收槽位并回滚资源。

### 存档段升级
- 人口段与聚落段 `SegmentVersion` 均升至 2；
- 人口段记录 SlotCapacity + 分层数组（删除 FactionCount）；
- 聚落段按 slot 顺序收集，恢复时顺序 Found 以对齐人口段布局。

## 验证结果

- Roslyn 4.8 编译 6 目标（UnityEngine/NUnit 桩、Game Desktop、Game WebGL、EditMode、PlayMode）全部 exit=0，无编译错误。
- 反射式 EditMode 测试（无 Unity 环境，真实断言）：**63 / 63 全部通过**。
  - 其中 `SettlementTests` 9 个（含首都 100 人、移民守恒、AutoExpand 等）逐一执行通过；
  - `MilitaryTests` 5 个（改 FakePopulation 后）逐一执行通过。

## 未验证项（需团结许可机器）

以下项受限于本机团结引擎许可，**未验证**：
- 团结引擎真实编译（IL2CPP）、WebGL `.wasm` 实际打包；
- EditMode / PlayMode 的 Unity 官方测试运行器结果；
- 原生平台（Windows/macOS/iOS/Android）构建与运行；
- 存档读档路径的完整接线（PluginRuntime.RestoreSegments 当前未被 GameBootstrap 调用，本版本存档段主要保证布局一致，为后续接线做准备；新游戏 InitializeAll 路径正常）。

## 兼容性

- 保持原有玩法与核心模拟循环；
- 未新增第三方依赖（仍为自研轻量 SoA + JsonUtility，禁用 BinaryFormatter）；
- 依赖人口的经济/军事/灾害/胜利系统因接口签名保留而自动生效。
