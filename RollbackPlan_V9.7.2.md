# 《从像素到文明》V9.7.2 回滚方案

- 版本：V9.7.2
- 上一稳定版本：V9.7.1
- 日期：2026-10-07

---

## 一、回滚触发条件

出现以下任一情况且热修无法快速解决时，回滚：
1. V9.7.2 构建后无法启动 / 白屏 / 崩溃；
2. 地面部队、存档、战斗等核心链路出现阻断性回归；
3. 关键性能或资源异常严重影响正常游玩。

---

## 二、回滚目标（已验证可用）

- **V9.7.1 Release**：
  https://github.com/TwinsEarth/PixelToCivilization/releases/tag/v9.7.1
- 对应 zip：`PixelToCivilization_V9.7.1_HTML5.zip`（55.2 MB，密钥扫描 CLEAN）
- 该版本构建成功、回归通过，为回滚基线。

---

## 三、回滚操作（Release 维度）

### 方式 A：保留 V9.7.2，将 V9.7.1 标记为最新（推荐，可追溯）
1. GitHub Release 中编辑 **V9.7.1**，确认其为完整 Release；
2. 在 V9.7.2 Release 标题前缀加 `[YANKED]` 并在 notes 顶部说明回滚原因；
3. 必要时更新 README 的"最新版本"指引指向 V9.7.1；
4. 本地服务器 / 部署目录替换为 V9.7.1 内容。

### 方式 B：删除 V9.7.2 Release
1. 删除 V9.7.2 Release 及其上传资产（不删除 git tag 可保留历史，或一并删除 tag）；
2. 玩家重新下载 V9.7.1 即可；
3. 若代码已合入 main，通过 revert 对应提交恢复代码状态。

---

## 四、回滚操作（本地 / 服务器）

```powershell
# 停止当前 HTTP 服务
Get-CimInstance Win32_Process -Filter "Name='python.exe'" |
  Where-Object { $_.CommandLine -like '*http.server*' } |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force }

# 解压并部署 V9.7.1
$dst="E:\DB\pixel_to_civilization_win\rollback_v971"
Expand-Archive "E:\DB\pixel_to_civilization_win\PixelToCivilization_V9.7.1_HTML5.zip" -DestinationPath $dst -Force
python -m http.server 8000 --directory $dst
```

---

## 五、存档兼容性

- 存档包含 schema / version 字段，V9.7.2 与 V9.7.1 存档载体一致（PlayerPrefs 摘要 + IndexedDB 正文）。
- 若 V9.7.2 存档在 V9.7.1 上出现不兼容，读档前可在存档界面选择其他槽位；旧版本无法识别的新字段会被忽略，不会损坏其他槽位。
- 关键操作前建议手动在独立槽位存档，避免覆盖自动存档。

---

## 六、回滚后验证

1. 版本显示为 V9.7.1；
2. 可新游戏、时间推进、存档 / 读档；
3. 原有功能（不含 V9.7.2 地面部队改动）正常。

---

## 七、本次回滚需求状态

- V9.7.2 经真实构建与浏览器探针回归，**核心功能全部 PASS**，当前**无需回滚**；
- 本方案作为预案保留。
