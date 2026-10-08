# V9.8.1 TestReport

## 测试范围
V9.8.1 三 BUG 加固版回归（集结令 / 古典地面部队 / 塔防造型）。

## 环境
- 构建：团结引擎 2022.3.62t12 WebGL（IL2CPP），exit=0
- 运行：python http.server 8064（v981_web），product 浏览器 plane=bu
- 加载：25 秒，公元 1700 年开局正常

## 结果矩阵

| # | 用例 | 探针/方法 | 预期 | 实测 | 状态 |
|---|---|---|---|---|---|
| 1 | 集结令连插 20 次不崩 | WebRallyStress（20 次 SetRally + NaN 攻击） | 不崩溃、旗模型复用、NaN 拒绝 | ok=20, children=4, nanRejected=True | PASS |
| 2 | 三色旗传令广播 | 编年史 OCR | 蓝/绿/红三色"传令：…集结" | 蓝旗-海军、绿旗-地面部队、红旗-军人集结广播齐全 | PASS |
| 3 | 古典部队生成 | WebBuildClassicGround + WebV962Probe | ours=3 | ours=3 enemy=3 factions=3 | PASS |
| 4 | 古典部队自主移动 | WebV963Probe ×2（间隔 8s） | 坐标变化 | phalanx (8.8,-103.7)→(19.4,-95.8)，chariot 同步移动 | PASS |
| 5 | 古典部队自主战斗/阵亡 | 编年史 OCR + 最终 WebV962Probe | 战斗发生 | "敌军兵队一队被歼灭！"，ours 3→0（参战阵亡） | PASS |
| 6 | 塔防矮墩比例 | WebTowerBounds | h/w≈1.2 非方楼 | arrow_tower h/w=1.24（9.8.0 为 1.46）含弩机零件 | PASS |
| 7 | 构建产物完整性 | 文件清单 | 22 文件 WebGL 可跑 | 22 文件 88.9MB，index.html 含 V9.8.1 | PASS |
| 8 | 密钥不随包 | 正则扫描 | 0 命中 | CLEAN（0 命中） | PASS |

## 未验证项
- 其余四塔 bounds 未逐一取证（共用 wallH 系数，比例随代码确定）。
- 存档/读档在本版本未做专项回归（9.8.0 已覆盖且无改动）。
- 地面部队升级/战功晋升路径未在本版本重跑（9.8.0 已有"战功晋升：马拉战车→Lv2"铁证，9.8.1 无相关改动）。
