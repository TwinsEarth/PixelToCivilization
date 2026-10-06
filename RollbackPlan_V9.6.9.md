# 从像素到文明 · 回滚方案 — V9.6.9

仓库：https://github.com/TwinsEarth/PixelToCivilization · 适用版本：V9.6.9（向前兼容 V9.6.6 存档链）
目标：任意环节异常时，5 分钟内恢复可玩状态，且不丢失玩家存档。

---

## 一、回滚总原则

1. **历史 Release 常驻**：GitHub Releases 只增不删（v9.6.6/v9.6.7/v9.6.8 均保留），任何时候可取旧包回滚。
2. **存档与版本解耦**：存档键体系独立（见下文），跨版本读档无损（schema=3，float/double 数值兼容）。
3. **发布前置闸门**：zip 必须过密钥扫描（`RESULT: CLEAN`）+ 浏览器回归 + 探针，任一不过不发。

---

## 二、四维回滚清单

### 1. 构建回滚（回到上一发布版）
| 场景 | 动作 | 说明 |
|---|---|---|
| 新构建失败/产物异常 | 直接用 GitHub Releases 中 `PixelToCivilization_V9.6.8_HTML5.zip` 部署 | 上一版 tag 未删除，一键可得 |
| 构建缓存损坏（Internal build system error） | `cmd /c rd /s /q <工程>\Library\ScriptAssemblies` + `cmd /c rd /s /q <工程>\Library\Bee\artifacts\2000b0aE.dag` 后重构建 | Bee dag 是目录，必须 `cmd /c rd` |
| 部署目录污染 | 全量解压重建（不增量覆盖），并核对 Build/ 下 data/wasm/framework/loader 四件齐全 | loader.js 为引擎通用脚本（13168B），版本无关可复用 |
| 版本回退后 IndexedDB 残留 | index.html 按 VER 自动清 IndexedDB；再不行浏览器"清除站点数据" | 防 memory access out of bounds |

### 2. 运行回滚（浏览器运行时）
| 场景 | 动作 |
|---|---|
| 运行时异常/卡死 | Ctrl+F5 强刷（index.html no-store + VER 时间戳唯一化） |
| 主档损坏（校验和不符） | 启动自动校验 → 失败自动切写前备份 `PxC_Bak_*` |
| 连续崩溃（≥2 次） | 自动安全模式：关闭 AI 联网、降粒子/特效、限制单位数量，仍可进游戏读档 |
| 手动恢复入口 | Debug 面板「恢复备份」「回滚槽 0/1/2」（WebRestoreBackup） |

### 3. 存档回滚（存档链结构）
| 层 | PlayerPrefs 键 | 作用 | 保留份数 |
|---|---|---|---|
| 主档 | `PxC_Save_auto|1..5` + `PxC_SaveSum_auto|N` | 正常存/读 | 6 槽 |
| 写前备份 | `PxC_Bak_*` + `BakSum_*` | 覆盖写档前的完整副本 | 1 |
| 回滚链 | `PxC_Roll_0/1/2` + Sum | 最近 3 次提交快照（环形） | 3 |
| 原子写 | `PxC_Tmp_*` | 写全→校验→转正；残留启动清理 | — |
| 崩溃标记 | `PxC_Crash_*` / `PxC_State` | 崩溃检测、安全模式判定 | — |

流程：`PxC_Tmp_* 写入 → FNV-1a64 校验 → 转正主档（写前先备份到 PxC_Bak_*）→ 快照推入回滚链 → 校验和落盘`。
读档失败链：主档 → 备份 → 回滚槽 → 新档（不丢玩家进度）。
跨版本兼容：schema=3；旧档 ResVals(float) 数值 JSON 反序列化无损（V9.6.8 起 double[]）。

### 4. 配置回滚（AI Key / 环境）
| 项 | 动作 |
|---|---|
| AI Key | 存浮动键 `PxC_AIKey_*`/`PxC_AIHorizon_*`，不随包分发；误填在 Debug/帮助面板清空保存即可 |
| 引擎版本 | 仓库 `Packages/manifest.json` 固化 2022.3.62t12；回退用 Hub 锁定旧版本，Library 重建、存档键不受影响 |
| 平台构建 | Win/Android/iOS 产物独立目录，互不覆盖；回归失败即切回 WebGL 主交付 |

---

## 三、回滚演练（模拟步骤，可复现）

```powershell
# 1) 构造主档损坏：浏览器 console 手动把 PxC_Save_1 改坏一个字符后刷新
# 2) 预期：自动校验失败 → 自动加载 PxC_Bak_1 → 游戏正常进入并提示"已从备份恢复"
# 3) 构造连续崩溃：两次 WebCrashSimulate(2) → 第三次启动进入安全模式（SafeMode=on，AI/粒子降级）
# 4) 手动回滚：Debug 面板 → 回滚槽 1 → 世界恢复为 3 次提交前的状态
# 5) 全链路探针断言：slot:110000|roll:111|schema:3|tmp:0|bak:1（槽位/回滚链/版本/临时清理/备份）
```

## 四、回滚判定标准

- 任一异常 5 分钟内恢复可玩 ✓
- 回滚过程不覆盖玩家现有存档 ✓
- 旧版本包与存档可继续使用（跨版本兼容）✓
- 每次发布前对回滚路径做一次探针级回归 ✓
