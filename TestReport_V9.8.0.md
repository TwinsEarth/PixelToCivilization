# 《从像素到文明》V9.8.0 测试报告

- 版本：V9.8.0「智能体驱动 · 万物协作」
- 引擎：Tuanjie 2022.3.62t12
- 测试平台：WebGL（本地 HTTP 127.0.0.1:8063，独立 origin）
- 日期：2026-10-08
- 测试方式：真实构建 + 浏览器探针（SendMessage / console hook / GraphicRaycast / CDP 完整鼠标序列），非静态推断

---

## 一、测试环境

| 项 | 值 |
|---|---|
| 引擎路径 | E:\Unity\2022.3.62t12\Editor\Tuanjie.exe |
| 工程 | E:\DB\pixel_to_civilization_win\TuanjieCivilization_V9 |
| 构建入口 | PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI |
| 部署目录 | E:\DB\pixel_to_civilization_win\v980_web |
| 访问 URL | http://127.0.0.1:8063/index.html?nc=v980b13 |
| 最终 wasm | 约 33.6 MB |

---

## 二、智能体面板测试用例（本轮核心）

| 编号 | 用例 | 操作 | 预期 | 结果 |
|---|---|---|---|---|
| TC-01 | 编译 | Tuanjie 命令行构建 | 无 CS 错误 | PASS（grep error CS=0） |
| TC-02 | WebGL 构建 | IL2CPP / bee_backend | BUILD_SUCCESS，退出 0 | PASS（code=0，wasm 33.6MB） |
| TC-03 | 面板打开 | SendMessage WebOpenModal agent | 面板显示，118 身份 | PASS（childCount=118） |
| TC-04 | 布局正确 | WebAgentPanel 探针 | Head=24 / top=32 / filt=28 / sr 本地约 513 | PASS（实测全部符合） |
| TC-05 | 拖拽滚动 | CDP 起势→按下→移动→释放 | 内容随拖拽滚动 | PASS（滚到指挥官类别） |
| TC-06 | 过滤 Ship | CDP 点击 Ship 按钮 | 只显示 8 条船 | PASS（childCount=8，h=359，scrollable=False） |
| TC-07 | 切回全部 | CDP 点击全部按钮 | 恢复 118 条 | PASS（childCount=118） |
| TC-08 | 模式切换 | CDP 点击切换模式 | Hybrid → Offline | PASS（WebAgentList 模式=Offline） |
| TC-09 | 求助回复 | WebAgentHelp 探针 | Help → 对应系统 Reply | PASS（驱逐舰求助 → sys_combat Reply） |
| TC-10 | 保存行为库 | WebAgentSaveLib 探针 | 行为库持久化 | PASS（"行为库已保存"） |
| TC-11 | 关闭面板 | CDP 点击关闭按钮 | 面板消失 | PASS（标题不再显示） |
| TC-12 | 建筑身份区块 | 点击棚屋打开属性面板 | 含 💬 身份简介 | PASS（棚屋 Lv.1 显示简介） |

---

## 三、118 身份装载验证（WebAgentList）

| 类别 | 预期 | 实测 | 结果 |
|---|---|---|---|
| System | 41 | 41 | PASS |
| Manager | 4 | 4 | PASS |
| Building | 23 | 23 | PASS |
| Ship | 8 | 8 | PASS |
| Vehicle | 9 | 9 | PASS |
| Ground | 6 | 6 | PASS |
| Person | 12 | 12 | PASS |
| Nature | 13 | 13 | PASS |
| Commander | 2 | 2 | PASS |
| **合计** | **118** | **118** | **PASS** |

运行时状态（实测样本）：模式 Offline（由 Hybrid 切换）、决策数随时间增长、LLM 调用=0（离线不发请求）、行为库=47 条。

---

## 四、消息总线协作测试

| 用例 | 消息流 | 结果 |
|---|---|---|
| 求助匹配 | ship_destroyer 发 Help（topic=combat）→ FindHelper 匹配 sys_combat | PASS（匹配正确，能力域 combat） |
| 回复内容 | sys_combat Reply："收到，我来支援。战斗仲裁已就位。" | PASS（内容正确） |
| 混合模式沉淀 | 求助→回应时 Library.Record（当前切 Offline，不沉淀；Hybrid 下沉淀） | 符合设计 |

---

## 五、性能与稳定性

| 项 | 设计 | 结果 |
|---|---|---|
| 自主调度节流 | 现实秒 0.8s 一次，单帧一个身份 + 一批 pending | PASS（决策数按节流增长，无卡顿） |
| 异常隔离 | 调度 / 消息回调 / LLM 协程全 try/catch | PASS（无阻断性异常） |
| 离线降级 | 无 Key 不发请求、不增加 LLM 调用 | PASS（LLM 调用=0） |
| 内存 | 身份按原型共享，实例不持完整身份 | 符合设计（无千对象身份副本） |

---

## 六、测试结论

- **12 个核心测试用例全部 PASS**；118 身份全部装载；消息总线协作、模式切换、求助回复、行为库保存均正常。
- 面板布局根因（Head 膨胀、top/filt 膨胀、sr 被挤塌）已定位并修复，经探针实测确认。

### 未验证项（如实标注）

1. 船只 / 车辆 / 地面 / 树木属性面板的身份区块：复用同一 `AgentIntroBlock` 方法（已验证建筑类），其余按同法生效，未逐一截图。
2. 在线大模型（Online）模式真实 LLM 调用：当前无 Key 环境，已验证无 Key 正确降级；填 Key 后的联网链路未实测。
3. 1000× Debug 档超长加速：未在本轮专门压测。
4. 混合模式"上传豆包云盘"通道：行为库仅验证本地持久化，上传通道未实现。
