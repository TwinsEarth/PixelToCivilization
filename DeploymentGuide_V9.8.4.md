# V9.8.4 构建部署指南（DeploymentGuide）

> 版本：V9.8.4 ｜ 目标平台：WebGL（HTML5）｜ 同套 C# 可构建 Win/macOS/Android/iOS

## 1. 环境

| 项 | 值 |
|---|---|
| 编辑器 | 团结引擎 Tuanjie 2022.3.61t13（基于 Unity 2022.3 LTS） |
| 路径 | `C:\Program Files\Tuanjie\Hub\Editor\2022.3.61t13\Editor\Tuanjie.exe` |
| 工程 | `E:\DB\pixel_to_civilization_win\PxC982` |
| 构建入口 | `PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI` |
| 输出 | `PxC982\BuildWebGL\`（22 文件，约 92 MB） |

## 2. 构建（命令行）

```powershell
# 0) 先杀残留进程 + 删锁（否则报 Another Tuanjie instance / IOException 占目录）
Get-Process | Where-Object { $_.Name -match 'Tuanjie|bee|il2cpp' } | Stop-Process -Force
Remove-Item "E:\DB\pixel_to_civilization_win\PxC982\Library\UnityLockfile" -Force -ErrorAction SilentlyContinue
# 1) 如 8070 服务器在跑且指向 BuildWebGL，先停（否则构建写目录被占）
# 2) 构建
& "C:\Program Files\Tuanjie\Hub\Editor\2022.3.61t13\Editor\Tuanjie.exe" -batchmode -quit -nographics `
  -projectPath "E:\DB\pixel_to_civilization_win\PxC982" `
  -executeMethod "PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI" `
  -logFile "E:\DB\pixel_to_civilization_win\PxC982\build_v984b.log"
# 3) 确认
Select-String -Path "E:\DB\pixel_to_civilization_win\PxC982\build_v984b.log" -Pattern "BUILD_SUCCESS|error CS|Exception" 
```

## 3. 本地运行（WebGL 必须经 HTTP，不能 file://）

```powershell
python -m http.server 8070 --directory E:\DB\pixel_to_civilization_win\PxC982\BuildWebGL
# 浏览器访问 http://127.0.0.1:8070/index.html
```

或直接解压发布 zip：
- Windows：双击 `start_webserver.bat`
- macOS：`chmod +x start_webserver.command` 后双击（提示损坏时 `xattr -dr com.apple.quarantine start_webserver.command`）

## 4. 版本号点（V9.8.4）

- `Assets/Scripts/Editor/WebGLBuilder.cs`：`BuildVer=9.8.4`（含 index.html 标题模板 12 处替换）
- `ProjectSettings/ProjectSettings.asset`：bundleVersion ×2（历史版本曾同步，本轮 WebGLBuilder 统一即可）

## 5. 探针调试（浏览器 F12 / bu 平面）

```js
window.unityInstance.SendMessage('GameManager','WebRallyUiCycle', 20)   // 集结令压力
window.unityInstance.SendMessage('GameManager','WebV963Probe')         // 地面部队状态
window.unityInstance.SendMessage('GameManager','WebTowerBounds')       // 塔结构
```
结果写回 `window.pxcProbe`（Application.ExternalEval）。

## 6. 常见错误排查

| 错误 | 根因 | 处理 |
|---|---|---|
| `Another Tuanjie instance is running with this project open` | 残留进程 + UnityLockfile | 全杀 Tuanjie/bee/il2cpp + 删 UnityLockfile |
| `IOException ... BuildWebGL` 构建失败 | HTTP 服务器占输出目录 | 先杀 8070 再构建，成功后再起 |
| WebGL 打开黑屏/白屏 | file:// 直开 | 必须经 HTTP 服务器 |
| `Mesh can not have more than 65000 vertices` | 动态 UI 单 Canvas 顶点爆炸（V9.8.2 前） | 已治本：事件 80 字截断 + 三独立 Canvas + 编年史 25 + 日志 120 |
| 古典部队困湖（V9.8.3 前） | NearestLand 半径 48 不够 | 已治本：240 格三档步进 + 困水 2.0 + 去 continue |
