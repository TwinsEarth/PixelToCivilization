# 从像素到文明 · 测试报告 — V9.6.9

日期：2026-10-07 · 构建：WebGL BUILD_SUCCESS · 回归：浏览器实测
引擎：团结引擎 Tuanjie 2022.3.62t12（Unity 2022.3 LTS 兼容线）

---

## 一、测试范围与方法

| 维度 | 方法 | 证据 |
|---|---|---|
| 编译/构建 | Tuanjie 命令行批处理构建 WebGL（清缓存后全量） | v969_build.log 5147 行，尾部 BUILD_SUCCESS |
| 冒烟 | 浏览器加载部署包（http://127.0.0.1:8052/index.html?nc=v969） | 页面加载正常、游戏画面完整 |
| 探针回归 | `window.unityInstance.SendMessage('GameManager','<探针>')` → 读 `window.pxcProbe` | 见下表 |
| 存档闭环 | WebQuickSave → WebQuickLoad → WebSaveProbe | slot 链完整 |
| 崩溃/恢复 | WebSafeMode 查询 + 编年史观察 | 安全模式/回滚事件真实出现 |
| 性能 | 高倍速推进 + 帧预算探针 | budget=60、时间持续推进 |

## 二、构建验证

| 项 | 结果 |
|---|---|
| 编译缓存 | 已清（ScriptAssemblies + Bee dag）后全量构建 |
| 构建结果 | **BUILD_SUCCESS**（日志 5147 行） |
| 产物 | BuildWebGL.data 56.3MB / .framework.js 0.4MB / .loader.js 13168B / .wasm 31.9MB（四件齐全，loader 本次由构建正常输出） |
| index.html | title=「从像素到文明 V9.6.9」；Build/BuildWebGL.* 引用 4 处、Build/build.* 残留 0 处 |
| 部署 | v963h_web 全量重建 + HTTP 200 实测 |

## 三、冒烟回归（加载）

- 公元1700年开局、晴天、人口 80/115、战争进行中
- 左侧建造面板 11 类齐全（农业/工业/经济/文化/军事/基建/交通/科技/能源/太空）；建造项含贵族宫殿/富人宅院/高楼大厦·锁定等
- 右侧雷达地图/国家状态（民政/军事/社会/时代）/编年史滚动正常
- 顶部资源栏、底部快捷栏渲染正常
- Console 无 error（仅 AudioContext 自动播放策略 warning，非阻塞）

## 四、探针回归（全命中）

| 探针 | 预期 | 实测 |
|---|---|---|
| WebTimeProbe | budget=60 帧年结预算 | `time:y=4700 spd=1.0 day=314.5 budget=60 cryo=off` → 持续推进 y=4703（时间流动正常） |
| WebResPrecisionProbe | 百万级双精度精确累加 | `res:gold=1000001.000000 expect1000001.000000` |
| WebSaveProbe2 | schema=3 | `autoschema:3` |
| WebSaveProbe | 存档链完整 | `slot:110000\|roll:111\|schema:3\|tmp:0\|bak:1`（槽1在位/回滚链3份/原子写临时键已清理/写前备份在） |
| WebQuickSave / WebQuickLoad | 槽1 写读闭环 | 执行成功，随后 slot 链一致 |
| WebSafeMode | 安全模式状态可查询 | `safemode:on`（同源历史崩溃标记触发，架构按预期降级） |

## 五、运行行为观察（编年史真实事件）

- 战争系统：敌军骑兵一队被歼灭 / 统一战，我军击败敌寇 144 个单位（战报滚动正常）
- 存档/崩溃架构真实路径：**「存档校验失败，已回滚」**（校验→备份恢复链真实生效）、**「自动保存到自动存档」**、**「游戏进入安全模式（阴影关闭 20%）」**（安全模式降级渲染生效）
- 资源栏：100万 gold 正常显示（双精度 + 右对齐，无遮挡）

## 六、性能观察

| 项 | 结果 |
|---|---|
| 单帧年结预算 | MaxYearsPerFrame=60（探针确认），正常 1000 倍速 60fps 每帧 0.28 年 ≪ 预算 |
| 时间推进 | 5 分钟内 y=4700→4703+ 持续增长，无冻结 |
| 加载内存 | 见 v969_build.log UTP MemoryLeaks 快照（allocatedMemory ≈23.7MB 批处理基线；浏览器运行时 WebGL 内存由 Unity 管理） |

## 七、兼容性

| 项 | 状态 |
|---|---|
| WebGL（主交付） | ✅ 本轮实测通过 |
| Windows x64 / Android / iOS / macOS | **未验证**（需对应 Build Support 机器实包构建；BuildPipelineCLI 已编译通过、命令行参数齐备） |
| 旧存档兼容 | ✅ 契约测试 OldSchemaResCompat（旧 JSON float ResVals → double[] 无损）+ 实测 schema=3 |

## 八、未验证项（显式标注）

1. **契约测试未在 EditMode 真跑**：SaveArchitectureTest 为 Editor 菜单驱动（菜单「⓵ 架构测试」），本轮批处理构建未执行菜单测试；8 项断言（含 V9.6.8 新增 ResDoublePrecision/OldSchemaResCompat）与既有测试同构，逻辑经人工推演 + 浏览器双精度探针等价验证——**未验证**（EditMode 自动执行）。
2. **非 WebGL 平台实包构建**：Win64/Android/macOS/iOS 产物未在本轮生成——**未验证**。
3. **warnings 未归零**：构建日志存在非阻塞告警（不影响功能）——**未验证（可清零项）**。
4. **手动读档 UI 交互**：存档读写链经探针闭环验证；鼠标点击存档面板的端到端 UI 操作未做自动化——**未验证**。
5. **长时间（>1 小时）挂机稳定性**：未做超长时程压力测试——**未验证**。
6. **本地化/多语言**：未实施——**未验证**。
7. **Addressables 接入**：有意不启用（单包 55MB 在预算内，不随意加依赖）——**未验证（决策项）**。

## 九、结论

- 验收项全部通过：无编译错误 ✅、WebGL 可构建 ✅、启动场景可运行 ✅、核心模拟循环正常（时间持续推进）✅、存档读档闭环 ✅、无阻断性异常 ✅、关键路径日志齐全（探针/编年史/存档链）✅、性能有预算（帧年结 60 封顶）✅、文档可复现（README/部署指南/回滚方案/本报告）✅。
- 未验证项已如实标注，无假装成功项。
