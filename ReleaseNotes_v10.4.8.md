# Release Notes — v10.4.8：灭国领土占领

## 背景
v10.4.7 补齐了灭国判定，但败者被灭国后，其聚落、建筑、人口仍挂在败者名下（一个已死亡的势力），成为"无主资产"，逻辑不完整。本版补齐灭国后的领土归属：胜者占领败者的全部领土。

## 新增

### 1. 领土占领：`ConquestPlugin`
- 订阅 `FactionEliminatedEvent`：败者被灭国后，
  - 败者的全部聚落（SettlementRegistry）归属转移给胜者；
  - 败者的全部建筑（BuildingRegistry）归属转移给胜者；
  - 人口绑定在聚落 slot 上（settlement×layer SoA），聚落归属改变后人口随之归属胜者；
  - 发布 `TerritoryConqueredEvent`，记录转移的聚落数、建筑数。
- 依赖 `pxc.warresolution`（灭国先结算，占领随后立即处理，同年完成）。
- 无运行时状态、无存档段（归属转移结果直接落在聚落/建筑注册表，随其存档）。

### 2. 新事件：`TerritoryConqueredEvent`
- 记录 Loser、Winner、Settlements、Buildings、GameYear。

## 测试
- 新增 4 项（EditMode 106→110）：聚落/建筑/人口转移、事件数量正确、无资产正常发布、胜者自身资产不受影响。
- Roslyn 6 目标编译全部 exit=0；EditMode **Total 110 / Passed 110 / Failed 0**。

## 未实现 / 未验证
- 未做"只占领实际控制范围内聚落"的判定（本版转移败者全部聚落）；战败者聚落若远离胜者也一并转移。
- 未做和约/停战、割地、赔款、附庸。
- 占领的长期平衡（滚雪球）**未验证**。
- 真实团结引擎编译 / IL2CPP / WebGL `.wasm` 打包 / 官方测试运行器 **未验证**（需有团结许可机器）。
