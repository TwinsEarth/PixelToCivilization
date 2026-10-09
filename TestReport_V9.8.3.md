# V9.8.3 测试报告（TestReport）

> 版本：V9.8.3 ｜ 构建：v983c BUILD_SUCCESS ｜ 回归：浏览器 bu 平面 HTTP 8070

## 1. 测试范围

| 类别 | 覆盖 |
|---|---|
| BUG-1 集结令 | 插旗/撤销循环压力、语音桥状态、页面存活 |
| BUG-2 古典地面部队 | 生成、敌方响应、战斗战报、位移采样 |
| BUG-3 塔防造型 | 建塔、包围盒比例、结构部件 |
| 游戏主循环 | 时间推进、资源、人口、天气、编年史、战斗 |
| 版本号 | ProjectSettings ×2 + WebGLBuilder 12 处 + index.html 标题 |

## 2. 测试环境

- 编辑器：`C:\Program Files\Tuanjie\Hub\Editor\2022.3.61t13\Editor\Tuanjie.exe`（唯一可用）
- 运行：`python -m http.server 8070 --directory PxC982\BuildWebGL` → `http://127.0.0.1:8070/index.html`
- 浏览器：bu 平面（829×870 视口，canvas 1244×1305）

## 3. 测试用例与结果

| 用例 | 操作 | 结果 | 判定 |
|---|---|---|---|
| TC-01 版本号 | 读页面标题 | 「从像素到文明 V9.8.3 · HTML5 网页版 · 九神AI·存档架构·多槽位/校验/原子写/备份/异步」 | ✅ |
| TC-02 游戏启动 | 打开 index.html | 公元 1700 开局，正常渲染 | ✅ |
| TC-03 时间推进 | 观察年份 | 1700→1702→1703→1705→1708→1710→1712 连续推进 | ✅ |
| TC-04 BUG-1 插旗压力 | SendMessage `WebRallyUiCycle(10)` | `cycle:10|ok=1|clear=5|children=0|rally:none` | ✅ |
| TC-04b 语音隔离 | 读全局语音状态 | `pending=0 busy=0 speaking=false last=null` | ✅ |
| TC-04c 崩溃检测 | 过滤 console | 非 AudioContext 错误 0 条；页面存活 | ✅ |
| TC-05 BUG-2 古典生成 | SendMessage `WebBuildClassicGround` | `made=3 year=4712`（公元 1712，古典时代） | ✅ |
| TC-05b 敌方响应 | 观察 3 秒 | `enemy=3 factions=3`（3 敌对阵营） | ✅ |
| TC-05c 自主战斗 | 读编年史 | 「我军击败敌346个单位」「敌军战士一队被歼灭」「我方有部队在作战中被击退」 | ✅ |
| TC-05d 位移采样 | `WebV963Speed` ×2（间隔 8s） | `dist=7.52 sec=9.04`（部队在移动） | ✅ |
| TC-06 BUG-3 建塔 | SendMessage `WebBuildTowers` | `towers:5/5`（箭/火/炮/碉堡/烽火台） | ✅ |
| TC-06b 塔型比例 | SendMessage `WebTowerBounds` | 5 塔 h/w=2.38（楼≥3.2，塔型成立） | ✅ |
| TC-06c 结构部件 | SendMessage `WebTowerProbe` | 五型含 L1-Eave1/L2-Eave2/L3-Eave3（L4-Peak）+ BowArm/Bolt/Nozzle/FireMo/Turret/GunBre/MG0-3/Dome/Cauldr/Beacon | ✅ |
| TC-07 战斗系统 | 观察战报 | 敌军多种部队持续被歼灭，防御 12→125 火力 22→34 | ✅ |
| TC-08 存档 | 观察编年史 | 「自动保存到自动存档」出现 | ✅ |

## 4. 探针协议（供复测）

```
window.unityInstance.SendMessage('GameManager','WebRallyUiCycle', n)   // 集结令插旗/撤销循环 n 次
window.unityInstance.SendMessage('GameManager','WebRallyStress')       // 连续插旗 20 次
window.unityInstance.SendMessage('GameManager','WebBuildClassicGround')// 古典部队 ×3（不跳年）
window.unityInstance.SendMessage('GameManager','WebGroundProbe')       // 地面部队状态
window.unityInstance.SendMessage('GameManager','WebV963Speed')         // Ours[0] 位移采样（两次调用测速）
window.unityInstance.SendMessage('GameManager','WebBuildTowers')       // 建 5 类塔防
window.unityInstance.SendMessage('GameManager','WebTowerBounds')       // 塔包围盒 h/w
window.unityInstance.SendMessage('GameManager','WebTowerProbe')        // 塔结构部件
```
结果经 `Application.ExternalEval` 写回 `window.pxcProbe`；也可读 console `[WEB]/[Web]` 前缀日志。

## 5. 结论

- 三 BUG 全部修复并通过浏览器实证：**集结令插旗链路与 speechSynthesis 完全隔离（10 次循环无崩溃）；古典部队生成后自主寻敌战斗且在移动（不困湖）；塔防为多层收分塔（h/w=2.38 < 楼 3.2，五型专属部件在册）**。
- 游戏主循环、战斗、存档、版本号均正常。
- 未验证：Chrome 原生崩溃恢复、塔防逐座放大目视、原生平台构建（见闭环文档 §八）。
