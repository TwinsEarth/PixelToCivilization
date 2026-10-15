# 《从像素到文明》PixelToCivilization
# v2.0 插件化架构开发计划（含契约/宿主/清单/路线）

> 文档版本：**v2.0-PLAN**（2026-10-15）
> 取代：v10.1-PLAN《插件化架构开发计划》
> 配套：《PxC_v2.0_设计指南与策划书》
> 本文档是插件化重写的**工程施工说明书**：给出目标目录、程序集、契约 API、宿主内核、能力模型、插件清单、版本路线、任务分解与验收。

---

## 一、架构总览

### 1.1 目标形态

```
┌──────────────────────────────────────────────────────────────────┐
│  Bootstrap（仅启动接线）：发现/装配插件、进入主循环，唯一允许装配一切 │
├──────────────────────────────────────────────────────────────────┤
│  插件层（Plugins）：Tier-0 核心 / Tier-1 系统 / Tier-2 内容 / Tier-3 社区 │
│   每个插件：独立 asmdef + 独立目录 + 能力申请 + 存档段 + 可独立测试     │
├──────────────────────────────────────────────────────────────────┤
│  宿主内核（Host Kernel，约 20% 代码，不含业务）：                   │
│   PluginRuntime · GameLoop · StrongEventBus · ServiceRegistry      │
│   CapabilitySystem · World(Chunked SoA) · PluginContext            │
├──────────────────────────────────────────────────────────────────┤
│  内置框架服务（Built-in Plugins，宿主级，可替换）：                  │
│   ObjectPool · AddressablesLoader · MemoryMonitor · CrashGuard      │
│   SaveSystem · FrameBudget · SimScheduler                          │
└──────────────────────────────────────────────────────────────────┘
```

### 1.2 五条依赖方向铁律

1. `Bootstrap → Host → Core`；插件 → `{Core, Host 接口}`；**禁止 Core/Host 反向依赖任何插件**。
2. 插件之间**禁止直接引用**（Tier-0 之间也不直接 `using` 对方具体类），只通过事件总线、服务契约、数据世界通信。
3. 插件只能访问 `IPluginContext` 暴露的 API，不直接 new 宿主内核。
4. 表现层只读数据世界、订阅事件，不持有模拟状态。
5. 每个插件一个 asmdef，`Allow Unsafe` 默认关、仅必要时显式开启。

---

## 二、程序集与目录结构

### 2.1 程序集（asmdef）

| 程序集 | 引用 | 内容 |
|---|---|---|
| `PxC.Core` | 无（仅 UnityEngine/数学） | 契约接口、World/Chunk/SoA、时间、事件结构体、能力枚举、DTO、数学 |
| `PxC.Host` | PxC.Core | PluginRuntime、GameLoop、StrongEventBus、ServiceRegistry、CapabilitySystem、PluginContext |
| `PxC.Infra` | PxC.Core, PxC.Host | 内置框架服务：对象池、Addressables 加载、内存监控、崩溃守护、存档、帧预算/算力 |
| `PxC.Plugin.X` | PxC.Core（必要时 PxC.Host 抽象） | 每个业务插件一个 asmdef（不引用其他插件） |
| `PxC.Bootstrap` | Core, Host, Infra | 启动装配（唯一接线层） |
| `PxC.Editor` | 全部（Editor only） | 构建、扫描、工具、插件清单生成 |
| `PxC.Tests` | 全部（Tests only） | EditMode/PlayMode |

### 2.2 目标目录

