# Release Notes — v10.4.7：战争结果 / 灭国闭环

## 背景
审计发现 `FactionInfo.Alive` **永远为 true**——宣战（v10.4.6）+ 战斗（单位可被击杀）之后，没有任何机制判定战争结果或消灭势力。战争只有开头、没有结尾。本版补齐灭国闭环。

## 新增

### 1. 战争结果：`WarResolutionPlugin`
- 年度对每一对正在交战的势力，统计双方剩余作战单位（地面单位 + 船艇）：
  - **一方作战单位归零、对方仍有单位 → 败者被灭国**（`FactionInfo.Alive = false`）；
  - 发布 `FactionEliminatedEvent { Faction, ByFaction, GameYear }`；
  - 双方均有单位（战斗继续）或双方均为 0（平局，互不灭国）不处理。
- 依赖 military / groundforce / naval / automilitary / autodiplomacy（在当年军事建设、宣战评估之后结算）。

### 2. 新事件：`FactionEliminatedEvent`
- 记录败者、胜者、年份。

## 修改
- `VictoryPlugin` 依赖增加 `pxc.warresolution`：灭国先结算，胜利随后检测，**当年灭国当年即可触发征服胜利**。
- 存档往返集成测试的插件清单补齐 AutoGovernor / AutoMilitary / AutoDiplomacy / WarResolution。

## 测试
- 新增 7 项（EditMode 99→106）：无单位被灭、双方有单位不灭、双方无单位平局不灭、非交战不灭、仅船艇不灭、玩家可被灭、灭国触发征服胜利。
- Roslyn 6 目标编译全部 exit=0；EditMode **Total 106 / Passed 106 / Failed 0**。

## 未实现 / 未验证
- 未做和约/停战（双方僵持时战争持续）；未做割地、赔款、附庸。
- 灭国后败者的聚落/建筑/人口未做归属转移或清理（仅标记势力死亡）。
- 战争结果的长期节奏 **未验证**。
- 真实团结引擎编译 / IL2CPP / WebGL `.wasm` 打包 / 官方测试运行器 **未验证**（需有团结许可机器）。
