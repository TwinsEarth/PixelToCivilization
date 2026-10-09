# 《从像素到文明》V9.8.2 回滚方案（Rollback Plan）

> 版本：V9.8.2 ｜ 仓库：github.com/TwinsEarth/PixelToCivilization

---

## 一、回滚触发条件

1. 玩家/QA 报告三 BUG 修复引入新回归（如集结令、地面部队、塔防出现新的崩溃/错乱）。
2. 浏览器实测发现 V9.8.2 包无法进局、时间不推进或存档损坏。
3. 在线大模型接入后出现密钥相关异常（不含密钥随包分发风险）。

## 二、回滚目标版本

- 源码回滚点：`edab668`（V9.8.1，三 BUG 加固前基线）+ 上一发布版本 V9.8.1 WebGL 包。
- 产物回滚点：GitHub Release `v9.8.1` 的 `PixelToCivilization_V9.8.1_HTML5.zip`。

## 三、回滚步骤

### 源码回滚（git）
```powershell
git checkout edab668 -- Assets Packages ProjectSettings
git commit -m "V9.8.2 rollback to V9.8.1 baseline"
```
或整体回退：
```powershell
git reset --hard edab668   # 仅当确认丢弃 V9.8.2 改动
```

### 构建回滚
用 Hub 版 61t13 重新执行 WebGL 构建（命令见 DeploymentGuide），产出自 V9.8.1 代码的包。

### 发布回滚
1. GitHub Release：将 `latest` 标记移回 v9.8.1（如已标记 v9.8.2）。
2. 保留 v9.8.2 release 但标注"已回滚"，或删除 release（不删 tag，保留审计）。

### 玩家端回滚
玩家重新下载 v9.8.1 zip 即可；存档格式带 version 字段（V9.6.6 起），MigrationManager 支持旧档升级，若回滚后读档版本高于当前，按存档兼容策略提示或重建新档。

## 四、存档兼容说明

- 存档含 `int version` 字段（V9.6.6 架构），MigrationManager 逐版本迁移。
- V9.8.2 未改动存档结构 → V9.8.1 存档可直读；回滚到 V9.8.1 读 V9.8.2 存档时，若结构未变则可读（本轮无存档结构变更）。

## 五、风险与审计

- 回滚不丢失 git 历史（commit 可追溯）。
- 回滚后三 BUG 将回到 V9.8.1 状态（集结令仍可能连插崩溃），需在回滚前权衡。
- 若回滚原因是构建/许可环境问题而非代码问题，建议先修复环境（换 Hub 版 61t13）而非回滚代码。
