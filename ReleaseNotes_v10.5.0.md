# Release Notes — v10.5.0：船艇经济产出

## 背景
渔船（FishingBoat）、木筏（Raft）、帆船（SailingShip）建造后没有任何经济产出——AI 投入资源建了渔船/帆船却不产出食物/金币，海军建设缺乏经济价值。本版补齐船艇经济产出。

## 改造
### 1. NavalService 新增按船型统计
- `CountByModel(int faction, NavalModel model)`：统计某势力某船型的存活数量。

### 2. 年度经济接入船艇产出
- EconomyPlugin.FinalizeYear（所有 sim OnYear 之后、扣口粮前）：
  - 渔船每条年产食物 8；木筏每条年产食物 2；
  - 帆船每艘贸易年产金币 5；
  - 船艇产出在扣口粮前到账，渔船食物可帮助养活人口。
- NavalService 为可选依赖（TryResolve、不声明依赖，避免与 naval 拓扑形成环）。

## 测试
- 新增 4 项（EditMode 115→119）：渔船产食物、木筏产少量食物、帆船产金币、渔船产出可养活人口。
- Roslyn 6 目标编译全部 exit=0；EditMode **Total 119 / Passed 119 / Failed 0**。

## 未实现 / 未验证
- 战船/潜艇/航母未加入经济产出（纯军事单位）；产出系数（8/2/5）为设计初值，长期平衡**未验证**。
- 真实团结引擎编译 / IL2CPP / WebGL `.wasm` 打包 / 官方测试运行器 **未验证**（需有团结许可机器）。
