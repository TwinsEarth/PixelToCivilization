# tools/ —— 无 Unity 编译验证与 EditMode 反射测试

本目录让项目在**没有安装团结引擎 / 没有 Unity 许可证**的机器上，也能验证：

1. 全部 C# 游戏源码能否编译（语法 / 类型 / 引用是否正确）；
2. EditMode 单元测试的断言能否真实通过。

> 这是**离线验证通道**，不是 Unity 官方编译，也不能替代真实的 Editor 构建 / IL2CPP / WebGL 打包。
> 它用一个最小化的 `UnityEngine` API 桩（stub）顶替真实引擎，只覆盖游戏代码实际用到的 API。

## 目录结构

```
tools/
├── run_editmode_tests.ps1   # 一键：编译桩 + 6 个构建目标 + 反射运行测试
├── unity_stub/              # UnityEngine / NUnit 的最小 API 桩（C# 源码）
│   ├── UnityEngine.Core.cs
│   ├── UnityEngine.Modules.cs
│   ├── UnityEngine.Objects.cs
│   ├── UnityEngine.Rendering.cs
│   ├── UnityEngine.Systems.cs
│   ├── JsonUtility.cs       # 真实反射 JSON 序列化器（不是空桩）
│   └── NUnit.cs             # 真实断言（失败抛 AssertionException，不是空桩）
└── README.md
```

`run_editmode_tests.ps1` 会在 `tools/build_out/` 下产出 DLL 与响应文件（该目录是临时产物，默认不入库）。

## 前置条件

| 依赖 | 说明 |
|------|------|
| Windows PowerShell | 5.1 或更高（PowerShell 7 也可） |
| .NET Framework 4.x | Windows 自带，脚本引用 `C:\Windows\Microsoft.NET\Framework64\v4.0.30319` |
| Roslyn `csc.exe` | 4.x 编译器（系统自带的 `csc` 仅支持 C# 5，**不够用**） |

### 获取 Roslyn csc.exe

任选一种：

1. 从 NuGet 下载 [Microsoft.Net.Compilers.Toolset](https://www.nuget.org/packages/Microsoft.Net.Compilers.Toolset/)，
   把 `.nupkg` 当 zip 解压，编译器位于 `tasks\net472\csc.exe`；
2. 或从 Visual Studio / Build Tools 里找 `MSBuild\Current\Bin\Roslyn\csc.exe`；
3. 运行时用 `-CscPath` 指定，或把它放到 `tools\roslyn\tasks\net472\csc.exe`（脚本默认会找这里）。

## 用法

```powershell
# 完整流程：编译 6 个目标 + 运行全部 EditMode 测试
powershell -ExecutionPolicy Bypass -File tools\run_editmode_tests.ps1

# 指定 csc.exe
powershell -ExecutionPolicy Bypass -File tools\run_editmode_tests.ps1 -CscPath X:\path\to\csc.exe

# 已经编译过，只重新跑测试（跳过编译）
powershell -ExecutionPolicy Bypass -File tools\run_editmode_tests.ps1 -SkipBuild
```

成功时最后一行是：

```
Total: 63   Passed: 63   Failed: 0
```

退出码为 0；任一编译错误或测试失败时退出码为 1。

## 编译目标（6 个）

| # | 输出 DLL | 条件编译符号 | 对应 Unity 目标 |
|---|----------|--------------|------------------|
| 1 | UnityEngine.dll | NET_STANDARD | 引擎桩本身 |
| 2 | NUnit.dll | NET_STANDARD | 测试框架桩 |
| 3 | PxC.Game.dll | UNITY_EDITOR;UNITY_2022;UNITY_STANDALONE;NET_STANDARD | 桌面 Editor/Player |
| 4 | PxC.Game.WebGL.dll | UNITY_WEBGL;UNITY_2022;UNITY_WEBGL_BUILD;NET_STANDARD | WebGL |
| 5 | PxC.Tests.EditMode.dll | …UNITY_INCLUDE_TESTS | EditMode 测试 |
| 6 | PxC.Tests.PlayMode.dll | …UNITY_INCLUDE_TESTS | PlayMode 测试（只编译） |

## 它能证明 / 不能证明什么

**能证明（真实执行）：**
- 游戏 C# 代码在桌面与 WebGL 两套条件符号下，语法、类型、程序集引用均无错误；
- EditMode 测试的所有断言真实跑通（63/63），包括存档序列化往返、版本校验、对象池、模拟拓扑等。

**不能证明（必须标注「未验证」）：**
- 真实团结引擎 Editor 编译；
- IL2CPP 转换、WebGL `.wasm` 打包、原生平台构建；
- Unity 官方 Test Runner 的 EditMode / PlayMode 结果；
- 渲染、帧率、物理、内存等运行时表现。

这些需要在装有团结引擎并已登录激活许可证的机器上，用 Editor 完成（见 `docs/` 部署文档）。

## 编码约定

`run_editmode_tests.ps1` 以 **UTF-8 with BOM** 保存：Windows PowerShell 5.1 对无 BOM 的
`.ps1` 会按系统 ANSI 代码页解码，导致其中中文乱码并可能引发解析错误。脚本内的运行时
提示信息统一使用英文，避免任何编码依赖。
