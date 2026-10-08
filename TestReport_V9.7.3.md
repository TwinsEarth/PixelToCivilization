# 《从像素到文明》V9.7.3 测试报告

- 版本：V9.7.3「历史 Bug 全量回归复测」
- 引擎：Tuanjie 2022.3.62t12
- 测试平台：WebGL（本地 HTTP 127.0.0.1:8062，干净 origin）
- 日期：2026-10-08
- 测试方式：真实构建 + 浏览器探针（SendMessage / pxcProbe / console hook / IndexedDB 直查），非静态推断

---

## 一、测试环境

| 项 | 值 |
|---|---|
| 引擎路径 | E:\Unity\2022.3.62t12\Editor\Tuanjie.exe |
| 工程 | E:\DB\pixel_to_civilization_win\TuanjieCivilization_V9 |
| 构建入口 | PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI |
| 部署目录 | E:\DB\pixel_to_civilization_win\v973_web |
| 访问 URL | http://127.0.0.1:8062/index.html?nc=v973slot |

---

## 二、回归矩阵结果（45 项 / 9 类）

| 类别 | 项数 | PASS | 未验证 |
|---|---|---|---|
| A 时间 / 地图 | 4 | 3 | 1（A2 1000×） |
| B 资源 / 经济 | 4 | 4 | 0 |
| C 地面部队 | 7 | 7 | 0 |
| D 海军 / 船只 | 7 | 7 | 0 |
| E 存档 | 4 | 4 | 0 |
| F 集结 / 军旗 | 3 | 3 | 0 |
| G 建筑 / 塔防 | 8 | 8 | 0 |
| H 道路 / 车辆 | 5 | 5 | 0 |
| I 天气 / 环境 | 9 | 9 | 0 |
| **合计** | **45** | **43** | **2** |

明细见 `BugMatrix_V9.7.3.md`。

---

## 三、存档闭环测试用例（本轮核心）

| 编号 | 用例 | 操作 | 预期 | 结果 |
|---|---|---|---|---|
| TC-01 | 编译 | Tuanjie 命令行构建 | 无 CS 错误 | PASS（CS_ERRORS=0） |
| TC-02 | WebGL 构建 | bee_backend / IL2CPP | BUILD_SUCCESS，引擎退出 0 | PASS（BUILD_SUCCESS=1，EXIT 0） |
| TC-03 | 防裁剪 | link.xml Assembly-CSharp preserve | WebQuickSave 可达 | PASS（不再静默失败） |
| TC-04 | QuickSave 写入 | WebQuickSave → WebSaveProbe | slot1=1 | PASS（slot:010000） |
| TC-05 | IndexedDB 直查 | IDB 读 PxC_Save_1/auto | 版本号 9.7.3 | PASS（len18098/22750 ver=9.7.3） |
| TC-06 | 预加载竞态 | reload 同 origin 读 slot | 双 gate 读到槽 | PASS（slot:110000 schema:3） |
| TC-07 | QuickLoad 读档 | WebQuickLoad | QuickLoad OK | PASS（恢复后时间继续推进） |
| TC-08 | checksum 固定点 | 双精度往返 | 写入/校验串一致 | PASS（canonicalization 固定点，tmp:0 bak:0） |
| TC-09 | 六行槽 UI | 打开存档界面 | 6 行按钮全显 | PASS（槽0-5 全显，槽5 按钮完整） |
| TC-10 | 空槽数量 | 新局存档列表 | 空槽 ≥3 | PASS（新局空槽 4） |
| TC-11 | 密钥扫描 | 扫描文本资源 | 无 API Key | PASS（RESULT: CLEAN） |
| TC-12 | 打包 | Compress-Archive | zip 完整可解压 | PASS（见交付） |

---

## 四、其余 8 类无回归（抽样原始记录）

### 真实扩展（A3）
```
推进100年后 factor=1.010 activeHalf=484.8 activeWorld=968.0 activeN 240→242
```

### 资源精度（B2）
```
gold=1000000 循环16次 AddRes 0.0625 = gold=1000001.000000
```

### 潮汐不拖船（D1）
```
退潮 Day22：ships=6 ocean=6 beached=0 IN_INLAND=0
涨潮 Day8 ：ships=6 ocean=6 beached=0 IN_INLAND=0
```

### 集结不卡死（F2）
```
连插20次（navy/ground/inf 循环）：ok=20 children=4 nanRejected=True
```

### 塔防矮墩台（G2）
```
高宽比 arrow1.46 fire1.65 cannon1.29 bunker1.46 watch1.65（全部 <2）
```

---

## 五、未验证 / 限制项（如实标注）

| 项 | 状态 | 说明 |
|---|---|---|
| 1000× Debug 档加速 | 未验证 | 普通档 100× 已验证不崩溃；1000× 需 ToFuture 解锁 DebugLevel≥2，本轮未测 |
| 跨年代长跨度陆地海洋比例 | 未验证 | 初始口径符合；数千年长跨度比例漂移未测 |
| 旧 V7.0.2 存档全量迁移 | 未验证 | schema=3 迁移链就位；旧档全量迁移未完整跑（可选） |
| Win / macOS / Android / iOS 原生构建 | 未验证 | 本轮仅实际构建 WebGL；多平台构建脚本存在但未执行 |
| 5000–10000 年完整挂机 | 未验证 | 本轮为探针级回归，未做完整长时挂机 |
| 性能 Profiler 帧率 / GC 定量 | 未验证 | 本轮以功能正确性为主，未导出 Profiler 数据 |

---

## 六、测试结论

- 回归矩阵 45 项实测：43 PASS、2 未验证（均为非阻断性深度项）；
- 存档系统五项修复全部闭环，每个修复点均有真实探针 / IDB 证据；
- 其余 8 类相对 V9.7.2 无回归；
- 未验证项已如实标注，未冒充通过。

**结论：V9.7.3 历史 Bug 回归通过，具备发布条件。**
