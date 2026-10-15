# Release Notes — v10.7.1（经济生产链 & 发展型自动建造）

> 项目：PixelToCivilization（中华五千年 + 从远古到星际 · 开放世界 4X 文明模拟器）
> 日期：2026-10-16
> 定位：修复"自动扩张节奏"调查中发现的**最上游致命问题——经济生产链断裂**。

## 一、背景与根因

v10.7.1 用确定性多年模拟测试（120 年，全 29 插件，每年 TickYear/FinalizeYear + 600 帧 OnTick）跑出基线，发现比"行军接敌"更上游的致命问题：

- **基线（修复前）**：120 年里 `buildings 恒为 0`、`ground 恒为 15`（0 新增单位）、`wars=0`；仅殖民（settlements 5→25）与人口增长（510→2497）正常。
- **根因 1（P0 经济生产链断裂）**：全部 12 种建筑**没有任何建筑生产 Bronze/Iron/Coal/Oil/Steel/Electricity/Data/Capital**。现有建筑只产 Food/Wood/Stone/Gold/Goods。而所有军事单位（骑兵需 Bronze、战车需 Bronze、坦克需 Steel/Oil/Capital）、进阶塔（炮塔需 Bronze、碉堡需 Steel）、航母（Steel/Oil/Capital）都依赖这些无产出资源 → 自动建造/征兵/建军永远因资源不足失败。
- **根因 2（P1 AutoGovernor 决策缺陷）**：旧逻辑只"缺了才补"，且在 economy.OnYear 之后执行，本年劳力/人头产出已把资源补到安全线以上 → 每年判定"无需建造"，从不主动建 Quarry/Workshop/Barracks 等发展型建筑。

## 二、修复内容（完整文件）

### 1. 隐式产业链：建筑按时代副产出（不引依赖、不换架构）
- **`Assets/Scripts/Plugins/Tier0/Building/BuildingCatalog.cs`**
  - 新增 `BuildingEraOutput { Resource, Amount, MinEra }` 与 `BuildingCatalogEntry.EraOutputs[]`。
  - **Quarry（采石场）**：青铜时代→Bronze 3；铁器时代→Iron 2；工业时代→Coal 4 + Oil 2。
  - **Workshop（工坊）**：铁器时代→Iron 3；工业时代→Steel 3 + Electricity 2；信息时代→Data 3。
  - **Market（市集）**：工业时代→Capital 3；信息时代→Capital 5。
  - 设计为"隐式产业链"：建筑本身代表"采集→冶炼→加工"完整链路，不逐笔消耗原料；建筑成本与时代门槛是主要成本，产出受建筑数量自然限速。

### 2. 副产出年结
- **`Assets/Scripts/Plugins/Tier0/Building/BuildingPlugin.cs`**：OnYear 遍历建筑，除主产出外，对 `EraOutputs` 检查 `tech.HasEra(faction, MinEra)` 后额外产出（随等级缩放）；tech 缺失时副产出不产出（保守）。

### 3. 发展型 AutoGovernor
- **`Assets/Scripts/Plugins/Tier1/AutoGovernor/AutoGovernorPlugin.cs`**
  - 新增 `pxc.tech` 依赖（读时代）。
  - 决策由"缺了才补"升级为**按发展阶段生成候选优先级列表**，每势力每年选第一个通过 `CanBuild` 的类型建成（不再"想建但建不了就空过一年"）：
    1. 粮食安全（food < total×1.5 → Farm）
    2. 关键资源短缺（wood/stone/gold 跌破安全线 → 对应采集建筑）
    3. 基建覆盖（LumberMill/Quarry/Barracks/Market/Workshop 按时代各≥1）
    4. 工业扩展（工业时代第二套生产建筑）
    5. 持续发展（农田随人口，每 150 人 1 个）

### 4. 阵兵成本修正
- **`Assets/Scripts/Plugins/Tier0/GroundForce/GroundForceService.cs`**：Formation（原始时代步兵阵型）成本由 Food30+**Iron10**（原始时代无 Iron，矛盾）改为 Food30+**Wood10**（木矛/棍棒，原始可得）。

## 三、验证结果

确定性多年模拟（EditMode，无 Unity 下反射真实执行）：

| 指标 | 修复前 | 修复后 |
|---|---|---|
| buildings（120 年） | 0 | **44** |
| ground（地面单位） | 15（0 新增） | **23**（+8） |
| wars（宣战） | 0 | **4** |
| settlements | 25 | 25（殖民正常） |
| 总人口 | 2497 | 2497（正常） |

- **EditMode：Total 132 / Passed 132 / Failed 0。**
- 战争链上游已打通：高级资源有了 → 自动建造/征兵/建军恢复 → 军力发展 → AutoDiplomacy 按军力优势宣战。

## 四、未验证 / 已知缺口

- **战争结束数 resolutions=0（核心缺口，留 v10.7.2）**：宣战后部队仍不会"长途行军接敌"——GroundForceAi 单位 Idle 只在 PerceptionRadius（早期 8-12）内寻敌，而默认布局势力首府相距约 60，单位互相看不见、打不起来，故无灭国/割地/和约。v10.7.2 将实现"宣战时攻方单位向敌方首府/战区行军，接敌后进入追击/攻击"。
- **地面单位目标写死为 5/势力**：早期合理，但工业/现代时代应随时代扩展（坦克等），纳入后续版本。
- **真实团结编译/IL2CPP/WebGL `.wasm` 打包/官方测试运行器/原生平台：未验证**（需有团结许可机器 GUI 登录激活）；Roslyn 仅证明 C# 语法/类型/引用无编译错误，反射运行器在无 Unity 下真实执行 EditMode 断言，但非官方测试运行器。
- 高级资源"不逐笔消耗原料"的隐式产业链长期平衡：**未验证**（数值为初值，后续按模拟数据校准）。
