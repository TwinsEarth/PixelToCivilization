# Release Notes — v10.4.9：自动和约（白和约）

## 背景
当前战争一旦开打，若双方都保有作战单位、实力接近（谁也无法灭国），战争会无限持续，没有结束路径。本版补齐"僵持战争"的自然结束：满足条件的双方在持续若干年后自动停战。

## 改造
### 1. 战争矩阵记录开始年份
- MilitaryService 的战争矩阵由 `bool[,]`（仅交战/未交战）改为 `int[,]`（0=未交战，否则记录开始的游戏年份）。
- `DeclareWar(f, other, startYear)`（已交战不覆盖原开始年份）、新增 `MakePeace(f, other)`（清除交战关系）、`GetWarStartYear(f, other)`。
- 存档段 MilitarySaveSegment 新增 StartYears（与交战对一一对应，随存档持久化）。
- MilitaryPlugin 订阅 WarDeclaredEvent 时，从 IClock 取当前游戏年份记录开始时间（无时钟环境按 1 年）。

### 2. 自动和约：AutoPeacePlugin
- 年度对每对交战势力（双方存活）：
  - 双方仍都有作战单位（地面 + 海军，排除一方归零的灭国路径）；
  - 战争持续 ≥ 5 个游戏年；
  - 作战单位数量接近（max/min ≤ 1.5）；
  - 全满足 → MakePeace 结束战争，发布 PeaceTreatyEvent（无胜负）。
- 一方单位归零的灭国路径仍由 WarResolutionPlugin 处理（本插件不介入）。

### 3. 新事件：PeaceTreatyEvent
- 记录 FactionA、FactionB、WarStartYear、GameYear。

## 测试
- 新增 5 项（EditMode 110→115）：僵持满 5 年签和约、持续不足不签、实力悬殊不签、一方无单位不签、开始年份与 MakePeace 存档口径。
- 同步更新 MilitaryTests/AutoMilitaryTests/WarResolutionTests/AutoDiplomacyTests 的战争接口签名。
- Roslyn 6 目标编译全部 exit=0；EditMode **Total 115 / Passed 115 / Failed 0**。

## 未实现 / 未验证
- 未做带胜负的和约（割地、赔款、附庸）；本版仅"无胜负白和约"。
- 最小战争年数（5）、接近比（1.5）为设计初值，长期平衡（是否过早停战、是否仍有无限战争）**未验证**。
- 真实团结引擎编译 / IL2CPP / WebGL `.wasm` 打包 / 官方测试运行器 **未验证**（需有团结许可机器）。
