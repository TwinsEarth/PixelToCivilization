# 从像素到文明 · 部署与运行指南 — V9.6.7

引擎：团结引擎 Tuanjie 2022.3.62t12（Unity 2022.3 LTS 兼容线）· 目标平台：PC / Android / iOS / WebGL（主交付）
渲染管线：URP 14.2.0-t1 · 输入：旧 Input（UGUI + 自定义鼠标/触摸）· 架构：单包 + Assets/Resources 加载

---

## 一、Unity CLI 命令

### 1. 一键多平台构建（V9.6.7 新增 BuildPipelineCLI）

```powershell
# WebGL（主交付，Windows 本机即可）
& "E:\Unity\2022.3.62t12\Editor\Tuanjie.exe" -batchmode -quit `
  -projectPath "E:\DB\pixel_to_civilization_win\TuanjieCivilization_V9" `
  -logFile "E:\DB\pixel_to_civilization_win\v967_build.log" `
  -executeMethod PixelToCivilization.EditorTools.BuildPipelineCLI.BuildTargetCLI `
  -buildTarget WebGL

# Windows x64（需 Hub 安装 Windows Build Support）
# ... 同上，-buildTarget Win64 -outDir D:\out\BuildWindows

# Android（需 Android Build Support + SDK/NDK/JDK；ARM64 + IL2CPP 自动设置）
# ... -buildTarget Android -outDir D:\out\BuildAndroid

# macOS（需在 macOS 机器上执行，产物为 .app 目录）
# ... -buildTarget macOS

# iOS（需 macOS + Xcode，产物为 Xcode 工程，再 xcodebuild 出 .ipa）
# ... -buildTarget iOS
```

`-outDir` 缺省时输出到工程根 `Build<平台名>/`。退出码：0=成功，1=失败；logFile 尾部 `BUILD_SUCCESS:<目录>` 或 `BUILD_FAILED:<原因>`。

### 2. 旧入口（兼容保留）
`-executeMethod PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI` 仍可单独构建 WebGL。

### 3. Editor 菜单（调试构建）
「像素到文明/⑪ 一键构建 WebGL(部署)」「⑪ 一键构建 Windows x64(部署)」。

---

## 二、BuildPipeline 脚本（BuildPipelineCLI.cs）

```text
BuildTargetCLI()                    命令行入口（解析 -buildTarget / -outDir）
Build(target, outDir?)              核心：Refresh(ForceUpdate) → SwitchActiveBuildTarget → ConfigurePlayerSettings → BuildPlayer → 报告
ConfigurePlayerSettings(group, t)   公共（公司/产品/版本/IL2CPP Release/.NET Standard）+ 平台特定
OutDirName(t)                       输出目录映射
LogReport(report)                   result/size/duration/warnings/errors + 顶层步骤耗时
```

行为要点：
- 构建前 `AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate)`——命令行新增脚本防漏编译（CS0103/CS0234 历史坑）。
- 每次构建删旧输出目录重生成（干净产物）。
- WebGL 构建后自动追加中文全屏加载页 + 本地服务器脚本（复用 WebGLBuilder.WriteCustomIndex）。

---

## 三、PlayerSettings（部署相关硬化）

| 项 | 值 | 说明 |
|---|---|---|
| companyName / productName / bundleVersion | ToFuture / 从像素到文明 9.6.7 / 9.6.7 | 全平台统一 |
| Scripting Backend | IL2CPP（全平台） | 发布用；Editor 内仍 Mono |
| ApiCompatibilityLevel | .NET Standard 2.1 | 与 C#9 兼容 |
| Il2CppCompilerConfiguration | Release | 体积/性能 |
| WebGL.threadsSupport | false | 免 COOP/COEP 跨域隔离头 |
| WebGL.compressionFormat | Disabled | 免 Content-Encoding，任意静态服务器可跑 |
| WebGL.dataCaching | false | 防同源多版本 .data 与 wasm 错位 → memory access out of bounds |
| WebGL.exceptionSupport | ExplicitlyThrownExceptionsOnly | Release 档 |
| Android ARM64 / minSdk 22 / target 32 / 包名 com.tofuture.pixelcivilization | 自动设置 | |
| iOS 12.0+ / 包名 com.tofuture.pixelcivilization | 自动设置 | |
| Standalone x86_64 | 自动设置 | Win/macOS |

---

## 四、IL2CPP

- 全平台发布档 IL2CPP；WebGL 必须 IL2CPP（Mono 不支持 WebGL）。
- 常见 IL2CPP 构建失败：
  1. `Internal build system error` / 增量 dag 损坏 → 删缓存后重构建：
     ```powershell
     cmd /c "rd /s /q <工程>\Library\ScriptAssemblies"
     cmd /c "rd /s /q <工程>\Library\Bee\artifacts\2000b0aE.dag"
     ```
     （注意：Bee dag 是**目录**；PowerShell Remove-Item 与 [IO.File]::Delete 均会失败，必须用 `cmd /c rd`）
  2. 内存不足：批处理下 IL2CPP 全量编译吃内存，构建机建议 ≥16GB；失败看 log 中 il2cpp 段错误码。
  3. 反射裁剪：仅影响动态调用（本项目探针全部 [Preserve]；存档/迁移用纯托管代码，无 Link 风险）。

