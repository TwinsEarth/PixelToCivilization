# 《从像素到文明》V9.7.3 部署指南

- 版本：V9.7.3「历史 Bug 全量回归复测」
- 引擎：团结引擎 Tuanjie 2022.3.62t12（兼容 Unity 2022.3 LTS）
- 主交付：WebGL / HTML5

---

## 一、普通玩家：运行 HTML5 版

从 Release 下载 `PixelToCivilization_V9.7.3_HTML5.zip`，解压后：

### Windows
双击 `start_webserver.bat`，浏览器自动打开。

### macOS
```bash
chmod +x start_webserver.command
xattr -dr com.apple.quarantine start_webserver.command   # 若提示"已损坏/无法打开"
```
然后双击 `start_webserver.command`。

### 任意系统（手动）
```bash
python3 -m http.server 8000
```
浏览器访问 `http://127.0.0.1:8000/index.html`。

> WebGL 出于安全策略**不能**直接双击 index.html 以 file:// 打开，必须经本地 HTTP 服务。

---

## 二、从源码构建（WebGL）

### 1. 安装引擎
- 团结引擎 Tuanjie 2022.3.62t12（或兼容的 Unity 2022.3 LTS）
- 安装模块中勾选 **WebGL Build Support**。

### 2. 打开工程
- Unity Hub → Add → Add project from disk → 选择工程根目录（含 Assets / ProjectSettings / Packages）。
- 等待首次导入完成。

### 3. 命令行一键出包（Windows）
```powershell
& "E:\Unity\2022.3.62t12\Editor\Tuanjie.exe" `
  -batchmode -quit `
  -projectPath "E:\DB\pixel_to_civilization_win\TuanjieCivilization_V9" `
  -executeMethod PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI `
  -logFile "E:\DB\pixel_to_civilization_win\TuanjieCivilization_V9\v973_slot.log"
```
- 产物输出到工程下 `BuildWebGL/`。
- 成功标志：日志末尾出现 `BUILD_SUCCESS:...`，引擎退出码为 0。

### 4. 编辑器内手动构建
File → Build Settings → 切换平台 WebGL → Build。

---

## 三、关键配置

| 项 | 值 |
|---|---|
| 渲染管线 | URP 14.2.0-t1 |
| 输入系统 | 旧 Input Manager |
| 架构 | 单包无 asmdef，Assets/Resources；脚本全编译进 Assembly-CSharp |
| **防裁剪** | **`Assets/link.xml`：Assembly-CSharp preserve="all"（保证 SendMessage 反射目标与 JSON 类型不被 High stripping 裁剪）** |
| WebGL 注意 | 不开启 threadsSupport / 自定义 compressionFormat / dataCaching 时需对应服务端头 |
| 存档载体 | PlayerPrefs 摘要 + IndexedDB 正文（库 `PxC_SaveBody`） |
| 存档校验 | FNV checksum，基于 canonicalization 固定点串（双精度往返稳定） |
| 构建入口 | `PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI` |
| 多平台入口 | `PixelToCivilization.EditorTools.BuildPipelineCLI.BuildTargetCLI`（参数 -buildTarget/-outDir） |

---

## 四、日志路径与排错

| 现象 | 排查 |
|---|---|
| 构建立刻退出、无日志 | 检查 `-logFile` 参数引号是否被外层解析（用单字符串传参） |
| **SendMessage 探针静默失败 / MissingMethod** | **确认 link.xml 含 Assembly-CSharp preserve="all"（IL2CPP High stripping 裁剪反射目标）** |
| **存档写入后 slot 不出现** | 检查 checksum 是否往返一致（canonicalization）；WebSaveProbe 看 tmp/bak 计数 |
| **reload 后读不到已存档** | 检查 jslib 双 gate preloadFinish（keys/vals 都回来才填缓存） |
| 存档版本号回退 | 确认 SaveData.Version / RuntimeVersion 与 Snapshot 设值 |
| CS0117 / CS0103 编译错误 | 重写/删除方法后全工程 grep 旧方法名、常量名拼写 |
| 浏览器白屏 | 打开 DevTools Console 看 wasm/加载错误；确认经 HTTP 而非 file:// |
| 端口被占、显示旧内容 | 查所有 python http.server 进程及其 --directory，杀旧重启 |
| macOS "已损坏" | `xattr -dr com.apple.quarantine start_webserver.command` |
| 资源/进度异常 | 帮助界面查看 Log；高级 Debug 需密码（本地单机功能） |

---

## 五、关于 AI 联网密钥

开源版本**不含任何密钥**。九神共治默认离线即可完整运行；如需接入在线大模型：
- 配置环境变量 `PXC_ARK_API_KEY`；
- 或在游戏内帮助 / Debug / 九神面板通过按钮弹窗自行填写（不随包分发）。
**请勿将密钥提交进仓库。**

---

## 六、验证记录

- 本版本 WebGL 已实际构建成功（BUILD_SUCCESS=1，EXIT 0，CS_ERRORS=0）；
- 浏览器探针回归通过（45 项矩阵 43 PASS / 2 未验证，详见 TestReport_V9.7.3.md）；
- 存档闭环五项修复全部浏览器验证通过；
- 密钥扫描 RESULT: CLEAN；
- 其余原生平台本轮未实际构建，标注为"未验证"。
