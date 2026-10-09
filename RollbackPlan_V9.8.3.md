# V9.8.3 回滚方案（RollbackPlan）

> 版本：V9.8.3 ｜ 目标：V9.8.3 出现阻断性问题时，快速回到 V9.8.2 或修复点。

## 1. 回滚触发条件

- 游戏启动即崩溃 / WebGL 白屏 / wasm 加载失败。
- 集结令插旗仍崩溃（回归证据失效）。
- 地面部队仍困湖不战斗。
- 塔防模型渲染异常（黑块/闪烁/贴图丢失）。

## 2. 源码回滚（Git）

```bash
# 查看当前 HEAD
git log --oneline -5

# 方案 A：回退到 V9.8.2 标签
git checkout v9.8.2 -- Assets/Scripts/Systems/WarBroadcastSystem.cs \
    Assets/Scripts/Systems/RallySystem.cs \
    Assets/Scripts/Systems/GroundWarfareSystem.cs \
    Assets/Scripts/Buildings/BuildingMeshFactory.cs \
    Assets/Scripts/Editor/WebGLBuilder.cs \
    ProjectSettings/ProjectSettings.asset

# 方案 B：丢弃 V9.8.3 全部改动（仅在确认 V9.8.3 完全不可用时）
# git reset --hard v9.8.2
```

- 回滚后重新构建（见 DeploymentGuide §3），产出 `BuildWebGL/` 并重新打包。

## 3. 发布物回滚（GitHub Release）

- 远程已保留 `v9.8.2` Release 与 zip 资产（`PixelToCivilization_V9.8.2_HTML5.zip`，sha256 `8b55a5986c9c331b6f69e8fed8a4e4b49cb81f86c222058ce474a96bc1599ddc`）。
- 若 V9.8.3 Release 异常：编辑该 Release 标记 `Pre-release`，或删除后重新发布 v9.8.2 资产；玩家侧下载 v9.8.2 zip 即可回退。

## 4. 存档兼容

- 存档载体为 IndexedDB 正文 + PlayerPrefs 摘要（V9.7.1 起），带版本字段与迁移链；V9.8.3 未改动存档格式，V9.8.2 存档可直接加载。
- 若存档异常：清浏览器站点数据（IndexedDB/PlayerPrefs）后重开新档。

## 5. 验证回滚成功

1. 打开 `http://127.0.0.1:8070/index.html`，标题应显示回滚后版本号。
2. 三 BUG 回归探针（TestReport §3）全部通过。
3. 存档读档正常、战斗/编年史正常。

## 6. 注意事项

- 不要删除远程 v9.8.2 tag/Release（回滚锚点）。
- 构建产物（BuildWebGL/）不入库，回滚后必须重新构建，不能复用旧产物。
