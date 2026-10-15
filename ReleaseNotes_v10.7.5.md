# Release Notes — v10.7.5（海军换代 / 蒸汽铁甲现代化）

## 背景
v10.7.4 完成了地面单位的机械化换代（骑兵→装甲车、战车→坦克），但海军同样只新建、
不换代：早期建造的木质桨帆战船、远洋风帆战舰，在势力进入工业时代后不会换装为
蒸汽铁甲舰，火力被拖低，也无法体现"蒸汽跨越"这一近代海军史关键变革。
本版本补上海军换代。

## 本版本改动（完整文件）

### 1. 蒸汽换代映射（数据元数据）
**文件**：`Assets/Scripts/Plugins/Tier0/Naval/NavalType.cs`
- 新增 `NavalGeneration.TryGetSteamUpgrade(current, era, out upgrade)`：
  - 时代达到 **Industrial** 后：
    - **战船 Warship → 蒸汽轮船 Steamship**（木质桨帆 → 蒸汽铁甲）；
    - **帆船 SailingShip → 蒸汽轮船 Steamship**（远洋风帆 → 蒸汽）；
    - 蒸汽机统一水面舰艇，近战战船与远洋帆船角色在工业化后合并。
  - 民用船（木筏 / 渔船）角色永恒不换代；
  - 蒸汽轮船不换代（现代水面舰艇，潜艇 / 航母由新建获得）；
  - 潜艇 / 航母不换代。

### 2. 换代接口与成本
**文件**：`Assets/Scripts/Plugins/Tier0/Naval/NavalService.cs`
- `TryUpgradeModel(slot, newModel)`：扣费后直接改型号，保留等级 / 经验 / 位置，
  速度 / 耐久 / 攻防按新 Profile 重置。
- `UpgradeFactionStep(faction, era)`：遍历该势力船艇，逐艘换代，资源不足跳过。
- `UpgradeCost(old, new)`：在老船体 / 船员基础上改造，比新建便宜：
  - 战船 / 帆船 → 蒸汽轮船：Steel90 + Coal40
    （新建蒸汽轮船为 Steel120 + Coal60）。

### 3. AI 自动换代
**文件**：`Assets/Scripts/Plugins/Tier1/AutoMilitary/AutoMilitaryPlugin.cs`
- 在海军新建步骤之前新增 `NavalUpgradeStep`：先把过时战船 / 帆船换代，再按时代目标补建。

### 4. 测试（新增 4 个 EditMode）
**文件**：`Assets/Tests/EditMode/AutoMilitaryTests.cs`
- 工业战船→蒸汽轮船（扣 Steel90+Coal40）；
- 工业帆船→蒸汽轮船；
- 时代不到工业战船不换代；
- 工业民用渔船不换代。

## 验证结果
- **EditMode：143 / 143 / 0**（139 + 4 海军换代，无编译错误）。
- 确定性 120 年模拟仍保持健康节奏，玩家存活。

## 未验证项（诚实标注）
- 真实团结 / Unity 引擎、IL2CPP、WebGL `.wasm`、官方测试运行器、原生平台均为 **「未验证」**
  （需有团结许可的机器 GUI 激活）。
- 换代成本 / 映射在更长跨度、随机地图、玩家手动操作下 **「未验证」**。

## 下一步（v10.7.6 候选）
- 结盟 / 附庸 / 集体防御，以及战争赔款、战胜方战争消耗衰减。
- 现代水面舰艇（驱逐舰 / 巡洋舰）的独立型号与信息化换代。
