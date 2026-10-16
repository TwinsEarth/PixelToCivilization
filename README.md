# 从像素到文明 · Pixel to Civilization

> 一款从三皇五帝到太空时代的 **Q 版低多边形 4X 文明模拟游戏**。开局一座海边村落，看小人出生、成长、劳作、衰老；看王朝分合、群雄争霸、扬帆殖民、迈向海洋与星辰。目标是让文明在无人干预下延续五千年乃至一万年。

- **引擎**：团结引擎 Tuanjie 2022.3.61t13（基于 Unity 2022.3 LTS，Hub 版）
- **平台**：HTML5 / WebGL（主交付），同一套 C# 可构建 Win / macOS / Android / iOS
- **当前版本**：**V10.1.01「全插件化架构重写」**——极薄宿主 + 插件契约，除核心模块外所有系统/功能/副本均以插件形式重写；数据导向（SoA）存储，分主次 / 分权限 / 分布式 / 分模块 / 分版本。
- **协议**：[MIT](LICENSE)，第三方 CC0 资源见 [NOTICE](NOTICE)

---

## 一、架构总览（V10.1.01 全新）

V10 是一次**从零开始的架构重写**。旧的单体 `GameManager + 一堆 System` 被替换为「**极薄宿主 + 契约 + 插件**」模型。

### 分层依赖（自上而下，只允许上层依赖下层）

```
┌───────────────────────────────────────────────────────────┐
│  Plugins  插件层（Tier0 核心玩法 / Tier1 扩展 / Tier2 内容副本）│
│   Economy · Population · Building · Tech · GroundForce ·    │
│   Naval · Combat · Rally · Military · Smoke(样板) …         │
├───────────────────────────────────────────────────────────┤
│  Gameplay  共享玩法层（纯数据 + 服务接口 + 实体目录）          │
│   Entities(UnitRegistry/BuildingRegistry) · Factions ·      │
│   Resources · Tech · Services(各服务接口)                    │
├───────────────────────────────────────────────────────────┤
│  Host  宿主运行时（不含玩法）                                │
│   GameLoop · PluginRuntime · PluginRegistry · ServiceRegistry│
│   StrongEventBus · CapabilitySystem · PluginContext          │
├───────────────────────────────────────────────────────────┤
│  Core  内核契约（零 Unity 重型依赖，可独立编译）               │
│   Contracts · Events · Services · Persistence · Capabilities │
│   Time · World                                              │
├───────────────────────────────────────────────────────────┤
│  Infra  基础设施实现（可被宿主替换）                          │
│   Assets(Addressables/Resources) · Budget · Crash · Memory · │
│   Pool · Save · Time                                        │
└───────────────────────────────────────────────────────────┘
```

**关键设计**：
- **契约在 Core，实现在 Infra/Plugin**：内核只定义接口（`IPlugin`、`IEventBus`、`ISaveService`、`IClock`…），宿主和插件面向接口编程。
- **宿主零玩法**：`Host` 只负责装配、生命周期、服务定位、事件分发、能力闸门，不写任何文明规则。
- **数据导向（SoA）**：人口 / 单位 / 建筑等大规模模拟数据存为并行数组（`StructArray`、`UnitRegistry`、`BuildingRegistry`），而非大量 `GameObject`，保证缓存命中与长跨度性能。
- **能力闸门**：文件读写、网络、生成实体、UI、音频等敏感操作通过 `Capability` 声明，`CapabilitySystem` 统一授权，插件不能越权。

---

## 二、插件契约

每个插件实现 **`IPlugin`**（`Assets/Scripts/Core/Contracts/IPlugin.cs`）；需要参与模拟的再实现 **`ISimPlugin`**。

```csharp
public interface IPlugin {
    string Id { get; }                       // 全局唯一，如 "pxc.groundforce"
    string DisplayName { get; }
    int Version { get; }                    // 插件自身版本（整数）
    int MinHostVersion { get; }             // 所需宿主最低版本
    Tier Tier { get; }                      // Core0 / System1 / Content2 / Community3
    IReadOnlyList<PluginDependency> Dependencies { get; }   // 依赖的其他插件
    IReadOnlyList<CapabilityClaim> RequiredCapabilities { get; }
    void OnRegister(IPluginContext context);
    void OnInitialize();
    void OnShutdown();
    ISaveSegment GetSaveSegment();
    void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion);
}

public interface ISimPlugin : IPlugin {
    SimulationLayer Layer { get; }          // P0Core / P1Tactical / P2Ambient
    void OnTick(float simDt);               // 每模拟秒
    void OnYear();                          // 每年结算
    void OnEra(int newEra);                 // 跨时代
    void OnDynasty(int dynastyId);          // 朝代更替
}
```