```
Assets/
├── Scripts/
│   ├── Core/                    # PxC.Core（契约 + 数据世界 + 核心服务接口）
│   │   ├── Contracts/           # IPlugin / ISimPlugin / IViewPlugin / IContentPlugin / IPluginContext
│   │   ├── World/               # ChunkedWorld, Chunk, SoA arrays, EntityId
│   │   ├── Time/                # GameClock, TickBudget
│   │   ├── Events/              # 强类型事件结构体
│   │   ├── Capabilities/        # Capability 枚举/声明
│   │   ├── Persistence/         # SaveData DTO, ISaveSegment, IMigration
│   │   └── PxC.Core.asmdef
│   ├── Host/                    # PxC.Host（内核，无业务）
│   │   ├── PluginRuntime.cs  GameLoop.cs  StrongEventBus.cs
│   │   ├── ServiceRegistry.cs  CapabilitySystem.cs  PluginContext.cs
│   │   └── PxC.Host.asmdef
│   ├── Infra/                   # PxC.Infra（内置框架服务）
│   │   ├── ObjectPool/  Addressables/  Memory/  Crash/  Save/  Budget/
│   │   └── PxC.Infra.asmdef
│   ├── Bootstrap/              # PxC.Bootstrap（启动接线）
│   │   └── GameBootstrap.cs  PxC.Bootstrap.asmdef
│   └── Plugins/                # 全部业务插件（每个一个目录 + asmdef）
│       ├── Tier0/  economy/ population/ building/ tech/ military/ naval/ groundforce/ combat/
│       ├── Tier1/  rally/ weather/ timehistory/ mapgen/ nation/ culture/ wonder/
│       │           city/ traffic/ colonization/ expedition/ expansion/
│       │           agent/ council/ disaster/ broadcast/ airlift/
│       └── Tier2/  towerdef/ era-preqin/ era-han/ ... scene-nanyang/ scene-mars/ ...
├── Editor/                     # PxC.Editor
├── Tests/                      # PxC.Tests
├── Data/                       # ScriptableObject 配置（配置化）
├── Prefabs/                    # 预制体（配置化）
├── AddressablesData/           # Addressables 分组/内容
└── Plugins~ (Tier3 远程包目录，符号/本地缓存)
```

---

## 三、插件契约（代码级 API）

> 下列为契约接口定义；开发阶段以完整文件落地（不省略）。

### 3.1 基础契约 IPlugin

```csharp
public interface IPlugin
{
    // 身份
    string Id { get; }                 // 唯一 id，如 "pxc.economy"
    string DisplayName { get; }        // 显示名
    int Version { get; }               // 插件自身版本（兼容/存档段用）
    int MinHostVersion { get; }        // 所需宿主最低版本

    // 依赖与能力
    IReadOnlyList<PluginDependency> Dependencies { get; }   // 依赖其他插件 id + 版本
    IReadOnlyList<CapabilityClaim> RequiredCapabilities { get; } // 能力申请
    Tier Tier { get; }                 // 0/1/2/3

    // 生命周期
    void OnRegister(IPluginContext ctx);   // 注册（拿到受限上下文）
    void OnInitialize();                   // 初始化（分配/订阅/读配置）
    void OnShutdown();                     // 卸载（反订阅/释放/清空，必须与 Init 镜像）

    // 存档段（可返回 null=无存档）
    ISaveSegment GetSaveSegment();
    void RestoreSaveSegment(ISaveSegment segment, int saveVersion);
}

public sealed class PluginDependency
{
    public string Id; public int MinVersion; public bool Optional;
}
public sealed class CapabilityClaim
{
    public Capability Capability; public string Scope; // Scope 如域名/资源路径
}
```

### 3.2 模拟契约 ISimPlugin（可由同一类实现）

```csharp
public interface ISimPlugin : IPlugin
{
    SimulationLayer Layer { get; }       // P0Core / P1Tactical / P2Ambient
    void OnTick(float simDt);            // 每帧（已乘游戏速度）
    void OnYear(int gameYear);           // 每个游戏年
    void OnEra(int newEra, int oldEra);  // 时代切换
    void OnDynasty(int idx, int year);   // 朝代更迭
}
```

### 3.3 表现契约 IViewPlugin

```csharp
public interface IViewPlugin : IPlugin
{
    void OnViewReady();                  // 视图层就绪
    void OnViewTick(float realDt);       // 视图刷新（只读数据）
    void OnViewRelease();                // 视图回收（镜像创建，对象池/Release）
}
```

### 3.4 内容契约 IContentPlugin

```csharp
public interface IContentPlugin : IPlugin
{
    IReadOnlyList<string> DefinitionAddresses { get; } // SO/Addressables 地址
    IReadOnlyList<string> ContentGroups { get; }       // Addressables 分组
    void OnContentLoaded();
}
```

### 3.5 插件生命周期状态机

```
Discovered → Loaded(asmdef/清单加载) → Registered(OnRegister)
   → Initialized(OnInitialize) → Started(可 Tick)
   → Paused ↔ Started
   → Stopped → Deinitialized(OnShutdown) → Unloaded
```

- 每次状态迁移由 PluginRuntime 驱动；异常迁移失败 → 进入 `Faulted`，按故障升级策略处置。
- 热插拔（桌面/编辑器）：`Unload → 重新 Load`；已注册的存档段/事件订阅必须在 OnShutdown 完全清掉（Diff 归零）。

---

## 四、宿主内核（Host Kernel）

