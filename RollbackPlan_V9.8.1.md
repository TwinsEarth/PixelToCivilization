# V9.8.1 回滚方案（Rollback Plan）

## 回滚触发条件
- 9.8.1 上线后出现：集结令仍崩溃、古典部队仍不自主战斗、塔防仍为方楼，或任何新增回归（存档损坏/资源异常/性能劣化）。

## 回滚目标版本
- 上一稳定版：V9.8.0（Release tag `v9.8.0`，zip `PixelToCivilization_V9.8.0_HTML5.zip` 54.6MB）。

## 回滚步骤

### 1. 代码回滚（GitHub）
```
cd E:\DB\pixel_to_civilization_win\PixelToCivilization
git checkout v9.8.0 -- Assets/Scripts/Buildings/BuildingMeshFactory.cs \
  Assets/Scripts/Systems/RallySystem.cs \
  Assets/Scripts/Systems/GroundWarfareSystem.cs \
  Assets/Scripts/Bootstrap/GameBootstrap.cs \
  Assets/Scripts/Core/SaveSystem.cs \
  Assets/Scripts/Editor/WebGLBuilder.cs \
  Assets/Scripts/Editor/SaveArchitectureTest.cs \
  Assets/Scripts/Editor/BuildPipelineCLI.cs \
  Assets/Scripts/UI/UIManager.cs
git commit -m "rollback to v9.8.0 (V9.8.1 regression)"
git push origin main
```
> 9.8.1 仅改动上述 9 个文件 + 6 份文档；回滚只还原代码，文档保留。

### 2. 构建回滚
```
& "E:\Unity\2022.3.62t12\Editor\Tuanjie.exe" -batchmode -quit `
  -projectPath "<仓库路径>" `
  -executeMethod PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI `
  -logFile rollback_build.log
```

### 3. 发布回滚
- 重新打包 `PixelToCivilization_V9.8.0_HTML5.zip`（或直接用已存档的 9.8.0 zip）。
- 删除或隐藏 v9.8.1 Release，重发 v9.8.0（或发 v9.8.2 承载回滚内容）。

### 4. 客户端缓存
- index.html 内 `pxc_build_ver` 更新为回滚版本值，强制触发 IndexedDB 清库，防止客户端加载旧缓存。

## 恢复确认清单
- [ ] WebGL 构建 exit=0
- [ ] 集结令连插 20 次不崩（WebRallyStress ok=20）
- [ ] 古典部队自主移动/战斗（WebV963Probe 坐标变化）
- [ ] 塔防 h/w≥1.4（回滚后为 9.8.0 造型）
- [ ] 存档读档正常（9.8.0 存档兼容）
