# Release Notes — v10.7.4（单位换代 / 军事现代化）

## 背景
v10.7.1–v10.7.3 的确定性模拟暴露一个真实核心缺陷：早期建造的阵兵 / 骑兵 / 战车，
在势力进入工业、现代后**不会自动换代**——AutoMilitary 只新建单位、不换代老单位。
结果一个进入现代的势力仍可能带着 6–7 个老旧 Formation，火力被严重拖低，
也无法体现"机械化跨越"这一近现代军事史的关键跃迁。

## 本版本改动（完整文件）

### 1. 机械化换代映射（数据元数据）
**文件**：`Assets/Scripts/Plugins/Tier0/GroundForce/GroundForceType.cs`
- 新增 `GroundForceGeneration.TryGetMechanizedUpgrade(current, era, out upgrade)`：
  - 时代达到 **Modern** 后：
    - **骑兵 Cavalry → 装甲车 Armor**（延续机动 / 侦察 / 投送角色）；
    - **战车 Chariot → 坦克 Tank**（延续冲击主力角色）；
    - **步兵 Formation 保持**（步兵是永恒兵种，现代仍有步兵，不机械化）；
  - 已机械化单位（Tank / Armor / MissileVehicle）不重复换代。

### 2. 换代接口与成本
**文件**：`Assets/Scripts/Plugins/Tier0/GroundForce/GroundForceService.cs`
- `TryUpgradeModel(slot, newModel)`：扣费后直接改型号，**保留等级 / 经验 / 位置 / 编队**，
  速度与耐久 / 攻防按新型号 Profile 重置（老兵延续战斗力）。
- `UpgradeFactionStep(faction, era)`：遍历该势力老单位逐一代，资源不足跳过（来年再试）。
- `UpgradeCost(old, new)`：在老底盘 / 人员基础上改造，比新建便宜、不重复征收 Capital：
  - 骑兵→装甲车：Steel50 + Oil25（新建装甲车为 Steel70+Oil40+Capital50）；
  - 战车→坦克：Steel90 + Oil40（新建坦克为 Steel120+Oil60+Capital80）。

### 3. AI 自动换代
**文件**：`Assets/Scripts/Plugins/Tier1/AutoMilitary/AutoMilitaryPlugin.cs`
- 年度顺序在征兵之后、新建之前新增 `UpgradeStep`：先把过时的骑兵 / 战车换代，
  再按时代目标补建。玩家势力默认跳过（`EnablePlayerAutomation` 可开启）。

### 4. 测试（新增 5 个 EditMode）
**文件**：`Assets/Tests/EditMode/AutoMilitaryTests.cs`
- 现代骑兵→装甲车（扣 Steel50+Oil25，数量不变）；
- 现代战车→坦克（扣 Steel90+Oil40）；
- 现代步兵不换代（不扣资源）；
- 资源不足骑兵不换代；
- 时代不到现代骑兵不换代。

## 验证结果
- **EditMode：139 / 139 / 0**（134 + 5 换代，无编译错误）。
- 确定性 120 年模拟仍保持健康节奏：wars=5、elim=2、whitePeace=2、3 势力存活，玩家存活。
- 换代不改变单位数量，只提升过时单位的机械化水平。

## 未验证项（诚实标注）
- 真实团结 / Unity 引擎、IL2CPP、WebGL `.wasm` 打包、官方测试运行器、原生平台均为 **「未验证」**
  （需有团结许可的机器 GUI 激活）。
- 换代成本与映射在更长跨度 / 随机地图 / 玩家手动操作下 **「未验证」**。
- 海军（风帆 → 蒸汽铁甲）的换代暂未实现，仅完成地面单位换代。

## 下一步（v10.7.5 候选）
- 海军换代：风帆战舰 → 蒸汽铁甲舰 → 现代舰艇（当前海军同样只新建不换代）。
- 结盟 / 附庸 / 集体防御、战争赔款、战胜方战争消耗衰减。
- 导弹车的信息化换代与防空 / 反导逻辑。