### 4.1 PluginRuntime（插件运行时）
- 发现：扫描 asmdef 插件 + 数据清单 + 远程地址（Tier-3 需授权）。
- 解析依赖：构建 DAG，**Kahn 拓扑排序**；检测环（环=加载失败并明确报错，不启动）。
- 按序执行生命周期；维护每插件状态机与故障计数。
- 提供 `Enable/Disable/Reload(id)`；卸载时调用 OnShutdown 并验证订阅/句柄已清。

### 4.2 GameLoop（极薄主循环）
- 只驱动：按算力优先级调度 `ISimPlugin.OnTick`（sim 时间）与 `IViewPlugin.OnViewTick`（真实时间）。
- 帧末统一 flush StrongEventBus；执行存档写队列（每帧 ≤1）；更新内存/帧预算探针。
- 不含任何业务规则。

### 4.3 StrongEventBus（强类型事件总线）
- 事件为 `struct`（强类型，按类型订阅），杜绝字符串弱类型与每事件 GC。
- 高频事件进**环形缓冲**，帧末统一派发；订阅/反订阅必须镜像。
- 关键事件：`YearAdvanced`、`EraChanged`、`DynastyChanged`、`EntitySpawned/Died`、`WarDeclared`、`ProjectileFired/Hit`、`ChunkLoaded/Unloaded`、`SaveRequested`、`RallyOrdered`。

### 4.4 ServiceRegistry（服务契约注册）
- 服务以接口注册（如 `IObjectPool`、`IAssetLoader`、`ISaveService`、`ICrashReport`、`IClock`）。
- 插件按需查询服务，不使用静态单例；内置服务可被替换（测试替身）。

### 4.5 CapabilitySystem（能力授权）
- **零默认原则**：插件启动时无任何资源/功能能力，全部按 `RequiredCapabilities` 申请。
- Tier-0/1 内置能力在构建期清单授权；Tier-2 随内容；Tier-3 首次加载前**弹窗展示能力清单+来源**，用户确认才授予。
- 运行时调用敏感 API 前校验：未授权 → 拒绝并记录（安全失败，不崩溃）。
- 能力分类见 §六。

### 4.6 IPluginContext（受限上下文）
- 向插件暴露：事件发布/订阅、服务查询、数据世界访问（按段）、时间、日志、能力校验、资源加载句柄登记。
- 不暴露内核对象创建/反射加载等危险能力（除非显式授权且仅桌面）。

---

## 五、数据块世界（Chunked SoA World）

### 5.1 世界结构
- 世界 = 若干 `Chunk`（按坐标分块），每 Chunk 持有 SoA 纯数据数组；只有**活跃 Chunk** 驻留。
- 实体 = `EntityId`（稳定句柄：chunk + index + generation），运行时不 new class。
- 组件按列连续存储（如 PopulationSoA：`int[] Count; float[] Food; ...`），逻辑批量遍历。

### 5.2 实体分类表（唯一权威，根治"骑兵当船"）
| 类别 | 成员 | 移动域 | 禁止逻辑 |
|---|---|---|---|
| Land 陆地 | 人员、骑兵、阵兵、战车、坦克、装甲车、导弹车、车辆 | 陆地 | 禁止水流/锚点漂流/船舶牵引 |
| Naval 船舶 | 小船/帆船/渔船/战船/轮船/航母/潜艇 | 水域 | 巡游锚点只对本类生效 |
| Air 空中 | 飞机、舰载机、导弹、运输机 | 空中 | — |
| Static 静态 | 建筑、塔防、桥、奇观、树木、资源点 | 固定 | — |

- 生成系统按类别选择出生域与移动逻辑；陆地实体永不生成/迁移到水域。

### 5.3 数据上限（沿用并在 SoA 容量中实现）
- 人口 5000 / 建筑 800 / 地面部队 120 / 船舶 200 / 车辆 120 / 鸟 500 / 鱼 300 / 势力 8；
- 逼近 90% 暂停生成（裁决：暂停而非继续），高优先销毁/回收才允许补充。

---

## 六、能力权限模型（Capability）

| 类别 | 能力项 | 默认 |
|---|---|---|
| 资源 | FileSystem.Read / FileSystem.Write、IndexedDB、Clipboard、Download | 内置插件按需；社区弹窗 |
| 网络 | Network（Scope=白名单域名）、CloudSync | 离线模式无；在线显式 |
| 功能 | SpawnEntity、CreateProjectile、UI、Audio、TimeControl、Save | 对应插件按需 |
| 系统 | AssemblyLoad（仅桌面）、Process、Unsafe | **默认全禁** |
- 校验：注册时申请 → 启动时授予 → 运行时 API 入口校验；越权 = 拒绝 + 记录 + 计数。
- 密钥：网络密钥只在运行时输入/本地环境，**不进清单、不进存档、不进包**。

