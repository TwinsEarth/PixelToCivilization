# Release Notes — v10.4.2

> 版本：v10.4.2
> 类型：粮食饥荒时序治本（两阶段年度结算）
> 日期：2026-10-16
> 上一版本：v10.4.1（存档闭环接线）

---

## 一、修复的核心问题

### 现象
文明模拟中，即使势力已经建造了农田（Farm，年产 Food 12），在粮食基础产出紧张的年份，系统仍可能在该年度判定饥荒、饿死人口，表现为"有农田也饿死"。

### 根因（时序 bug）
12 个模拟插件的年度 OnYear 按拓扑顺序执行，经济插件（`pxc.economy`）排在建筑插件（`pxc.building`）之前：

```
economy → population → settlement → building → tech → culture → weather → groundforce → naval → colonization → victory → smoke
```

旧版 `EconomyPlugin.OnYear` 在**同一次**调用中，对每个势力一次性计算

```
newFood = 粮食库存 + 劳力产出 − 粮食需求
```

并在 `newFood < 0` 时立即把库存钳制为 0、发布 `FamineEvent`。但**农田等建筑产出是在其后的 `BuildingPlugin.OnYear` 才通过 `economy.Add(Food)` 到账**。因此经济判定饥荒时，农田的粮食产出尚未计入，导致建筑产出被排除在饥荒判定之外。

### 修复（两阶段年度结算，治本）
把"粮食产出收集"与"粮食消耗/饥荒判定"拆成两个阶段：

1. **阶段 1 — `EconomyPlugin.OnYear`（在 building 之前）**：
   - 只结算劳力基础粮食产出（天气/政策系数）并加入库存；
   - 按**年初人口**计算并缓存本年度粮食需求（`_pendingFoodDemand`）；
   - 木/石/金基础采集、研究/文化产出照常；
   - **此阶段不再判定饥荒**。
2. **阶段 2 — `BuildingPlugin.OnYear`**：农田/市集等建筑产出照常 `economy.Add(Food)` 到账。
3. **阶段 3 — `EconomyPlugin.FinalizeYear`（GameLoop 年度后置钩子，所有 sim OnYear 完成后）**：
   - 从已收集齐全部产出（劳力 + 建筑）的库存统一扣除粮食需求；
   - 缺口才把库存置 0、发布 `FamineEvent`；否则写入结余。

`GameBootstrap` 在 GameLoop 注册年度钩子时，**先**注册 `FinalizeYear`、**再**注册 `SaveCoordinator.OnYearAdvanced`，保证自动存档存的是饥荒判定后的状态。

### 新年度时序
```
TickYear:
  economy.OnYear   （劳力产出 + 缓存需求，不判饥荒）
  population.OnYear（出生/成年/自然死亡）
  building.OnYear  （农田等建筑产出到账）
  …其余 sim
年度后置钩子:
  FinalizeYear      （扣粮食消耗、判定饥荒、FamineEvent → 饥荒死亡）
  SaveCoordinator   （必要时自动存档）
```

---

## 二、变更文件

| 文件 | 类型 | 说明 |
|---|---|---|
| `Assets/Scripts/Gameplay/Services/IEconomyYearFinalizer.cs` | 新增 | 年度后置结算接口 `FinalizeYear(int gameYear)` |
| `Assets/Scripts/Plugins/Tier0/Economy/EconomyPlugin.cs` | 修改 | 实现 `IEconomyYearFinalizer`；OnYear 只产出并缓存需求；新增 FinalizeYear 统一扣消耗/判定饥荒；注册/反注册/恢复段处理 |
| `Assets/Scripts/Bootstrap/GameBootstrap.cs` | 修改 | 版本号 → v10.4.2；在 SaveCoordinator 之前注册 FinalizeYear 年度钩子 |
| `Assets/Tests/EditMode/FamineTimingTests.cs` | 新增 | 3 个饥荒时序用例 |
| `Assets/Tests/EditMode/SaveRoundTripIntegrationTests.cs` | 修改 | AdvanceYears 镜像真实时序（TickYear → FinalizeYear → 自动存档） |
| `tools/unity_stub/NUnit.cs` | 修改 | 测试桩补充浮点 delta 容差重载 |

---

## 三、测试与验证

### 编译（Roslyn 4.8，无 Unity 环境）
6 个编译目标全部 exit=0：

```
unity=0  nunit=0  desktop=0  webgl=0  edit=0  play=0
```

### EditMode 反射测试
```
Total: 69   Passed: 69   Failed: 0
```
（v10.4.1 基线 66，新增 `FamineTimingTests` 3 个用例。）

新增用例：

| 用例 | 验证点 |
|---|---|
| `FarmOutputArrivesBeforeFamineCheck_NoFamine` | 农田产出（建筑）在 FinalizeYear 判定前到账，总产出 49 ≥ 需求 42，不饥荒，结余 7 |
| `WithoutFarms_BaseShortfall_StillFamine` | 无农田、基础产出 25 < 需求 42，仍发 1 次饥荒，缺口 17，库存置 0（防止过度放松） |
| `FamineEventOnlyAfterFinalize` | FamineEvent 只在 FinalizeYear 后发出；OnYear 后即使有缺口也不判定 |

测试口径（FakePopulation：labor=10, soldiers=20，total=30）：
- 基础粮食产出 = 10 × 2.5 = 25；
- 粮食需求 = 30 × 1.0 + 20 × 0.6 = 42；
- 基础缺口 = 17；2 个农田（2 × 12 = 24）补足后 49 − 42 = 7。

### 已知时序口径（留意）
- v10.4.2 后 `FamineEvent` 在年度后置钩子（`population.OnYear` 之后）发布。饥荒死亡由 `PopulationPlugin.OnFamine` 在该时刻同步处理，因此当年 `PopulationChangedEvent`（在 `population.OnYear` 中已发布）可能不含本次饥荒死亡，饥荒死亡计入下一游戏年的人口统计。这是当前最小改动下的时序细节；`PopulationChangedEvent` 仅用于聚合广播，不影响饥荒判定本身。如要求当年人口统计即含饥荒死亡，需另行调整发布口径（未在本版处理）。

---

## 四、未验证项（明确标注）

以下项在当前机器（无团结引擎许可导出）**未验证**：

- 真实团结引擎编译（IL2CPP）、WebGL `.wasm` 打包、官方测试运行器（EditMode/PlayMode）、原生平台构建。
- 真实浏览器运行下的饥荒/农田表现。

Roslyn 编译仅证明 C# 语法/类型/引用无编译错误；反射测试运行器在无 Unity 环境下真实执行 EditMode 断言，但非官方测试运行器。

---

## 五、兼容性

- 未改动 `IEconomyService` 接口，所有依赖经济扣费的系统不受影响。
- 未改变粮食产出/消耗口径常量，仅改变饥荒判定发生的时序（在建筑产出到账之后）。
- 存档格式/版本未改动（段序列化不变）。

---

## 六、下一步候选（按优先级）

1. AI 自动发展 / 自动建造（eXpand / eXploit 自动化）。
2. 三大历史 Bug（集结令插旗崩溃 / 骑兵困湖 / 塔防多层造型）真实浏览器回归。
3. 当年人口统计口径（`PopulationChangedEvent` 是否含饥荒死亡）裁决。
4. eXterminate 作战闭环深化。
