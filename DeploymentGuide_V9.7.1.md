# 从像素到文明 · 部署与运行指南 — V9.7.1

引擎：团结引擎 Tuanjie 2022.3.62t12（Unity 2022.3 LTS 兼容线）· 目标平台：PC / Android / iOS / WebGL（主交付）
渲染管线：URP 14.2.0-t1 · 输入：旧 Input（UGUI + 自定义鼠标/触摸）· 架构：单包 + Assets/Resources 加载

V9.7.1 相比 V9.7.0 的部署差异：① 新增 IndexedDB 混合存档桥；② 高倍速自适应分段；③ 实体生成门控；④ 高架桥生命周期清理。

---

## 一、Unity CLI 命令

### WebGL 一键构建（本版实际使用）

```powershell
& "E:\Unity\2022.3.62t12\Editor\Tuanjie.exe" -batchmode -quit `
  -projectPath "E:\DB\pixel_to_civilization_win\TuanjieCivilization_V9" `
  -logFile "E:\DB\pixel_to_civilization_win\v971b_build.log" `
  -executeMethod PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI
```

注意：`Start-Process` 启动后命令行立即返回，实际构建进程（GUI）独立运行。必须用
`Get-Process -Name Tuanjie` 取 PID 并 `WaitForExit`，再看日志末尾 `BUILD_SUCCESS:<目录>`。

### 多平台构建（V9.6.7 BuildPipelineCLI，兼容保留）
`-executeMethod PixelToCivilization.EditorTools.BuildPipelineCLI.BuildTargetCLI -buildTarget <WebGL|Win64|Android|macOS|iOS>`。
macOS/iOS 需在 macOS + Xcode 上执行；Android 需 Android Build Support + SDK/NDK/JDK。

---

## 二、PlayerSettings（部署硬化）

| 项 | 值 |
|---|---|
| companyName / productName / bundleVersion | ToFuture / 从像素到文明 9.7.1 / 9.7.1 |
| Scripting Backend | IL2CPP（WebGL 必须 IL2CPP） |
| ApiCompatibilityLevel | .NET Standard 2.1 |
| Il2CppCompilerConfiguration | Release |
| WebGL.threadsSupport | false（免 COOP/COEP 跨域头） |
| WebGL.compressionFormat | Disabled（任意静态服务器可跑） |
| WebGL.dataCaching | false（防旧 .data 与新 wasm 错位 → memory access out of bounds） |
| WebGL.exceptionSupport | ExplicitlyThrownExceptionsOnly |

---

## 三、混合存档（V9.7.1 新增，部署须知）

- WebGL 下存档正文存 **IndexedDB**：库名 `PxC_SaveBody`、version 1、objectStore `body`；
  槽位摘要/崩溃状态仍存 PlayerPrefs。
- 初始化在 `GameBootstrap` 协程中：PxcInitDb → 等 Ready（6 秒超时兜底）→ 自动迁移旧 PlayerPrefs 正文。
- IndexedDB 不可用/超时时降级为内存缓存（不卡死），但该场景正文不持久化。
- 写入经 500ms 防抖 + visibilitychange/beforeunload 自动 flush；单事务原子提交。
- 非 WebGL 平台：`PxcStorage` 直接 Ready，存档走原 PlayerPrefs 路径。
- 部署到静态服务器无需特殊配置（IndexedDB 为浏览器本地能力）。

---

## 四、高倍速自适应分段与实体门控（V9.7.1 新增）

- 模拟帧预算：WebGL 0.006s / 桌面 0.010s；单帧年结硬上限 120；超出结转下帧。
- 实体上限（逼近 90% 或内存 Critical 即暂停生成）：
  船 200 / 鸟 500 / 鱼 300 / 地面部队 60 / 车 120 / 火车 40 / 飞机 80 / 建筑 800。
- 作用：高倍速（尤其 1000×）下防止单帧 tick 过载与实体无限膨胀导致的崩溃。

---

## 五、IL2CPP / 缓存损坏排查

- Bee 增量 dag 损坏（Internal build system error）：
  ```powershell
  cmd /c "rd /s /q <工程>\Library\ScriptAssemblies"
  ```
  并删除对应 Library/Bee dag 目录（dag 为目录，必须 cmd rd）后重构建。
- 新增 .cs/.jslib 未被拾取：删该文件 .meta + 清 ScriptAssemblies/Bee 后重构建。

---

## 六、Addressables（不启用）

未安装 com.unity.addressables，单包 + Resources 加载、WebGL 约 55MB 在预算内，保持现状（铁律：不随意加依赖）。

---

## 七、日志路径

| 日志 | 路径 |
|---|---|
| 构建日志 | `-logFile` 指定（v971b_build.log），尾部 BUILD_SUCCESS/BUILD_FAILED |
| Editor 日志（Windows） | `%LOCALAPPDATA%\Tuanjie\Editor\Editor.log` |
| Editor 日志（macOS） | `~/Library/Logs/Tuanjie/Editor.log` |
| 浏览器 Console | F12 → Console（Unity Debug.Log 转发） |
| 浏览器存储 | F12 → Application → IndexedDB → PxC_SaveBody；Local Storage → PlayerPrefs 摘要 |

---

## 八、本地运行（构建产物）

```powershell
cd <解压目录>
python -m http.server 8052 --directory .
# 浏览器访问 http://127.0.0.1:8052/index.html
```

WebGL 不能 file:// 直开。包内附 start_webserver.bat（Win）/ start_webserver.command（macOS，先 `chmod +x`）。

**端口被旧 python 服务器占用**时（页面显示旧版本）：
```powershell
Get-CimInstance Win32_Process -Filter "Name='python.exe'" | Select-Object ProcessId,CommandLine
# 杀掉占用端口且 --directory 指向旧目录的进程，再重启
```

---

## 九、回归验证（浏览器探针）

- `WebTimeProbe`：期望 `hard=120`
- `WebSaveProbe`：期望 `schema:3`，且 IndexedDB 库 PxC_SaveBody 含正文键
- `WebOpenBuildTab('infra')`：期望基建标签含高架柱/铁路/城际公路/机场/高铁站
- `WebBuildGround`：跳年 1949 后期望现代地面部队镜像生成
- `WebViaductTest`：期望 `[VCT] RESULT: PASS`（runsRemoved/cellsCleared/piersRemoved 全 True）
