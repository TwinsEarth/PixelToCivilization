# Release Notes — v10.4.5：自动海军建设

## 背景
v10.4.4 已让 AI 自动征兵、付费建陆军单位、战时建箭塔，但 `NavalService.BuildCost` 同样**定义了却从未被调用**——除新游戏初始免费木筏外，没有任何付费造船入口。本版补齐海军建设闭环。

## 新增

### 1. 付费造船入口：`NavalService.TryBuildUnit`
- 在势力首府附近找水域（`FindWaterNear`，半径 40）；
- 扣费前完成上限（HardCap 200）/ 水域 / 经济全部校验；
- 按 `BuildCost(model)` 调 `economy.TrySpendBundle` 扣费，成功才在水域 `Spawn`；
- **内陆势力（无水域）/ 资源不足 / 达上限均拒绝**（扣费前完成全部校验，绝不扣了费却失败）。

### 2. 自动造船：`AutoMilitaryPlugin.NavalStep`
- 年度对每个存活势力按时代选船型（青铜→渔船，铁器→战船，大航海→帆船，工业→轮船，现代→潜艇）；
- 目标船艇 3 艘，经 `TryBuildUnit` 付费建造；
- **内陆势力 `FindWaterNear` 失败自动跳过**（不造船）；
- 依赖新增 `pxc.naval`。玩家势力默认跳过。

## 测试
- 新增 4 项（EditMode 总数 87→91）：青铜靠水造渔船（扣 Wood 25）、内陆无水域不造船、铁器造战船（扣 Wood 80 + Iron 20）、船艇达目标 3 不建。
- Roslyn 6 目标编译全部 exit=0；EditMode **Total 91 / Passed 91 / Failed 0**。

## 未实现 / 未验证
- 未做主动宣战；未做航空母舰自动建造（`ChooseNavalModel` 现代首选潜艇）；战时仅建箭塔（未建火塔/炮塔）。
- 渔船/帆船的经济产出（渔业、贸易）尚未接线。
- 真实团结引擎编译 / IL2CPP / WebGL `.wasm` 打包 / 官方测试运行器 **未验证**（需有团结许可机器）。
- 海军自动决策的长期平衡（长期跑批、海战推演）**未验证**。