---

## 七、内置框架服务（Built-in Plugins）

| 服务 | 接口 | 职责 |
|---|---|---|
| 对象池 | `IObjectPool` | GameObject/UI/投射物/数据对象池；Rent/Return/Stats |
| 资源加载 | `IAssetLoader` | Addressables 真加载，handle 登记，**Release 强制配对** |
| 内存监控 | `IMemoryMonitor` | 三级水位、快照/Diff、生成闸门、LiveCount |
| 崩溃守护 | `ICrashGuard` | 三路全局捕获、分层处置、本地崩溃快照、恢复 |
| 崩溃上报 | `ICrashReport` | 本地实现；预留 Crashlytics 适配（未引入） |
| 存档 | `ISaveService` | DTO/迁移/原子写/校验/备份/多槽/异步/分块 |
| 帧预算/算力 | `IFrameBudget`/`ISimScheduler` | 帧预算、P0/P1/P2 分层、画质自适应 |

> 内置服务与业务插件走同一契约与生命周期，宿主不写死其业务规则，可被测试替身替换。

---

## 八、插件清单（Tier 分层）

### 8.1 Tier-0 核心（8 个，编译期，必选，缺一即非本游戏）

| 插件 id | 职责 | 关键输出 |
|---|---|---|
| pxc.economy | 15 资源、产出/消耗、市场 | 年度结算、资源数组 |
| pxc.population | 人口分层（势力/年龄/职业）、出生死亡、周期律 | 人口 SoA |
| pxc.building | 建造/升级/拆除、建筑形制、建筑数据 | 建筑实体、视图事件 |
| pxc.tech | 科技树、解锁、研究 | 科技状态 |
| pxc.military | 军事总控、攻防、兵种 | 军事命令 |
| pxc.naval | 海军/船舶、编队、航母/潜艇 | 海军实体 |
| pxc.groundforce | 地面部队两时代、10 维参数、自主 AI | 地面部队实体 |
| pxc.combat | 统一战斗结算、经验升级 | 伤害/经验事件 |

### 8.2 Tier-1 系统（编译期，可关，默认开）

| 插件 id | 职责 |
|---|---|
| pxc.rally | 集结令（幂等/限频/队列/安全失败） |
| pxc.weather | 天气（雨/雪/风、时令） |
| pxc.timehistory | 时间记录与历史事件 |
| pxc.mapgen | 地图/地形生成（含分块） |
| pxc.nation | 势力/国家/地球国家 |
| pxc.culture | 文化/哲学 |
| pxc.wonder | 世界奇观 |
| pxc.city | 城市公共设施/指标/财政 |
| pxc.traffic | 车辆/火车/桥/路/城际/高架 |
| pxc.colonization | 殖民/海外据点 |
| pxc.expedition | 海图/星图探险副本 |
| pxc.expansion | 世界/海洋/太空扩张、运河 |
| pxc.agent | 智能体身份/驱动/三模式行为库 |
| pxc.council | 九神议会 SafetyNet（防灭绝） |
| pxc.disaster | 灾害/潮汐/洋流 |
| pxc.broadcast | 语音广播 |
| pxc.airlift | 空运投送/直升机 |

### 8.3 Tier-2 内容/副本（数据驱动 + 编译期资产）

- **塔防内容包**：箭塔/火塔/炮塔/碉堡/烽火台定义（多层收分形制 + 攻击特效）。
- **朝代历史包**：先秦/秦汉/隋唐/宋元/明清/近现代/未来（人物、单位、建筑、事件、政策）。
- **场景副本**：南洋、红海、火星、月球、岛屿等（独立地址/分组）。
- **美术包**：单位/建筑 authored LOD、肖像、音效。

### 8.4 Tier-3 社区（远程，地址加载 + 能力授权 + 白名单）
- 来源：地址/仓库；首次加载弹能力清单；未授权能力拒绝；运行在同一隔离契约下。

---

## 九、版本路线与任务分解

> 版本自动推进（用户偏好），无需逐版确认，直到资源/授权闸门。