---

## 五、Addressables（V9.6.7 结论：不启用）

项目当前 **未安装 com.unity.addressables**，且为单包 + Resources 加载。评估（菜单「⑫ Addressables 就绪评估」）：
- Resources 资产规模小、无热更/独立更新需求、WebGL 单包 55MB 在内存预算内 → **本轮保持现状，不接入**（铁律：不随意加依赖）。
- 接入时机：需按需下载/拆包热更、单 .data 超 1.5GB、跨平台统一资产管线时。
- 接入步骤：见菜单「⑫ 生成 Addressables 接入清单」生成的 `AddressablesMigrationChecklist.md`（含 manifest 追加、组/标签、Resources.Load 批量改写、WebGL bundle 缓存与内存预算、回滚方案）。

---

## 六、日志路径

| 日志 | 路径 |
|---|---|
| 构建日志（-logFile 指定） | 命令行里指定的 `v967_build.log`（含 BUILD_SUCCESS/BUILD_FAILED/报错） |
| Editor 日志（Windows） | `%LOCALAPPDATA%\Unity\Editor\Editor.log`（团结引擎：`%LOCALAPPDATA%\Tuanjie\Editor\Editor.log`） |
| Editor 日志（macOS） | `~/Library/Logs/Unity/Editor.log` / `~/Library/Logs/Tuanjie/Editor.log` |
| WebGL 浏览器控制台 | F12 → Console（Unity Debug.Log 转发）；Network 看 .data/.wasm 加载；Application → IndexedDB → UnityCache |
| 游戏内日志 | Debug 面板日志 + CrashGuard 环形日志 200 条（随存档持久化） |
| 存档键 | PlayerPrefs：PxC_Save_auto|1..5、PxC_SaveSum_*、PxC_Tmp_*（原子写临时）、PxC_Bak_*/BakSum_*（写前备份）、PxC_Roll_0/1/2（回滚链）、PxC_State/PxC_Crash_*（崩溃标记） |

---

## 七、常见错误排查表

| 现象 | 根因 | 处理 |
|---|---|---|
| 切换平台失败提示缺模块 | 未装对应 Build Support | Tuanjie Hub → 当前编辑器 → 添加模块 |
| 新增 .cs 后构建报 CS0103/CS0234 | 命令行 -batchmode 漏导入 | 已内置 Refresh(ForceUpdate)；手动也可先开一次 Editor |
| WebGL 启动 memory access out of bounds | 同源旧 UnityCache（旧 .data 与新 wasm 错位） | index.html 已自动按版本清 IndexedDB；仍现 → 浏览器清站点数据 |
| SharedArrayBuffer 不可用 / COOP-COEP 报错 | WebGL 多线程需跨域隔离头 | threadsSupport=false（已设），勿再开线程 |
| 解压失败 / Content-Encoding 报错 | 开启过 Brotli/Gzip 压缩 | compressionFormat=Disabled（已设） |
| PlayerPrefs 写入后刷新丢失（WebGL） | 未 Save/未落盘 | V9.6.6 起写入路径均 SetString+Save；临时键残留由启动 CleanupTempKeys 清理 |
| Internal build system error | Library/Bee 增量 dag 损坏 | `cmd /c rd /s /q` 删 ScriptAssemblies + 2000b0aE.dag 后重构建 |
| Android 构建失败（SDK/gradle） | 缺模块/JDK 或 gradle 下载慢 | Hub 装 Android Build Support；配镜像或代理 |
| iOS 无法在 Windows 构建 | 平台约束 | 需 macOS + Xcode：Unity 出 Xcode 工程，xcodebuild 打包 |
| 本地服务器端口被占用 | 旧 python 进程存活 | `Get-NetTCPConnection -LocalPort 8052` 取 OwningProcess 杀旧后重启 |
| 浏览器加载旧包 | HTTP 缓存 | index.html no-store + VER 时间戳唯一化（已做）；强刷 Ctrl+F5 |
| 中文乱码（bat/command） | 编码不符 | bat=GBK+CRLF、command=LF 无 BOM（已按平台写） |

---

## 八、本地运行（构建产物）

```powershell
# 部署目录运行静态服务器
cd <解压目录>
python -m http.server 8052 --directory .
# 浏览器访问 http://127.0.0.1:8052/index.html
```

WebGL 不能 file:// 直开；包内附 start_webserver.bat（Win）/ start_webserver.command（macOS，先 `chmod +x`）。
