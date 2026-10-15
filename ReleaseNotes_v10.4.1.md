# v10.4.1 发布说明 — 存档闭环接线（自动存档 / 读档恢复）

## 核心目标

v10.4.0 已保证存档段布局一致，但审计确认存档读写 API（`SaveSystem.Write/Read`）与插件段
收集/恢复（`PluginRuntime.CollectSegments/RestoreSegments`）**虽已实现却从未被任何代码调用**，
共享 `FactionRegistry` 也未完整序列化——即游戏不会自动保存、也不会读档恢复。

本版本接通两条路径：

1. **存档路径**：存档触发 → 收集全局状态/势力快照/各插件段 → 原子写入；
2. **读档路径**：启动读档 → 重建势力 → 恢复时钟/插件段 → 启动。

## 审计确认的闭环缺口

- `CollectSegments` / `RestoreSegments` / `Write` / `Read` 全部分布在 `Scripts` 中且从未被调用，
  只有 `HasSave` 被 GameBootstrap、GroundForce、Naval 使用。
- 势力（名称、玩家归属、颜色、发源点、灭国状态）没有任何存档载体，读档后无法重建。

## 主要变更

### 1. 共享势力快照（新增 DTO）
- 新增 `Assets/Scripts/Core/Persistence/FactionSnapshot.cs`：`Name / IsPlayer / Alive(默认 true) /
  ColorIndex / HomeX / HomeZ`。
- `SaveData` 顶层在 `FactionCount` 后新增 `List<FactionSnapshot> Factions`，按 factionId 顺序收集。

### 2. 集中存档入口（新增 SaveCoordinator）
- 新增 `Assets/Scripts/Infra/Save/SaveCoordinator.cs`，统一所有存档入口，防止并发写入与多处直接写盘：
  - `SaveGame(slot, onDone)`：手动存档，入异步写队列（每帧 ≤1 写）；
  - `SaveImmediate(slot)`：同步落盘，退出/暂停等不可依赖后续帧的场景；
  - `OnYearAdvanced(gameYear)`：跨过 `AutoSaveYears`（20 游戏年）写主槽，用 `_lastAutoSaveYear`
    防止高倍速同帧多年时重复触发；
  - `BuildData()`：写入 Version / BuildVersion / GameYear / Day / FactionCount、逐势力快照、
    Population = Σ各势力 `IPopulationService.Total`，再 `runtime.CollectSegments`。
  - 常量：`SaveVersion=0`、`MainSlot=0`、`AutoSaveYears=20`。

### 3. GameLoop 年度钩子
- `GameLoop` 新增 `_yearHandlers` + `AddYearHandler`；年度推进时先 `runtime.TickYear`（结算该年）
  再回调年度钩子（存档存的是结算后状态）。

### 4. GameBootstrap 读档时序
- 启动顺序：`BuildOrder` → `saves.Read(0)`：
  - 有存档：`ApplyFactionSnapshot` 重建 FactionRegistry（含灭国/玩家/颜色）→ `clock.Restore(GameYear, Day)`
    → `InitializeAll` → `RestoreSegments`；
  - 无存档：`NewGameSetup.PopulateNewWorld` → `InitializeAll`；
  - 最后 `StartAll`。
- `ApplyFactionSnapshot`：按快照顺序 Add 保证 factionId 对齐；旧存档无快照（v10.4.1 新增）时
  按默认开局布局兜底重建。
- GroundForce / Naval 用 `saves.HasSave(0)` 自行判断是否生成初始部队（读档时跳过，由段恢复）。
- 退出：QuitHandler 先 `SaveImmediate(0)` 再 `MarkCleanRun`（WebGL 关闭事件不可靠，靠自动存档兜底）。

### 5. 初始化顺序循环依赖修复（治本）
- 现象：拓扑顺序 groundforce/naval（依赖 economy/population/tech）在 combat（依赖 groundforce/naval）
  之前，但 GroundForce/Naval 在 `OnInitialize` 强 `Resolve<ICombatService>`（combat 尚未注册，
  `Resolve` 抛 `InvalidOperationException`），且 AI 在 OnInitialize 即被传入 combat（此时必为 null，
  之后永不更新）。
- 修复：`GroundForceAi` / `NavalAi` 构造第 4 参由 `ICombatService` 改为 `Func<ICombatService>`
  （延迟解析），开火时才解析；GroundForce/Naval 插件 OnInitialize 删除强 Resolve 与 `_combat` 字段，
  传 `() => _ctx.Services.TryResolve<ICombatService>(out var c) ? c : null`。

### 6. 测试基础设施修复（无符号整数序列化）
- 现象：跑年后天气/灾害的 xorshift64 RNG 状态（`ulong`）最高位为 1（> long.MaxValue），
  反射 JSON 序列化器用 `Convert.ToInt64(ulong)` 抛 `OverflowException: Value was either too large
  or too small for an Int64`。
- 修复：`tools/unity_stub/JsonUtility.cs` 对整数/枚举按底层类型区分无符号，无符号类型用
  `Convert.ToUInt64` 输出（对齐 Unity JsonUtility 对无符号字段的处理）。
  - 该问题仅存在于离线测试桩；真实 Unity JsonUtility 对 ulong 正常。

## 验证结果

- Roslyn 4.8 编译 6 目标（UnityEngine/NUnit 桩、Game Desktop、Game WebGL、EditMode、PlayMode）
  全部 exit=0，无编译错误。
- 反射式 EditMode 测试（无 Unity 环境，真实断言）：**66 / 66 全部通过**。
  - 新增 `SaveRoundTripIntegrationTests` 3 个：
    - `FullGame_SaveThenLoad_RestoresFactionsClockPopulationSettlements`：跑 30 年 → 立即存档 →
      全新装配读档，断言势力数=5、势力名、玩家标记、时钟年份、顶层 Population 与恢复后聚合一致、
      聚落数一致；
    - `AutoSave_FiresAtInterval_AndPersists`：跑 25 年，主槽在第 20 年落盘、LastAutoSaveYear 正确；
    - `ManualSave_AsyncQueue_PersistsAfterTick`：手动异步存档入队后未落盘、Tick 后落盘且回调成功。

## 未验证项（需团结许可机器）

以下项受限于本机团结引擎许可，**未验证**：
- 团结引擎真实编译（IL2CPP）、WebGL `.wasm` 实际打包；
- 读档路径（`clock.Restore`、`RestoreSegments`、势力快照恢复、P0 循环依赖修复）在真实 Unity 下的运行；
- EditMode / PlayMode 的 Unity 官方测试运行器结果；
- 原生平台（Windows/macOS/iOS/Android）构建与运行；
- 退出存档在 WebGL 关闭事件下的实际触发（设计上靠 20 年自动存档兜底）。
- 注：离线测试桩对 `ulong` RNG 经 double 解析存在低 12 位精度损失（真实 Unity 为精确 64 位解析），
  仅影响桩内天气序列逐位可复现性，不影响存档闭环。

## 兼容性

- 保持原有玩法与核心模拟循环；
- 未新增第三方依赖（仍为自研轻量 SoA + JsonUtility，禁用 BinaryFormatter）；
- 旧存档（无势力快照）读档时按默认开局布局兜底重建。