| 版本 | 主题 | 交付 | 门槛 |
|---|---|---|---|
| **v10.0.2** | 宿主框架 | 程序集/目录、契约、World/Chunk/SoA、PluginRuntime/GameLoop/EventBus/Registry/Capability/Context、内置服务骨架、最小冒烟 | 编译通过（待许可）、最小可加载 |
| **v10.0.3** | Tier-0 八插件 | economy/population/building/tech/military/naval/groundforce/combat 全量迁移 | 核心 4X 循环可玩 |
| **v10.0.4** | Tier-1 系统插件 | rally/weather/mapgen/nation/culture/wonder/city/traffic/colonization/expedition/expansion/agent/council/disaster/broadcast/airlift | 系统可独立开关 |
| **v10.0.5** | 内容与美术 | 塔防五型真形制、朝代包、authored LOD、低多边形统一 | 形制验收通过 |
| **v10.0.6** | 智能体真驱动 | 身份→目标→行为→命令，与 SoA 联动，三模式行为库 | 全实体自主行为 |
| **v10.0.7** | 测试与 CI | EditMode/PlayMode、隔离/热插拔/内存/崩溃/存档测试、GitHub Actions | 自动化回归通过 |
| **v10.0.8** | 打磨与发布 | 文档、回滚、最终构建、多平台、发布 | DoD 全部达成 |

### 9.1 v10.0.2 任务分解（施工顺序）
1. 建目录与全部 asmdef（Core/Host/Infra/Bootstrap/Tests/Editor）。
2. Core：契约接口、能力枚举、事件结构体、EntityId、World/Chunk、SoA 基类、DTO/存档段接口、Clock。
3. Host：StrongEventBus、ServiceRegistry、CapabilitySystem、PluginContext、PluginRuntime（DAG/拓扑/状态机）、GameLoop。
4. Infra：ObjectPool、AddressablesLoader、MemoryMonitor、CrashGuard、SaveSystem、FrameBudget/SimScheduler。
5. Bootstrap：发现→拓扑→初始化→启动→主循环；最小冒烟插件。
6. Editor：依赖环扫描、能力清单、地址配对静态扫描、构建脚本。
7. Tests：拓扑/能力/存档/对象池/热插拔/隔离的最小测试。

---

## 十、隔离与故障升级

- 单插件 Tick 异常 → 本帧捕获、计故障、隔离该插件（不影响其他）；
- 同插件连续 5 次同类异常 → 自动停用（P1 升级）；
- 宿主内核级异常（P0）→ 安全模式：写崩溃快照、停业务、保留存档、重启可恢复。
- 第三方插件任何越权/未声明行为 → 立即停用并提示。

---

## 十一、测试与验收

| 类型 | 关键用例 |
|---|---|
| EditMode | 依赖拓扑（含环检测）、能力申请/越权拒绝、存档段序列化/迁移、对象池 Rent/Return、SoA 一致性、数值公式 |
| PlayMode | 启动装配、最小冒烟、逐插件开关、热插拔（插拔 100 次）、热加载 50 次句柄归零 |
| 隔离 | 单插件抛异常全局不崩、越权被拒、第三方清单弹窗 |
| 内存 | 连续插旗 50 次 Diff 归零、长跑 1000 年增幅 <20%、LiveCount 归零 |
| 崩溃 | 模拟崩溃→重启→恢复/回滚槽 |
| 长跨度 | 跳年至游戏年 13000，无溢出/灭绝/爆表 |

- 发布前统一闭环：版本号（构建脚本 + ProjectSettings）→ 批处理构建 → 扫 `error CS` → 起 http → 探针 + 目视（截图）→ zip → 闭环报告 → commit/tag/push → Release（附 zip）→ 密钥扫描。
- 无团结许可前，编译/构建/运行/回归结论一律「未验证」。

---

## 十二、回滚方案

- 每个版本打 tag；构建失败/回归不过 → 回退上一个 tag（保留可玩基线）。
- 存档：3 回滚槽 + `.bak`；迁移失败自动回滚并提示。
- 插件：停用问题插件（单插件回滚），不影响主程序；Tier-3 直接卸载。
- 旧 v9.9 完整保留为基线分支，不删除。

---

## 十三、授权闸门（必须用户行动）

1. 团结引擎许可：需在本机安装团结 Hub/编辑器并 GUI 登录激活（我无法代做登录/授权）。
2. Tier-3 社区插件首次加载的能力确认（运行时弹窗）。
3. 远程发布（GitHub Release）按用户确认执行。

> 在许可激活前，我将先完成全部可离线编写的代码与文档（宿主框架 + 契约 + 数据世界 + 内置服务），并对编译/运行类结论统一标注「未验证」。
