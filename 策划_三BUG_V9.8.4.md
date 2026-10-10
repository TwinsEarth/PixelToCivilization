# 《从像素到文明》V9.8.4 三 BUG 真根治策划（Pro 定位 / Lit 修复分工）

> 版本：V9.8.4 ｜ 分工：Pro（2.1Pro 类）发现定位 Bug；Lit（2.1lite 类）修复·测试·验证·回归·发布

## 一、BUG-1 集结令连插 5 次 Chrome 必崩 —— 真根因：UGUI 动态 UI 网格顶点爆炸

### 现象
- 用户连续插旗 5 次，Chrome 渲染进程弹窗卡死。
- V9.8.2 曾归因 speechSynthesis 高频 cancel/speak（已改静音 + SilentCommand + 节流），仍复现。

### Pro 定位（浏览器 console 决定性证据）
V9.8.4 真实 UI 复现落旗时 console 给出：
```
ArgumentException: Mesh can not have more than 65000 vertices
at UnityEngine.UI.VertexHelper.FillMesh
```
+ CrashGuard 摘要 `ctx=dyn=dyn12|y=4744|pop=160|fac=1|era=4|spd=1.0`

**结论：UGUI 动态 UI 网格顶点爆炸才是 Chrome 渲染进程崩溃真根因。** 插旗链每次落旗 → 事件入列 → 编年史/日志/Ticker/Toast 全部在**同一个动态 UI Canvas** 上重建文本网格 → 高频操作下顶点累积超过 65000 → Unity WebGL 异常 → Chrome 渲染进程卡死。

### 治本方案（4 处）
| 落点 | 修改 |
|---|---|
| `GameManager.cs` AddEvent（L1848） | 事件文本 **80 字截断**（防单条事件无限膨胀） |
| `UIManager.cs` Build | **TickerCanvas / ToastCanvas / MarkerCanvas 三独立 Canvas**（互不重建；每 Canvas 只承载少量元素，顶点不累积） |
| `UIManager.cs` RefreshEventLog | 编年史保留条数 **40 → 25**（重建顶点量固定上限） |
| `UIManager.Overhaul.cs` L861 日志面板 | 日志保留 **200 → 120** |

（V9.8.3 的语音静音/节流方案保留作为辅助护栏，但不再是主因修法。）

## 二、BUG-2 古典地面部队（骑兵/阵兵/战车）生成后困湖不自主战斗

### 现象
- 1949 前古典部队生成后被拉到某坐标不动，找不到、无法操作、不索敌不战斗。
- V9.7.2 重写 GroundWarfareSystem 后仍复现（用户第 6 次投诉）。

### Pro 定位（源码取证）
`GroundWarfareSystem.NearestLand` 半径上限 **48 格**；主大陆大湖泊中心距岸 > 48 → 返回 null → 水域脱困失效 → 部队困在湖心。且困水分支用 `continue` 跳过本帧索敌 → 「困湖 + 不自主战斗」同源。

### 治本方案
| 修改 | 内容 |
|---|---|
| `GroundWarfareSystem.cs` NearestLand | 半径上限 **48 → 240 格**，三档螺旋步进：r≤10 八方向、r≤48 十六方向步进 2、r>48 二十四方向步进 4（大湖中心也能找到岸） |
| 困水脱困 | 脱困移动速度 **1.0 → 2.0**（快速上岸） |
| 困水分支 | **去掉 continue**（上岸后立即进入索敌/组队/战斗逻辑，不再整帧跳过） |

## 三、BUG-3 塔防（箭塔/火塔/炮塔/碉堡/烽火台）仍是方型楼而非多层塔

### 现象
- 五型塔「变矮了」但仍是方型楼；用户要求「塔不是楼，参考树木 3-5 层，每层比下一层小」。

### Pro 定位（源码取证，最关键）
`BuildingMeshFactory.Special.cs` 的 `BuildSpecial` switch **此前完全没有** arrow_tower / fire_tower / cannon_tower / bunker / watchtower 分支 —— 五型塔全部落入通用建筑主体（仅 wallH×2.4 方盒加高）—— **「多层收分塔」从未真正实现过**。

### 治本方案
新增 `BuildTower`（真正实现）：
- **3-4 层收分塔身**：火塔 4 层、其余 3 层；底层 3.0、层高 1.45、逐层收窄 18%、层间出檐外扩 14%+0.12、塔门。
- **五型专属顶部**：
  - 箭塔 = 弩机 Deck + BowBody + BowL/R + BowString
  - 火塔 = 黑口 Mouth + 常燃 Fireball
  - 炮塔 = TurretBase + Turret + Barrel 旋转炮
  - 碉堡 = 半球 Dome + 四向 Gun×4
  - 烽火台 = Deck + CradleL/R + Cauldron + Flame
- `BuildSpecial` 命中塔分支**提前 return true**（不走方盒主墙/平顶/AddCategoryFeatures）。
