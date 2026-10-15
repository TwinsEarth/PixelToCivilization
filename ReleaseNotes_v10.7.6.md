# Release Notes — v10.7.6（战争附庸与纳贡体系）

## 背景
v10.7.6 之前，战争结局只有三档：一方单位归零 → 灭国（WarResolution）；
强者碾压、弱者有可割让聚落 → 割地（CoercedPeace）；僵持 → 自动和约（AutoPeace）。
但历史上更常见的结局是：弱方无力再战、又不愿（或无处）割地，于是向强方
**称臣纳贡**，成为附庸，保留社稷。本版本补齐这一中间结局，并建立附庸权利义务。

## 本版本改动（完整文件）

### 1. 附庸关系数据
- `Assets/Scripts/Gameplay/Factions/FactionRegistry.cs`：FactionInfo 加
  `int Suzerain`（宗主，-1=独立）、`int BecameVassalYear`、`bool IsVassal`。
- `Assets/Scripts/Core/Persistence/FactionSnapshot.cs`：加对应存档字段。
- `SaveCoordinator`（写）/ `GameBootstrap`（读）接线。附庸关系随顶层 FactionSnapshot 持久化。

### 2. 附庸化与纳贡插件（新增）
**文件**：`Assets/Scripts/Plugins/Tier1/Vassalization/VassalizationPlugin.cs`（+ Register）
- 触发：交战满 3 年、双方都仍有单位、强者/弱者单位比 > 3.5（比割地 3.0 更极端）、
  弱者已无可割让非首府聚落（否则由割地处理）→ 弱者向强者称臣。
- 纳贡：附庸每年向宗主缴纳金币（Capital）= max(10, 存量 ×10%)，有则缴纳。

### 3. 附庸外交约束
- `Assets/Scripts/Plugins/Tier1/AutoDiplomacy/AutoDiplomacyPlugin.cs`：附庸跳过主动宣战。

### 4. 宗主保护与附庸解放
- 第三方宣附庸战时，宗主自动发布 WarDeclaredEvent 参战（MilitaryPlugin 接线）。
- 宗主被灭时，其附庸自动恢复独立（订阅 FactionEliminatedEvent）。

### 5. 测试（新增 7 个 EditMode）
**文件**：`Assets/Tests/EditMode/VassalizationTests.cs`
- 极端碾压只首府→附庸化；有村庄→不附庸（等待割地）；纳贡；
- 宗主保护；战争不足 3 年不附庸；实力比不足 3.5 不附庸；宗主被灭附庸解放。

## 验证结果
- EditMode：150 / 150 / 0（143 + 7 附庸，无编译错误）。
- 确定性 120 年模拟仍保持健康节奏，玩家存活。

## 未验证项（诚实标注）
- 真实团结 / Unity 引擎、IL2CPP、WebGL `.wasm`、官方测试运行器、原生平台均为 **「未验证」**。
- 附庸跟随宗主战争、附庸长期独立倾向 / 反叛、多级附庸（附庸的附庸）本期未实现。

## 下一步（v10.7.7 候选）
- 战争赔款（一次性金币 / 资源割让）、战胜方战争消耗衰减。
- 结盟 / 集体防御（平等盟友，区别于附庸）。
- 现代水面舰艇（驱逐舰 / 巡洋舰）独立型号与信息化换代。
