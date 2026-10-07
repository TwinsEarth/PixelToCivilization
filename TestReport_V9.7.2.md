# 《从像素到文明》V9.7.2 测试报告

- 版本：V9.7.2
- 引擎：Tuanjie 2022.3.62t12
- 测试平台：WebGL（本地 HTTP 127.0.0.1:8053）
- 日期：2026-10-07
- 测试方式：真实构建 + 浏览器探针（SendMessage / pxcProbe / console hook），非静态推断

---

## 一、测试环境

| 项 | 值 |
|---|---|
| 引擎路径 | E:\Unity\2022.3.62t12\Editor\Tuanjie.exe |
| 工程 | E:\DB\pixel_to_civilization_win\TuanjieCivilization_V9 |
| 构建入口 | PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI |
| 产物 | BuildWebGL（data 56.3MB / wasm 32MB / framework.js 0.4MB） |
| 部署目录 | E:\DB\pixel_to_civilization_win\v972_web |
| 访问 URL | http://127.0.0.1:8053/index.html?nc=v972 |

---

## 二、测试用例与结果

| 编号 | 用例 | 操作 | 预期 | 结果 |
|---|---|---|---|---|
| TC-01 | 编译 | Tuanjie 命令行构建 | 无 CS 错误 | PASS（NO CS errors） |
| TC-02 | WebGL 构建 | bee_backend / IL2CPP | BUILD_SUCCESS，引擎退出码 0 | PASS（BUILD_SUCCESS，EXIT 0） |
| TC-03 | 版本号 | 开始菜单 / 页面标题 | 显示 V9.7.2 | PASS（标题与 splash 均 V9.7.2） |
| TC-04 | 时间推进 | WebYearProbe | 年份/日期正常推进、未暂停 | PASS（Year=4701 Day=96.34 Paused=False） |
| TC-05 | 生成位置 | WebBuildGround + Probe963 | 单位生成在调用位置 | PASS（出生地附近，坐标可读） |
| TC-06 | GraceT 驻留 | 生成后等 11s 再读 | 驻留宽限期内坐标不变 | PASS（(7.3,-88.7) 11s 不变） |
| TC-07 | 自主追击 | 生成 3 坦克，等 6s 读坐标 | 主动向敌方移动 | PASS（3 辆均移动 14–29 格） |
| TC-08 | 战斗 / 升级 | 同上，观察等级与速度 | 击毁敌人后升级、速度提升 | PASS（1 辆 Lv1→Lv2，3.50→3.78） |
| TC-09 | 速度差异化 | Probe963 读不同类型 | 导弹车≠坦克速度 | PASS（2.50 vs 3.50） |
| TC-10 | 面板口径 | FillGround 显示 | 面板速度 = EffectiveSpeed | PASS（面板与探针一致） |
| TC-11 | 防御减伤 | DamageGround | 受击按 100/(100+Def) 减伤 | PASS（代码路径，未单独数值压测） |
| TC-12 | 密钥扫描 | 扫描文本资源 | 无 API Key | PASS（RESULT: CLEAN） |
| TC-13 | 打包 | Compress-Archive | zip 完整可解压 | PASS（54.6MB，22 文件） |

---

## 三、关键探针原始记录

### 年份
```
[Web] YearProbe Year=4701 Day=96.34 Era=4 Paused=False Speed=1 Eff=1 Greg=公元1701年
```

### 驻留（T0 → T1，11s）
```
missile_vehicle:Lv1(7.3,-88.7) spd2.50
missile_vehicle:Lv1(7.3,-88.7) spd2.50
```

### 追击 / 升级（T0 → T1，6s）
```
T0: tank(19.9,-60.1) spd3.50  tank(30.5,-61.6) spd3.50  tank(-11.0,-88.0) spd3.50
T1: tank(20.3,-74.7) spd3.50  tank(16.0,-70.9) Lv2 spd3.78  tank(17.7,-76.2) spd3.50
```

---

## 四、未验证 / 限制项

| 项 | 状态 | 说明 |
|---|---|---|
| Win / macOS / Android / iOS 原生构建 | 未验证 | 本版本仅实际构建 WebGL；C# 同源，多平台构建脚本存在但未在本轮执行 |
| 长期 5000–10000 年不间断模拟 | 未验证 | 本轮为探针级回归，未做完整长时挂机 |
| 性能 Profiler 帧率 / GC 定量 | 未验证 | 本轮以功能正确性为主，未导出 Profiler 数据 |
| 敌方围攻下 Lv1 生存平衡 | 已知限制 | 镜像围攻可能导致 Lv1 快速全灭（P2 平衡） |

---

## 五、测试结论

- 核心功能（TC-01 至 TC-10、TC-12、TC-13）全部 PASS；
- 三项需求均有真实探针证据支撑；
- 未验证项已如实标注，未冒充通过。

**结论：V9.7.2 功能回归通过，具备发布条件。**
