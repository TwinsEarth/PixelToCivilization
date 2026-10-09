# V9.8.3 构建部署指南（DeploymentGuide）

> 版本：V9.8.3 ｜ 目标：WebGL / HTML5（主交付）；同一套 C# 可构建 Win / macOS / Android / iOS

## 1. 环境

- 团结引擎：`C:\Program Files\Tuanjie\Hub\Editor\2022.3.61t13\Editor\Tuanjie.exe`（Hub 版，**唯一可用**；D:\TJ2022 全系许可失效勿用）
- 项目：`E:\DB\pixel_to_civilization_win\PxC982`
- 构建入口：`PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI`

## 2. 构建前置（铁律）

每次构建前必须：
1. 全杀残留进程：`Tuanjie`、`TuanjieShaderCompiler`、`TuanjiePackageManager`、`TuanjieAutoQuitter`、`TuanjieCrashHandler64`、`bee`、`il2cpp`（否则报 `Another Tuanjie instance is running with this project open`）。
2. 删除 `PxC982\Library\UnityLockfile`。
3. 确认无其它 Unity/团结引擎实例占用该项目。

## 3. 命令行构建（WebGL）

```
& "C:\Program Files\Tuanjie\Hub\Editor\2022.3.61t13\Editor\Tuanjie.exe" -batchmode -quit -nographics `
  -projectPath "E:\DB\pixel_to_civilization_win\PxC982" `
  -executeMethod "PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI" `
  -logFile "E:\DB\pixel_to_civilization_win\PxC982\build.log"
```

- 产物：`PxC982\BuildWebGL\`（22 文件：index.html / Build\*.data|.wasm|.framework.js|.loader.js / TemplateData / 启动脚本 / 说明 txt）
- 成功标志：日志含 `BUILD_SUCCESS`；index.html 标题含 `V9.8.3`。

## 4. 本地运行

WebGL 禁止 `file://` 直开，必须 HTTP 服务：

```
python -m http.server 8070 --directory E:\DB\pixel_to_civilization_win\PxC982\BuildWebGL
```

浏览器访问 `http://127.0.0.1:8070/index.html`。

用户侧一键启动（随包分发）：
- Windows：双击 `start_webserver.bat`
- macOS：`chmod +x start_webserver.command` 后双击（若提示损坏：系统设置→隐私与安全性→允许，或 `xattr -dr com.apple.quarantine start_webserver.command`）
- 任意系统：`python3 -m http.server 8000` 手动访问

## 5. 回归探针

见 TestReport §4（`unityInstance.SendMessage('GameManager','WebXxx', ...)`，结果在 `window.pxcProbe` / console `[WEB]`）。

## 6. 打包与发布

1. `Compress-Archive -Path "PxC982\BuildWebGL\*" -DestinationPath "PixelToCivilization_V9.8.3_HTML5.zip"`（57,035,556 B）。
2. 密钥扫描（源码+产物）零命中后再发布。
3. git commit/push main → tag `v9.8.3` → GitHub Release（附 zip）。

## 7. 日志路径

- 构建日志：`E:\DB\pixel_to_civilization_win\PxC982\build_v983c.log`（本轮 BUILD_SUCCESS）
- 运行时日志：浏览器 DevTools console（`[WEB]`/`[Web]` 前缀为探针输出）

## 8. 常见错误排查

| 错误 | 处理 |
|---|---|
| Another Tuanjie instance is running | 全杀 Tuanjie/bee/il2cpp 进程 + 删 Library\UnityLockfile |
| Licensing Code 10 | 换用 Hub 版 2022.3.61t13（D:\TJ2022 不可用） |
| CS0246 NUnit | Tests 目录冲突（历史已处理，勿恢复） |
| CS2001 Bee 缓存 | 清 Bee 缓存 |
| 构建停 IL2CPP 只出 15 文件 | 保证 -nographics + 完整执行 BuildCLI，勿中断 |
| RuntimeError wasm | 检查插旗语音链路（V9.8.3 已隔离 speechSynthesis） |
