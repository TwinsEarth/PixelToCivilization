# Release Notes — v10.4.4：自动军事建设

## 背景
审计发现军事成本（`GroundForceService.RecruitCost`、`NavalService.BuildCost`）虽已定义，却**从未被任何地方调用**——除新游戏初始免费编队外，没有任何"付费造单位"入口；非玩家势力也不会自动征兵或备战。v10.4.3 已让 AI 自动建经济建筑，v10.4.4 补齐军事建设闭环。

## 新增

### 1. 付费建造入口：`GroundForceService.TryBuildUnit`
- 扣费前完成全部校验（HardCap 上限、水域、经济服务）；
- 按 `RecruitCost(model)` 调 `economy.TrySpendBundle` 扣费，成功才 `Spawn`；
- 资源不足 / 水域 / 达上限均拒绝并返回 `Invalid`，**绝不扣了费却失败**；
- 是制造地面单位的唯一入口（初始免费编队除外）。

### 2. 自动军事建设：`AutoMilitaryPlugin`（Tier-1，`pxc.automilitary`）
年度对每个存活势力执行三步：

- **① 自动征兵**：维持士兵占总人口 15%；每年每势力最多 5 人；保留 20 名基础劳动力；
- **② 付费建造陆军单位**：按时代选型（青铜→骑兵，铁器→战车，现代→坦克，信息→导弹车），目标陆军单位 5 个，经 `TryBuildUnit` 扣费；
- **③ 战时自动建防御箭塔**：检测到与任意势力交战时，首府周边自动建箭塔，目标 2 个。

玩家势力默认跳过（保留控制权）；`EnablePlayerAutomation` 可开启。纯决策、无运行时状态，不产生存档段。

### 3. 编译期注册
`AutoMilitaryRegister`（BeforeSceneLoad）。

## 测试
- 新增 `AutoMilitaryTests`（9 项）：征兵、达标不征、付费建骑兵扣费、资源不足不建、战时建箭塔、和平不建塔、玩家跳过、原始时代无 Iron 不建、单位达目标不建。
- Roslyn 6 目标编译全部 exit=0；EditMode **Total 87 / Passed 87 / Failed 0**（基线 78 + 新增 9）。

## 未实现 / 未验证
- 未做自动造船 / 海军单位（`NavalService.BuildCost` 仍未接线）；未做主动宣战；战时仅建箭塔，未建火塔/炮塔。
- 真实团结引擎编译 / IL2CPP / WebGL `.wasm` 打包 / 官方测试运行器 **未验证**（需有团结许可机器）；Roslyn 仅证明 C# 语法/类型/引用无误，反射运行器非官方测试运行器。
- 军事自动决策的长期平衡（长期跑批）**未验证**。
