# 《从像素到文明》V9.8.0 回滚方案

- 版本：V9.8.0「智能体驱动 · 万物协作」
- 上一稳定版本：V9.7.3
- 日期：2026-10-08

---

## 一、回滚触发条件

出现以下任一情况且热修无法快速解决时，回滚：
1. V9.8.0 构建后无法启动 / 白屏 / 崩溃；
2. 智能体调度、消息总线异常影响主循环（卡顿、阻断）；
3. 存档、时间、战斗等核心链路出现阻断性回归；
4. 严重性能或资源异常。

---

## 二、回滚目标（已验证可用）

- **V9.7.3 Release**：
  https://github.com/TwinsEarth/PixelToCivilization/releases/tag/v9.7.3
- 对应 zip：`PixelToCivilization_V9.7.3_HTML5.zip`（约 54.6 MB，密钥扫描 CLEAN）
- 该版本构建成功、45 项回归通过，为回滚基线。

---

## 三、回滚操作（Release 维度）

### 方式 A：保留 V9.8.0，将 V9.7.3 标记为最新（推荐，可追溯）
1. GitHub Release 中编辑 **V9.7.3**，确认其为完整 Release；
2. 在 V9.8.0 Release 标题前缀加 `[YANKED]` 并在 notes 顶部说明回滚原因；
3. 更新 README 的"最新版本"指引指向 V9.7.3；
4. 本地服务器 / 部署目录替换为 V9.7.3 内容。

### 方式 B：删除 V9.8.0 Release
1. 删除 V9.8.0 Release 及上传资产（可保留或删除 tag）；
2. 玩家重新下载 V9.7.3；
3. 若代码已合入 main，通过 revert 对应提交恢复。

---

## 四、回滚操作（本地 / 服务器）

```powershell
# 停止当前 HTTP 服务
Get-CimInstance Win32_Process -Filter "Name='python.exe'" |
  Where-Object { $_.CommandLine -like '*http.server*' } |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force }

# 解压并部署 V9.7.3
$dst="E:\DB\pixel_to_civilization_win\rollback_v973"
Expand-Archive "E:\DB\pixel_to_civilization_win\PixelToCivilization_V9.7.3_HTML5.zip" -DestinationPath $dst -Force
python -m http.server 8000 --directory $dst
```

---

## 五、存档兼容性

- V9.8.0 与 V9.7.3 存档载体一致（PlayerPrefs 摘要 + IndexedDB 正文），存档 Schema/Version 兼容。
- V9.8.0 新增的智能体数据（行为库、模式）存于**独立 PlayerPrefs 键**（`PXC_AgentLib_v980` / `PXC_AgentMode_v980`），不影响存档正文；回滚后这些键为孤儿键，不影响 V9.7.3 运行。
- 身份层为非侵入新增，不修改既有 SaveData 结构；回滚不损坏任何旧存档。

---

## 六、回滚后验证

1. 版本显示为 V9.7.3；
2. 可新游戏、时间推进、存档 / 读档；
3. 既有玩法（V9.8.0 仅为非侵入身份层，未改玩法）正常。

---

## 七、本次回滚需求状态

- V9.8.0 经真实构建与浏览器探针回归，**12 个核心用例全部 PASS、118 身份全装载**，身份层异常全隔离、不影响主循环，当前**无需回滚**；
- 本方案作为预案保留。
