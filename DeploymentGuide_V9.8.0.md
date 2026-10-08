# 《从像素到文明》V9.8.0 部署指南

- 版本：V9.8.0「智能体驱动 · 万物协作」
- 引擎：团结引擎 Tuanjie 2022.3.62t12（兼容 Unity 2022.3 LTS）
- 主交付：WebGL / HTML5

---

## 一、普通玩家：运行 HTML5 版

从 Release 下载 `PixelToCivilization_V9.8.0_HTML5.zip`，解压后：

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
  -logFile "E:\DB\pixel_to_civilization_win\TuanjieCivilization_V9\v980_build14.log"
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
| **防裁剪** | **`Assets/link.xml`：Assembly-CSharp preserve="all"（SendMessage 反射目标与 JSON 类型不被裁剪）** |
| WebGL 注意 | 不开启 threadsSupport / 自定义 compressionFormat / dataCaching |
| 存档载体 | PlayerPrefs 摘要 + IndexedDB 正文（库 `PxC_SaveBody`） |
| 智能体入口 | `Assets/Scripts/AI/Agents/`（6 文件，namespace PixelToCivilization.AI.Agents） |
| 身份数量 | 118（AgentCatalog 集中声明） |
| 行为库持久化 | PlayerPrefs 键 `PXC_AgentLib_v980`；模式键 `PXC_AgentMode_v980` |
| 构建入口 | `PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI` |
| 多平台入口 | `PixelToCivilization.EditorTools.BuildPipelineCLI.BuildTargetCLI`（BuildVer 已 9.8.0） |

---

## 四、日志路径与排错

| 现象 | 排查 |
|---|---|
| 构建立刻退出、无日志 | 检查 `-logFile` 参数引号是否被外层解析 |
| SendMessage 探针静默失败 | 确认 link.xml 含 Assembly-CSharp preserve="all" |
| **面板 Head 膨胀 / 滚动区被挤塌** | **确认 Box/Body VLG 含 childControlHeight=true、childForceExpandHeight=false；top/filt 行 LayoutElement 先于 HLG 且 layoutPriority=2** |
| **面板滚动条不滚动** | 确认 Content 行高固定（LayoutElement preferredHeight）、content 总高确定；用完整 CDP 鼠标序列（含 mouseMoved 起势）验证 |
| 智能体不决策 / 无 LLM 调用 | 离线模式正常；在线模式需在面板填 Key，无 Key 自动降级 |
| 浏览器白屏 | DevTools Console 看 wasm/加载错误；确认经 HTTP 而非 file:// |
| 端口被占、显示旧内容 | 查所有 python http.server 进程及 --directory，杀旧重启 |
| macOS "已损坏" | `xattr -dr com.apple.quarantine start_webserver.command` |
| **wasm-opt 偶发崩溃（0xC0000409）** | 工具链偶发，构建内存紧张时重试通常恢复 |
| **il2cpp BUILD_FAILED（无 CS 错误）** | Bee 增量缓存损坏，删除 `Library/Bee` 全量重建 |

---

## 五、关于 AI 联网密钥

开源版本**不含任何密钥**。智能体默认离线即可完整运行；如需接入在线大模型：

- 在游戏内智能体面板 / 帮助 / Debug 通过按钮弹窗填写（PlayerPrefs，不随包分发）；
- LLM 端点默认 `https://api.deepseek.com/chat/completions`，可用 PlayerPrefs `PXC_AI_BASE` / `PXC_AI_MODEL` 覆盖。

**请勿将密钥提交进仓库。**

---

## 六、验证记录

- 本版本 WebGL 已实际构建成功（BUILD_SUCCESS=1，EXIT 0，CS_ERRORS=0，wasm 约 33.6 MB）；
- 浏览器探针回归通过（12 核心用例全 PASS、118 身份全装载，详见 TestReport_V9.8.0.md）；
- 面板布局根因修复并验证；
- 其余原生平台（Win/macOS/Android/iOS）本轮未实际构建，标注为"未验证"。