**插件生命周期**：宿主 `Collect → BuildOrder（拓扑排序 + 依赖检查）→ PopulateNewWorld → RegisterAll → InitializeAll → StartAll`。依赖缺失 / 循环依赖 / 能力被拒时安全降级（进入安全模式），不会崩溃。

---

## 三、Tier0 核心插件清单

| 插件 ID | 模块 | 职责 |
|---|---|---|
| `pxc.economy` | **Economy** | 15 种资源（双精度累加）、年度产出/消耗、研究点供给；SoA 年度结算 |
| `pxc.population` | **Population** | 人口 SoA、出生/衰老/死亡、劳动力分配（劳动力/士兵）、饥荒与移民 |
| `pxc.building` | **Building** | 建筑目录、建筑 SoA、建造/老化、多层塔形制规格（`TowerProfile` 收分 3–4 层） |
| `pxc.tech` | **Tech** | 时代推进（进入时代 N 需 `100+current*50` 研究点，上限 Space=9）、跨时代事件 |
| `pxc.groundforce` | **GroundForce** | 6 型地面部队（骑兵/阵兵/战车 + 坦克/装甲车/导弹车），**陆地校验生成、自主寻敌-追击-攻击-回撤、不掉湖**、经验升级 |
| `pxc.naval` | **Naval** | 7 型舰船（木筏/渔船/战船/帆船/轮船/潜艇/航母），**水域内自主行动、不上岸**、集结、经验升级 |
| `pxc.combat` | **Combat** | 统一伤害公式（减伤 `def/(def+100)`、穿甲减半、同域判定）、经验/击杀结算 |
| `pxc.rally` | **Rally** | 集结令：订阅集结事件，按旗帜点水陆分流到海军/地面；**幂等序列、不瞬时搬运、不分配大数组（根治连插崩溃）** |
| `pxc.military` | **Military** | 军事聚合（总兵力/火力 = 士兵人口 + 地面 + 海军）、征兵、战争矩阵（对称） |
| `pxc.smoke` | Smoke（样板） | 最小插件示例，供新插件复制 |

> 每个插件均包含完整「**五件套**」：`Plugin`（生命周期）+ `PluginRegister`（清单/依赖/能力）+ `State`（SoA 数据）+ `Service`（服务接口实现）+ `SaveSegment`（存档段）。

---

## 四、目录结构

```
Assets/Scripts/
├─ Bootstrap/      GameBootstrap（唯一组合根，启动装配）、NewGameSetup（初始势力）
├─ Core/           内核契约
│   ├─ Contracts/   IPlugin / ISimPlugin / IPluginContext / IViewPlugin / IContentPlugin
│   │               PluginDeclarations（依赖、能力声明）、Enums（Tier/SimulationLayer）
│   ├─ Events/      IEventBus、GameEvents（各 struct 事件）
│   ├─ Services/    IServiceLocator / IClock / ISaveService / IObjectPool / IAssetLoader
│   │               IMemoryMonitor / ICrashServices / IScheduling
│   ├─ Persistence/ SaveData、ISaveSegment
│   ├─ Capabilities/ Capability（能力枚举）
│   ├─ Time/        GameClock
│   └─ World/       Chunk / ChunkedWorld / EntityId / StructArray
├─ Host/           宿主运行时（零玩法）
│   ├─ GameLoop / PluginRuntime / PluginRegistry / PluginContext
│   ├─ ServiceRegistry / StrongEventBus / CapabilitySystem
├─ Infra/          基础设施实现
│   ├─ Assets/      AddressablesAssetLoader / ResourcesAssetLoader
│   ├─ Budget/      FrameBudget / SimScheduler
│   ├─ Crash/       CrashGuard / LocalCrashReport
│   ├─ Memory/      MemoryMonitor
│   ├─ Pool/        ObjectPool
│   ├─ Save/        SaveSystem / FnvChecksum
│   └─ Time/        ClockAdapter
├─ Gameplay/       共享玩法层
│   ├─ Entities/    UnitRegistry（统一可移动单位 SoA）/ BuildingRegistry
│   ├─ Factions/    FactionRegistry（上限 8）
│   ├─ Resources/   ResourceType（含 None=-1 哨兵，15 种资源）
│   ├─ Tech/        Era（10 个时代）
│   └─ Services/    各服务接口 + IGeographyQuery（陆地/水域查询，真实地图可替换）
└─ Plugins/
    ├─ Tier0/      Economy · Population · Building · Tech · GroundForce
    │              Naval · Combat · Rally · Military · SmokePlugin
    ├─ Tier1/      （扩展插件，规划中）
    └─ Tier2/      （内容副本插件，规划中）
Assets/Tests/EditMode/   13 个 EditMode 测试 + 测试替身
Assets/Editor/           编辑器/构建脚本
Assets/Legacy_v990~/     旧 v9 脚本归档（~ 结尾，Unity 不导入、不进仓库）
```

