# 从像素到文明 · 回滚方案 — V9.7.1

目标：V9.7.1 构建/部署/运行出现问题时，快速恢复到可用状态，且不丢玩家存档。

---

## 一、构建回滚

| 场景 | 动作 | 目标 |
|---|---|---|
| V9.7.1 构建 BUILD_FAILED / 产物异常 | 用上一版包 `PixelToCivilization_V9.7.0_HTML5.zip` 重新部署（GitHub Releases 常驻，历史 tag 不删） | 线上立即恢复可用包 |
| Bee 增量 dag 损坏 | `cmd /c rd /s /q <工程>\Library\ScriptAssemblies` + 删除对应 Library/Bee dag 目录后重构建 | 干净增量 |
| 新增 .cs/.jslib 未被拾取（CS0103/CS0234） | 删新文件 .meta + 清 ScriptAssemblies/Bee + 杀残留 Tuanjie 进程后重构建 | 强制重导入 |
| 部署目录污染 | 用上一版 zip 全量解压重建部署目录，不增量覆盖 | 杜绝旧文件残留 |

---

## 二、运行回滚（浏览器）

| 场景 | 动作 |
|---|---|
| 运行时异常/卡死 | 强制刷新 Ctrl+F5 |
| 浏览器加载旧版本 | 旧 python http.server 占用端口：`Get-CimInstance Win32_Process -Filter "Name='python.exe'"` 查 --directory，杀旧重启；再 Ctrl+F5 |
| 连续崩溃 | CrashGuard 自动安全模式（SafeMode）：关闭 AI 联网、限制粒子/特效，仍可读档继续 |
| 高倍速卡死 | 本版自适应分段 + 硬上限 120 已缓解；若复现，降倍速后强刷 |

---

## 三、存档回滚（混合存档，V9.7.1 重点）

V9.7.1 存档正文在 IndexedDB（库 PxC_SaveBody），摘要在 PlayerPrefs。

### 1. 存档链层级

| 层 | 正文键（IndexedDB） | 摘要键（PlayerPrefs） | 用途 |
|---|---|---|---|
| 主档 | PxC_Save_auto / PxC_Save_1..5 | PxC_SaveSum_* | 正常存档/读档 |
| 写前备份 | PxC_Bak_1..5 | PxC_BakSum_* | 覆盖写前完整副本 |
| 回滚链 | PxC_Roll_0/1/2 | PxC_RollSum_* | 最近 3 次提交快照（环形） |
| 原子写 | PxC_Tmp_* | — | 临时键，校验通过才转正；残留启动清理 |

### 2. 回滚操作

- 自动存档损坏：CrashGuard 启动检测 → 自动切换备份 PxC_Bak_*；
  手动可在 Debug 面板执行「恢复备份」（WebRestoreBackup）。
- Debug 面板「回滚槽 0/1/2」读取 PxC_Roll_*。
- 原子写中断：.tmp 未转正时主档不受影响，启动 CleanupTempKeys 清残留。

### 3. IndexedDB 迁移失败/异常（V9.7.1 新增）

- 迁移逻辑 `MigrateLegacyFromPlayerPrefs`：仅当旧 PlayerPrefs 正文存在且 IndexedDB 无对应正文时才复制，
  复制成功后才删旧键——任何一步失败旧键保留，可重试。
- 若 IndexedDB 正文整体损坏：
  1. 浏览器 F12 → Application → IndexedDB → 删除 `PxC_SaveBody` 库；
  2. 重新加载会重建空库，再从 PlayerPrefs 旧键（若未迁移）重新迁移，或用备份/回滚槽；
  3. 极端情况清站点数据后，用 Release 上一版 zip 部署。

### 4. 版本回退（V9.7.1 → V9.7.0）

- 存档 schema=3 为 V9.6.x 起沿用，V9.7.0 与 V9.7.1 同 schema（仅正文承载位置变化）。
- 回退到 V9.7.0 时：V9.7.0 读 PlayerPrefs 正文，而 V9.7.1 已把正文迁到 IndexedDB 并删除 PlayerPrefs 正文。
  因此**回退版本前**：先在 V9.7.1 存档界面手动导出/备份，或保留 IndexedDB 中正文（V9.7.0 不会自动读 IndexedDB）。
  建议：版本回退仅用于程序回滚，存档优先在同版本内使用。

---

## 四、配置回滚（AI Key 等）

- AI Key 存浮动键 `PxC_AIKey_*` / `PxC_AIHorizon_*`（PlayerPrefs），不随包分发；误填清空即可。
- 引擎/模块版本：`Packages/manifest.json` 固化；回退引擎用 Hub 锁旧版本打开（Library 重建，存档不受影响）。

---

## 五、回滚后验收

- 页面 title 与加载页版本正确（注意端口旧服务器问题）；
- 可新游戏、可推进时间、可存档读档；
- IndexedDB 库 PxC_SaveBody 正常生成、正文键可读写；
- 无阻断性异常；关键路径有日志。
