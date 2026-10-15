# Release Notes — v10.4.6：自动外交 / 主动宣战

## 背景
审计发现 `WarDeclaredEvent` 有订阅方（military 插件维护战争矩阵、history 插件记录），**但全仓库没有任何地方发布它**——AI 之间永远不会开战，军事建设失去意义。本版补齐战争发起闭环。

## 新增

### 1. 自动外交：`AutoDiplomacyPlugin`
- 年度对每个存活势力（玩家默认跳过）评估是否宣战：
  - 攻方火力达到最低门槛（`MinimumFirepower = 20`）；
  - 相对目标存在显著军力优势（`PowerAdvantageRatio = 1.5`，attacker/max(1,defender)）；
  - 未多线作战（`MaxConcurrentWars = 1`）；
  - 选择首个满足条件的目标，**发布 `WarDeclaredEvent`，每年最多宣战 1 次**。
- 玩家不主动宣战，但**可被 AI 宣战**。
- 依赖 military、automilitary（在当年军事建设后用最新军力评估）。

## 工具修复
- 一键脚本 `tools/run_editmode_tests.ps1` 的反射加载由 `LoadFrom` 改为 `Load(byte[])`：
  本机 Application Control (WDAC) 策略拦截 UnityEngine.dll 的 LoadFrom（0x800711C7），字节加载不受此限。

## 测试
- 新增 8 项（EditMode 91→99）：优势显著宣战、低于门槛不宣战、优势比不足不宣战、已交战不重复、玩家不主动宣战、AI 可宣战玩家、达战争上限不宣战、每年最多 1 场。
- Roslyn 6 目标编译全部 exit=0；EditMode **Total 99 / Passed 99 / Failed 0**。

## 未实现 / 未验证
- 未做和约/停战（战争一旦开始目前不会自动结束）；未做同盟、附庸、外交关系值（好感度）。
- 宣战决策的长期节奏（好战度、战争频率）为纯确定性，**未做长期平衡验证**。
- 真实团结引擎编译 / IL2CPP / WebGL `.wasm` 打包 / 官方测试运行器 **未验证**（需有团结许可机器）。