---

## 五、快速开始

### 编辑器运行
1. 安装 **团结引擎 Tuanjie 2022.3.61t13**（或兼容 Unity 2022.3 LTS）。
2. Hub「Add project from disk」打开仓库根目录。
3. 等待首次导入，打开入口场景，点击 Play。

### 命令行一键出 WebGL 包（Windows）

```powershell
& "C:\Program Files\Tuanjie\Hub\Editor\2022.3.61t13\Editor\Tuanjie.exe" `
  -batchmode -quit -buildTarget WebGL `
  -projectPath "E:\DB\PixelToCivilization" `
  -executeMethod PxC.Editor.BuildScript.BuildWebGL `
  -logFile build.log
```

构建产物输出到工程下的 `BuildWebGL/`。发布到本地 HTTP 服务（WebGL 不能 `file://` 直接打开）：

```powershell
cd BuildWebGL
python -m http.server 8000
# 浏览器访问 http://127.0.0.1:8000/index.html
```

### 运行测试
- 编辑器：`Window → General → Test Runner → EditMode → Run All`
- 命令行：`-runTests -testPlatform EditMode -testResults results.xml`

---

## 六、验证状态（务必阅读）

| 验证项 | 方式 | 状态 |
|---|---|---|
| **C# 编译（Desktop 配置）** | Roslyn 4.8 + UnityEngine stub，定义 `UNITY_EDITOR/UNITY_STANDALONE` | ✅ 0 错误（`PxC.Game.dll`） |
| **C# 编译（WebGL 配置）** | Roslyn 4.8 + UnityEngine stub，定义 `UNITY_WEBGL` | ✅ 0 错误（`PxC.Game.WebGL.dll`） |
| **测试代码编译** | Roslyn 编译 13 个测试文件 | ✅ 0 错误（`PxC.Tests.dll`） |
| **真实 Unity 编辑器编译** | 团结编辑器导入并编译 | ⚠️ **未验证**（当前开发机无有效许可） |
| **EditMode 测试断言执行** | 在真实 NUnit 中运行 | ⚠️ **未验证**（当前仅编译；NUnit 断言为空实现 stub） |
| **PlayMode 冒烟** | 启动 / 存档读档 / 模拟循环 | ⚠️ **未验证**（PlayMode 测试尚未编写） |
| **IL2CPP / WebGL 打包** | 真实出包 | ⚠️ **未验证** |
| **三大历史 Bug 行为回归** | 集结令崩溃 / 骑兵困湖 / 塔防造型 | ⚠️ 架构层已针对性重写（见下），**浏览器实测未验证** |
| **原生平台构建** | Win / macOS / Android / iOS | ⚠️ **未验证**（同一套 C# 理论可构建） |

### 三大历史 Bug 在 V10 中的架构级处理
> 以下为代码层面的针对性设计，**浏览器实际行为回归未验证**（需真实 Unity 出包）。
1. **集结令连续插旗崩溃**：`RallyCommandPlugin` 用 `Sequence` 幂等（丢弃旧命令），只改单位的命令字段、**不瞬时搬运、不创建/销毁单位、不分配大数组**；从源头消除累积性崩溃。
2. **古典地面部队被拉到湖里**：生成时 `IGeographyQuery.IsWater` 校验拒绝水域、贴地、记录 `HomePosition` 锚点；AI 移动每一步对下一位置做水域校验，**是水则停（永不进入水域）**；不再与船舶共用牵引逻辑（`EntityDomain.Land` 与 `Naval` 分离）。
3. **塔防做成方型楼**：`TowerProfile` 明确多层收分形制（3–4 层、每层收窄约 18%、出檐外扩），与普通建筑分档。

---

## 七、回滚方案

