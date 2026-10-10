# V9.8.4 回滚方案（RollbackPlan）

> 版本：V9.8.4 ｜ 仓库：github.com/TwinsEarth/PixelToCivilization

## 1. 回滚目标

若 V9.8.4 在用户环境出现新回归（预期外崩溃 / 玩法异常），回滚至 **V9.8.3**（上一已发布版，Release tag `v9.8.3`）。

## 2. 回滚步骤

### A. 玩家本地（zip 版）
1. 关闭游戏与 HTTP 服务器。
2. 下载 `PixelToCivilization_V9.8.3_HTML5.zip`（GitHub Releases tag v9.8.3）。
3. 解压覆盖原目录（或换新目录）。
4. 重新 `start_webserver` 或 `python -m http.server 8000` 运行。

### B. 存档兼容
- V9.8.4 未改存档结构（版本号字段沿用），旧存档可继续读；若异常，删除本地存档槽重开新局（存档入口左下角）。

### C. 源码回滚（开发/构建者）
```powershell
cd E:\DB\pixel_to_civilization_win\PxC982
git checkout v9.8.3 -- Assets/Scripts  # 或整体 git reset --hard v9.8.3
# 重新构建（见 DeploymentGuide §2）
```

## 3. 涉及文件清单（V9.8.4 变更，回滚时还原）

| 文件 | 变更 |
|---|---|
| `Assets/Scripts/Core/GameManager.cs` | AddEvent 80 字截断 |
| `Assets/Scripts/UI/UIManager.cs` | 三独立 Canvas + 编年史 25 |
| `Assets/Scripts/UI/UIManager.Overhaul.cs` | 日志 120 |
| `Assets/Scripts/Systems/GroundWarfareSystem.cs` | NearestLand 240 + 困水 2.0 + 去 continue |
| `Assets/Scripts/Buildings/BuildingMeshFactory.Special.cs` | BuildTower 五型收分塔 |
| `Assets/Scripts/Editor/WebGLBuilder.cs` | 版本号 9.8.4 |

## 4. 验证回滚

- 构建 BUILD_SUCCESS + 页面标题版本号 + 三 BUG 探针回归（TestReport §4 协议）。

## 5. 决策记录

- V9.8.4 为治本版（三 BUG 真根因修复），回滚仅作保险；若 V9.8.4 无回归则继续作为最新版。
