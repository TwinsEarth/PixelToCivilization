# Release Notes — v10.7.3（平衡自动扩张节奏）

## 背景
v10.7.2 的确定性多年模拟暴露了极端战争节奏：
- **前期疯狂**：4703–4714，11 年爆发 10 场战争，多方同时围殴同一弱者；
- **后期躺平**：4714–4792，约 78 年 0 场战争，世界长期停滞；
- 最后仅靠一场灭国结束。

本版本目标是让战争"有起有伏"：不疯狂围殴、不长期躺平、不形成单极雪球，玩家始终受保护。

## 本版本改动（完整文件）

### 1. 防围殴（战争以单挑为主）
**文件**：`Assets/Scripts/Plugins/Tier1/AutoDiplomacy/AutoDiplomacyPlugin.cs`
- 宣战候选目标若已在与任何第三方交战，则跳过（不趁火打劫）。
- 新增 `IsAlreadyInWar(faction, military, factions)`。
- 语义：杜绝"多方同时围殴同一弱者"的雪球开局。

### 2. 军力目标随时代
**文件**：`Assets/Scripts/Plugins/Tier1/AutoMilitary/AutoMilitaryPlugin.cs`
- 移除固定 `TargetGroundUnits=5 / TargetNavalUnits=3`，改为
  `TargetGroundUnitsForEra(era)` / `TargetNavalUnitsForEra(era)`。
- 早期军力少（Primitive 3 / Bronze 4），后期军力多（Modern 12 / Information 15 / Space 18），
  陆军建造、海军建造、航母建造三个步骤全部改用随时代目标。
- 解决"早期就疯狂堆兵 + 后期军力不增长"。

### 3. 代差优势（强时代势力可凭技术领先扩张）
**文件**：`Assets/Scripts/Plugins/Tier1/AutoDiplomacy/AutoDiplomacyPlugin.cs`
- 新增常量 `EraAdvantageGap=2`、`EraAdvantageRatio=1.1`。
- 当攻方时代领先守方 ≥ 2 个时代，军力比门槛由 1.5 降至 1.1。
- 让真正拉开技术代差的强时代势力能自然扩张（解决后期躺平）。

### 4. 灭国后清理战争矩阵（关键 Bug）
**文件**：`Assets/Scripts/Plugins/Tier1/WarResolution/WarResolutionPlugin.cs`
- 灭国前只置 `loser.Alive=false`，但胜者与已亡国家的战争矩阵位仍 > 0，
  导致胜者被 `MaxConcurrentWars=1` 永久卡死、再也无法宣战。
- 修复：灭国后立即 `military.MakePeace(winner, loser)` 清理矩阵；
  `CountCurrentWars` 也跳过已死势力（双保险）。

### 5. 战后冷却（防连续灭国滚雪球）
**文件**：`Assets/Scripts/Plugins/Tier1/AutoDiplomacy/AutoDiplomacyPlugin.cs`
- 新增 `PostWarCooldownYears=6` 与 `_lastWarEndYear[]`。
- 订阅 `FactionEliminatedEvent / PeaceTreatyEvent / CoercedPeaceEvent`，
  一场战争结束后攻方需休整 6 年才能再次宣战。
- 防止同一势力连续灭国形成单极统治。

### 6. 测试与诊断
**文件**：
- `Assets/Tests/EditMode/DeterministicSimPaceTests.cs`：注入战争改为"双方对等增援"
  （势均力敌），并打印宣战/灭国/占领/和约/割地时间线、军力、时代、建筑/资源/型号。
- `Assets/Tests/EditMode/AutoMilitaryTests.cs`：航母舰队门槛随时代更新（预置 12 坦克 + 6 潜艇）。

## 验证结果（确定性模拟，120 年，每帧 dt=0.1）
- **EditMode：134 / 134 / 0**（全部通过，无编译错误）。
- 战争节奏：**wars=5、elim=2、whitePeace=2、conquer=2、coerced=0**，
  5 场战争分布于 4710–4791（81 年），不再集中爆发。
- 既有和约（2 场），也有灭国（2 场），战争有起有伏；
  灭国由不同势力完成（f3 灭 f1、f2 灭 f4），无一势力连续滚雪球。
- 最终 **3 个势力存活**（玩家 f0 + f2 + f3），最后一场战争持续到模拟结束、世界仍在动态。
- **玩家 f0 全程存活**（`AiMayDeclareOnHumanPlayer=false` 保护）。
- 世界发展正常：settlements 5→19、buildings 5→48、人口 510→2485。

## 未验证项（诚实标注）
- 本版本平衡改动在**真实团结/Unity 引擎、IL2CPP、WebGL `.wasm` 打包、
  官方测试运行器、原生平台**上均为 **「未验证」**（需有团结许可的机器 GUI 激活）。
- 当前证据来自 Roslyn 语法/类型编译 + 无 Unity 反射运行器（真实执行 EditMode 断言）。
- 战争数值在 120 年确定性场景之外的更长跨度 / 随机地图下 **「未验证」**。

## 下一步（v10.7.4 候选）
- **军事现代化 / 单位换代**：早期建造的 Formation（阵兵）在进入现代后不会自动
  换代为坦克/装甲车，AutoMilitary 只新建不换代，老旧单位会拖低火力。
  计划设计兵种映射（骑兵→装甲车、战车→坦克）、换代成本与 `UpgradeModel` 接口。
- 结盟 / 附庸 / 集体防御、战争赔款、战胜方战争消耗衰减。