- **Git 回滚**：V10 重写为线性提交（`24ece35(v9.9.0) → 274b68c(框架) → f0b4ed4(插件)`）。如需回退到上一稳定版：
  ```powershell
  git checkout main
  git reset --hard 24ece35        # 回到 V9.9.0
  git push --force-with-lease origin main
  ```
- **存档回滚**：存档保留 5 手动槽 + 3 回滚槽，写入走 `.tmp → 正式 → .bak`，FNV-1a 校验；新存档加载失败可回退 `.bak`。
- **插件级回滚**：插件可独立开关，Tier0 之外的插件不影响核心启动；初始化失败的插件被跳过并进入安全模式。

---

## 八、下一步（待开发 / 待裁决）

1. **Tier1 扩展插件**：地图（真实水陆 `IGeographyQuery` 实现替换占位）、天气、朝代/事件、交通、桥、科技扩展。
2. **Tier2 内容副本插件**：塔防五型视图接线、海洋/太空副本、行为动作库。
3. **PlayMode 测试**：启动、存档/读档、AI 防掉湖/不上岸、模拟循环、崩溃恢复。
4. **真实 Unity 环境**：安装团结 Hub + 编辑器并由用户 GUI 激活许可，完成真实编译 / IL2CPP / 打包 / 浏览器回归（当前为授权闸门）。
5. 玩法侧待裁决项：开局年代、势力上限、胜利方式、在线模式默认值（详见《开发计划 2.0》）。

---

## 九、版本历程（摘）

| 版本 | 主题 |
|---|---|
| V9.x | 单体架构：内存/崩溃/存档/LOD/按需加载/算力分层工程底座，智能体驱动 |
| **V10.0.2** | 极薄宿主 + 插件契约 + Roslyn 无 Unity 编译验证通道 |
| **V10.1.01** | **全插件化重写**：9 个 Tier0 核心插件（经济/人口/建筑/科技/地面/海军/战斗/集结/军事），13 个 EditMode 测试，Roslyn 双配置编译 0 错误 |
| V10.2→V10.7.8 | 31 个插件迭代版本：天气/灾害/政策/文化/历史/胜利/朝代/事件/塔防/聚落/殖民/自动发展/战争闭环/附庸纳贡/赔款没收等，每版均含 EditMode 回归 |
| **V10.8.0** | **UI 表现层恢复（可玩性修复）**：全屏文明世界地图（势力色块/聚落/建筑/单位）+ 玩家面板（人口分层/资源/军队建筑聚落统计）+ 天下势力列表 + 中文事件日志 + 速度控制（⏸/1x/5x/20x/100x）；新增 PxC.UI / PxC.Context 程序集解除依赖环；真实团结引擎编译全绿、EditMode 160/160、PlayMode 11/11、WebGL 构建完整（data 55.6MB + wasm 27.9MB） |

---

## 🎮 三通道游玩

1. **本地浏览器（推荐）**：解压 Release 的 `PTC_v10.8.0_HTML5.zip`，目录内执行 `python -m http.server 8080`，浏览器打开 `http://localhost:8080`
2. **GitHub Pages**：https://twinsearth.github.io/PixelToCivilization/ （引导页，完整在线 WebGL 因构建包 55MB 超 GitHub REST 单文件上限暂未直挂，网络恢复后随 v10.9 部署）——**在线直玩：未验证/暂缓**
3. **打包分发**：GitHub Release v10.8.0 → `PTC_v10.8.0_HTML5.zip`（53MB 可玩包）+ `PTC_v10.8.0_src.zip`（491MB 源码包）

**验证口径**：本版所有结论均基于真实团结引擎 2022.3.61t13 实测（编译/EditMode/PlayMode/WebGL 构建落盘）；浏览器端 Unity 启动加载验证通过（console 无 JS 错误）；**浏览器 UI 画面截图级视觉回归未完成**（浏览器自动化会话被 WebGL 高负载卡死，标注未验证）。

---

## 🔐 关于 AI 联网密钥

开源版本**不含任何密钥**，离线即可完整运行。在线大模型 Key 通过环境变量 `PXC_ARK_API_KEY` 或游戏内面板填写，**不随包分发、不提交进仓库**。

## 🤝 贡献

欢迎 Issue / PR：新插件、新建筑与朝代美术、平衡数值、性能优化、多平台适配。提交前请确认不含凭证、不引入非 CC0/开源协议冲突的资源。

## 📜 License

[MIT License](LICENSE) © 2026 The PixelToCivilization Authors。第三方资源署名见 [NOTICE](NOTICE)。
