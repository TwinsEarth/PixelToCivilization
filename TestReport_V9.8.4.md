# V9.8.4 测试报告（TestReport）

> 版本：V9.8.4 ｜ 构建：v984b BUILD_SUCCESS ｜ 回归：浏览器 bu 平面 HTTP 8070

## 1. 测试范围

| 类别 | 覆盖 |
|---|---|
| BUG-1 集结令 | 插旗/撤销循环压力（5/20 次）、连续随机插旗 20 次、UI 顶点爆炸检测、页面存活 |
| BUG-2 古典地面部队 | 公元 1700 古典时代生成、部队类型/位置/速度、困水计数、敌方镜像 |
| BUG-3 塔防造型 | 五型建塔、包围盒 h/w、收分层与专属部件 |
| 游戏主循环 | 时间推进、资源、人口、天气、朝代、编年史、战斗战报 |
| 版本号 | WebGLBuilder + index.html 标题 |

## 2. 测试环境

- 编辑器：`C:\Program Files\Tuanjie\Hub\Editor\2022.3.61t13\Editor\Tuanjie.exe`（唯一可用）
- 运行：`python -m http.server 8070 --directory PxC982\BuildWebGL` → `http://127.0.0.1:8070/index.html`
- 浏览器：bu 平面（1280×960 视口，canvas 全屏）

## 3. 测试用例与结果

| 用例 | 操作 | 结果 | 判定 |
|---|---|---|---|
| TC-01 版本号 | 读页面标题 | 「从像素到文明 V9.8.4 · HTML5 网页版 · 九神AI·存档架构·多槽位/校验/原子写/备份/异步」 | ✅ |
| TC-02 游戏启动 | 打开 index.html | 公元 1700 开局，正常渲染 | ✅ |
| TC-03 时间推进 | 观察年份 | 1700→1701→1702→1703 连续推进；天气/潮汐/洋流正常 | ✅ |
| TC-04 BUG-1 插旗循环 | SendMessage `WebRallyUiCycle(5)` | `cycle:5\|ok=1\|clear=2\|children=0\|rally:none` | ✅ |
| TC-04b BUG-1 插旗循环 20 | SendMessage `WebRallyUiCycle(20)` | `cycle:20\|ok=1\|clear=10\|children=0\|rally:none` | ✅ |
| TC-04c BUG-1 随机插旗 20 | SendMessage `WebRallyStress` | `rally:navy\|65,-12\|ok=20\|children=4\|nanRejected=True` | ✅ |
| TC-04d 顶点爆炸检测 | 过滤 console | **无 65000 vertices 错误**；页面存活（title 可读、游戏推进） | ✅ |
| TC-05 BUG-2 古典生成 | 新局 1700 + `WebBuildClassicGround` | `v963 G[phalanx:(-62.5,-91.6)spd1.20; chariot:(-56.0,-82.2)spd2.80; chariot:(-63.6,-65.4)spd2.80]` | ✅ |
| TC-05b BUG-2 困水计数 | 读 `WebV962Probe` | `ground(ours=3 enemy=3 factions=3) stuck:0 inWater:0`（不困湖） | ✅ |
| TC-05c BUG-2 敌方镜像 | 同上 | 敌方 3 队自动响应（enemy=3 factions=3） | ✅ |
| TC-06 BUG-3 建塔 | SendMessage `WebBuildTowers` | `towers:5/5`（箭/火/炮/碉堡/烽火台） | ✅ |
| TC-06b BUG-3 塔型比例 | SendMessage `WebTowerBounds` | arrow 1.46 / fire 1.96 / cannon 1.41 / bunker 1.39 / watchtower 1.55（多层收分塔，非方盒 2.4） | ✅ |
| TC-06c BUG-3 结构部件 | 读 bounds 明细 | 五型含 Tier1-4+Eave 收分层 + BowBod/BowL/BowR/BowStr·Mouth/Fireba·Turret/Barrel·Dome/Gun×4·Cradle/Cauldr/Flame | ✅ |
| TC-07 战斗系统 | 观察战报 | 敌军持续被歼灭、防御/火力变化、编年史战报滚动 | ✅ |

## 4. 探针协议（供复测）

```
window.unityInstance.SendMessage('GameManager','WebRallyUiCycle', n)    // 集结令插旗/撤销循环 n 次
window.unityInstance.SendMessage('GameManager','WebRallyStress')        // 连续随机插旗 20 次
window.unityInstance.SendMessage('GameManager','WebNewClassic')         // 重开公元1700经典新局
window.unityInstance.SendMessage('GameManager','WebBuildClassicGround') // 古典部队 ×3（不跳年）
window.unityInstance.SendMessage('GameManager','WebV963Probe')          // 地面部队位置/速度/道路/车辆
window.unityInstance.SendMessage('GameManager','WebV962Probe')          // 地面部队/空投/集结状态
window.unityInstance.SendMessage('GameManager','WebBuildTowers')        // 建 5 类塔防
window.unityInstance.SendMessage('GameManager','WebTowerBounds')        // 塔包围盒 h/w + 部件明细
```
结果经 `Application.ExternalEval` 写回 `window.pxcProbe`；也可读 console `[WEB]/[Web]` 前缀日志。

## 5. 结论

- **三 BUG 全部真修复并通过浏览器实证**：集结令插旗 UI 链（落旗→事件→编年史→三 Canvas 重建）5/20 次循环 + 20 次随机插旗无 65000 顶点错误、页面存活；古典部队（阵兵/战车）在公元 1700 真实坐标、有速度、stuck:0、inWater:0、敌方自动镜像；塔防为 3-4 层收分塔（h/w 1.39-1.96，五型专属部件在册）。
- 游戏主循环、战斗、版本号均正常。
- 未验证：用户 Chrome 实机复核、底部按钮真实坐标点击（探针同路径已覆盖）、原生平台构建（见闭环文档 §八）。
