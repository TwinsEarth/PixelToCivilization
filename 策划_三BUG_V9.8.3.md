# 《从像素到文明》V9.8.3 策划：三 BUG 根治方案

> 版本：V9.8.3 ｜ 三 BUG 连续 5 次复现（V9.7.3 / V9.8.0 / V9.8.1 / V9.8.2）后的根治策划
> 用户铁律：**"绝对不能像上次那样糊弄其实根本没有修改"**——本轮以「根因 → 完整修改 → 浏览器实证 → 打包发布」真闭环交付。

---

## 一、BUG-1：集结令连续插旗 5 次 Chrome 必崩

### 现象
- 玩家在底部点「紧急集结令」切旗种 → 落旗，连续插旗 5 次，Chrome 渲染进程崩溃，页面弹窗卡死。
- 用户附真实运行日志：`?v=9.8.2-005604`（正式包版本号），崩溃表现为 `RuntimeError: null function or function signature mismatch` 类 wasm 异常。

### 根因（本轮最终锁定）
1. **speechSynthesis 高频 cancel/speak 是已知 Chrome 渲染进程崩溃源**。V9.8.1/9.8.2 虽加了 C# 侧节流，但每次插旗仍调用 `speechSynthesis.cancel()+speak()`，Chrome 下连续 5 次即可触发进程级崩溃。
2. **bu 浏览器（非 Chrome 内核）无法复现** → 早期回归"5 次存活"与用户 Chrome 崩溃的矛盾由此解释。
3. 语音广播（战时传令兵）属于表现层，**不应阻塞插旗主链路**。

### 根治方案（三重隔离）
1. `WarBroadcastSystem.cs`：新增 `public static bool VoiceEnabled=false`（WebGL 默认静音，注释说明原因）；新增 `SilentCommand(text)`（只写编年史/横幅，不 Speak）；`Speak()` 的 WebGL 分支首行 `if(!VoiceEnabled) return;`。
2. `RallySystem.cs`：`SetRally/ClearRally` 内所有 `GM.War.Command(...)` 改为 `GM.War.SilentCommand(...)`——插旗链路完全不触碰 speechSynthesis。
3. JS 侧双保险（index.html 模板内 `pxcSpeak`）：2.5s 节流 + 140 字截断 + try/catch。

### 验收
- 浏览器探针 `WebRallyUiCycle(10)`（10 次插旗/撤销循环，超过用户报告的 5 次）无异常、页面存活、语音桥零调用。

---

## 二、BUG-2：1949 年前古典地面部队（骑兵/列方阵兵/马拉战车）困湖不战斗

### 现象
- 古典部队生成后被困在水中/湖里，不动、不寻敌、不组队、不战斗；无法点击属性、无法升级。
- 用户附图：部队图标浮在湖面上（V9.8.1）。

### 根因
1. **生成坐标校正只做了「贴水偏移」，没有「遇水迁移」**：生成点若落在水域边缘或水内，部队原地滞留。
2. **移动逻辑无水域脱困兜底**：索敌/追击只在陆地寻路，水中的单位没有"返回陆地"指令 → 永远卡死。
3. 古典三型（骑兵/阵兵/战车）与敌方阵营的自主战斗链在生成时未闭环。

### 根治方案
`GroundWarfareSystem.cs` 新增三重兜底：
1. `Vector2? NearestLand(float x,float z)`：半径 2→48 逐环、8/16 方向螺旋扫 `_terrain.IsStaticLand`，返回最近陆地坐标。
2. `MoveToward`：贴岸 30-150° 五档转向全失败后，强向 `NearestLand` 前进；找不到才 `HasRoute=false`。
3. `UpdateOurs/UpdateEnemies` 每帧开头：`if(!_terrain.IsStaticLand(u.X,u.Z))` → `NearestLand` 强制 `MoveToward` 并 `continue`。
4. 三处生成点贴水校正（四方向 ±3 单位全水 → NearestLand 迁移）：`BuildGround` / `DebugBuildOwn` / `SpawnEnemy`。

### 验收
- `WebBuildClassicGround` 生成 3 队古典部队（公元 1712，<1949）；敌方 3 阵营响应生成；战报持续；`WebV963Speed` 位移采样证明部队在移动（非困湖）。

---

## 三、BUG-3：箭塔/火塔/炮塔/碉堡/烽火台仍是"方型楼"

### 现象
- 塔防建筑虽然变矮，仍是方型楼体；用户原话：**"我要的是塔不是楼，特别是别建成高楼，参考树木 3-5 层、每层比下一层小"**。

### 根因
1. `BuildingMeshFactory` 塔类墙高倍率与楼混用（曾 1.55，仍偏楼）。
2. 塔身是"方盒堆叠"而非"多层收分"（每层渐小 + 层檐出挑）。

### 根治方案
1. 塔类 `wallH` 倍率 **1.55 → 2.4**（低于楼 3.2，确保塔≠楼）。
2. `AddMilitary` 五型强收分多层重写：
   - **箭塔**：三层 1.0→0.72→0.52，层檐出挑 16%，顶部弩机+弩弦+粗弩箭+铁簇头（`BowArm/BowTip/BowStr/Bolt/BoltHe`）。
   - **火塔**：四层 1.0→0.76→0.58→0.42，锥形收尖顶+喷火口+常驻火球（`Nozzle/FireMo`）。
   - **炮塔**：三层 1.0→0.74→0.54，顶部旋转圆台炮座+长炮管+炮闩+炮弹球（`Turret/GunBre/ShellL/ShellR`）。
   - **碉堡**：三层环墙 0.96→0.74→0.54 + 圆顶 + 四向机枪/射孔（`RingL1-3/Dome/MG0-3/MGEmbr`）。
   - **烽火台**：木架台+双层横木+大锅+常驻烽火（`Timber×7/Cauldr/Beacon`）。

### 验收
- `WebBuildTowers`：5/5 塔在册；`WebTowerBounds`：5 塔包围盒 h/w=2.38（楼≥3.2，塔型成立）；`WebTowerProbe`：五型均含多层收分结构与专属部件。

---

## 四、版本与发布口径
- 版本号统一 V9.8.3（ProjectSettings ×2 + WebGLBuilder.cs 12 处 + index.html 标题）。
- 发布通道沿用 V9.8.2：`git credential fill` 取 PAT → REST 创建 Release + 上传 zip。
- 密钥纪律：源码+产物扫描 0 命中；API Key 绝不随包分发，游戏内九神面板自行填写。
