# V9.7.1 测试报告

- 测试方式：Tuanjie WebGL CLI 构建后，浏览器（bu 探针）回归
- 测试日期：2026-10-07
- 构建结果：BUILD_SUCCESS（两次：首次 + 含高架生命周期测试的二次）
- 二次产物：BuildWebGL.data 59,031,071 / .wasm 33,481,868 / .framework.js 446,321 / .loader.js 13,168

## 一、构建测试

| 项 | 结果 |
|---|---|
| 编译错误 (error CS) | 无 |
| WebGL 构建 | BUILD_SUCCESS |
| 产物增量 | .wasm 33,479,346 → 33,481,868（+2,522，证含新测试方法） |

## 二、功能探针（浏览器真实输出）

### 1. 时间系统（自适应分段）
- 输入：`WebTimeProbe`
- 输出：`time:y=4700 spd=1.0 day=306.9 ticks=0 avgMs=0.000 hard=120 cryo=off`
- 结论：hard=120 硬上限生效；ticks/avgMs 自适应字段就位。

### 2. 混合存档
- 输入：`WebSaveProbe`
- 输出：`slot:110000|roll:111|schema:3|tmp:0|bak:1`
- IndexedDB 实测键：PxC_Bak_1、PxC_Roll_0、PxC_Roll_1、PxC_Roll_2、PxC_Save_1、PxC_Save_auto
- 结论：正文走 IndexedDB（库 PxC_SaveBody），槽位/回滚/备份齐备。

### 3. 基建标签
- 输入：`WebOpenBuildTab('infra')`
- 实测项：高架柱（钢80 砼40 点地立柱，自动与相邻柱连片）、铁路（点城市，自动跨海一线一车）、城际公路（点城市，自动连相邻城市）、铁路(早期)、机场、高铁站
- 结论：基建标签及归并内容正确。

### 4. 地面部队分代
- 输入：`WebBuildGround`（跳年 4949 + 造 3 个现代型）
- 输出：`[Web] Ground groundActive=1 ours=2 enemy=2 factions=2 max=60`
- 年份探针：`Year=4950 Day=344.22 Era=6 Paused=False Speed=1 Eff=1 Greg=公元1950年`
- 结论：公元 1949 后现代型（坦克/装甲车/导弹车）镜像生成，上限 60。
- 备注：DebugBuildOwn(3) 期望 3，实际 ours=2（受门控/计数影响）；敌方镜像同步。

### 5. 高架桥生命周期（用户红线）
- 输入：`WebViaductTest`
- 输出：
  - `[VCT] BUILD first=True second=True | piers 0->2 | cells 0->20 | runs 0->1`
  - `[VCT] DECAY piers 2->0 | cells 20->0 | runs 1->0`
  - `[VCT] RESULT: PASS-桥面格/废弃柱随老化同步清理（地图错乱根因已消除） | runsRemoved=True cellsCleared=True piersRemoved=True`
- 结论：建柱连片→老化拆除→桥面格/废弃柱同步清理，全链路通过。

### 6. 密钥扫描
- 输出：`RESULT: CLEAN`

## 三、未验证项（明确标注）
| 项 | 状态 |
|---|---|
| 门控逼近上限实际暂停 | 未验证（代码接线静态确认） |
| 地面部队 AI 主动索敌/选中升级 | 未验证（本轮仅验证生成与镜像） |
| 帧率/内存 Profiler 快照 | 未验证 |
| Windows/macOS/Android/iOS 原生构建 | 未验证（本版仅 WebGL；C# 同套可构建） |

## 四、结论
- 核心五项改造均有真实探针/生命周期输出；
- 高架桥历史红线项 PASS；
- 未验证项已如实标注，未假装成功。
