# 《从像素到文明》V9.8.2 部署指南（Deployment Guide）

> 版本：V9.8.2 ｜ 平台：WebGL（主交付）；同一套 C# 可构建 Win/macOS/Android/iOS
> 引擎：团结引擎 Tuanjie 2022.3.61t13（Hub 版，`C:\Program Files\Tuanjie\Hub\Editor\2022.3.61t13`）

---

## 一、运行（玩家/用户）

方式 A（Windows）：解压 `PixelToCivilization_V9.8.2_HTML5.zip` → 双击 `start_webserver.bat`，浏览器自动打开。
方式 B（macOS）：`chmod +x start_webserver.command` 后双击（若提示"已损坏"，系统设置→隐私与安全性允许，或 `xattr -dr com.apple.quarantine start_webserver.command`）。
方式 C（任意系统手动）：解压目录运行 `python3 -m http.server 8000`，访问 `http://127.0.0.1:8000/index.html`。
> WebGL 禁止 file:// 直开，必须经本地 HTTP 服务。

## 二、从源码构建（开发者）

前置：
1. 安装团结引擎 Tuanjie 2022.3.61t13（Hub 版；D:\TJ2022 的非 Hub 编辑器因 Licensing Code 10 不可用——勿用）。
2. Hub "Add project from disk" 打开仓库根目录（含 Assets、ProjectSettings、Packages）。
3. 等待首次导入（本地包 com.unity.render-pipelines.* 14.1.0 已入库，无需额外下载）。

WebGL 一键出包（PowerShell，Windows 示例）：
```powershell
& "C:\Program Files\Tuanjie\Hub\Editor\2022.3.61t13\Editor\Tuanjie.exe" -batchmode -quit -nographics `
  -projectPath "<本仓库路径>" `
  -executeMethod "PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI" `
  -logFile build.log
```
产物：工程下 `BuildWebGL\`（四件套 + TemplateData + 启动脚本）。
备选多平台入口：`PixelToCivilization.EditorTools.BuildPipelineCLI.BuildTargetCLI`（Win/Android/iOS/WebGL）。
也可编辑器内 File → Build Settings → WebGL → Build。

## 三、日志路径与排查

| 场景 | 路径/方法 |
|---|---|
| 批处理构建日志 | `build.log`（工程根，由 -logFile 指定） |
| 浏览器运行时 | DevTools Console / Network（WebGL 加载、WebAssembly 实例化） |
| 崩溃恢复 | 游戏内置崩溃架构：异常捕获 + 自动保存 + 安全模式 + 回滚槽（编年史可见「存续权统失败，已回滚」） |

常见错误排查：
- `Code 10 signature verification failed`：编辑器许可异常 → 换 Hub 激活版编辑器。
- `memory access out of bounds`：wasm 内存不足 → 关闭多余标签页，或调大 WebGL memory size（WebGLBuilder 配置）。
- AudioContext autoplay warning：无害，用户点击后恢复。
- 中文乱码：请用 Tuanjie 61t13 重新构建（字体为程序化生成，不依赖系统字体）。

## 四、多平台说明（未验证项标注）

- Win/macOS/Android/iOS 原生构建入口已具备（BuildPipelineCLI），**本轮未实际执行**（标注未验证）。
- IL2CPP 已用于 WebGL 构建（ConfigurePlayerSettings 已设）；原生平台构建时建议同样使用 IL2CPP。
