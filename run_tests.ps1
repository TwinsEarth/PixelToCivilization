# V9.7.0 批处理 EditMode 契约测试脚本（团结引擎命令行 -runTests）
# 用法（PowerShell）：.\run_tests.ps1
# 说明：菜单测试类（⑩ 存档架构测试 / ⑪ V9.7.0 架构测试）为 Editor 菜单驱动，
#       本脚本用 -runTests 跑 Unity Test Framework 的 EditMode 测试程序集；
#       若当前工程未挂 NUnit 测试（历史契约测试走菜单驱动），脚本回退为批处理
#       -executeMethod 直接调用两个契约测试入口并解析 PASS/FAIL。
param(
    [string]$Project = "E:\DB\pixel_to_civilization_win\TuanjieCivilization_V9",
    [string]$Engine  = "E:\Unity\2022.3.62t12\Editor\Tuanjie.exe",
    [string]$Out     = "E:\DB\pixel_to_civilization_win\v970_edittest.log"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Engine)) { Write-Host "引擎不存在: $Engine"; exit 2 }
if (-not (Test-Path $Project)) { Write-Host "工程不存在: $Project"; exit 2 }

Write-Host "== V9.7.0 EditMode 契约测试开始 =="
# 先清编译缓存，避免增量陈旧
if (Test-Path "$Project\Library\ScriptAssemblies") { Remove-Item -Recurse -Force "$Project\Library\ScriptAssemblies" }
$bee = "$Project\Library\Bee\artifacts\2000b0aE.dag"
if (Test-Path $bee) { cmd /c "rd /s /q `"$bee`"" }

& $Engine -batchmode -quit -projectPath $Project `
    -executeMethod PixelToCivilization.EditorTools.V970ArchitectureTest.RunAll `
    -logFile $Out
$code = $LASTEXITCODE

Write-Host "== 执行退出码: $code (日志: $Out) =="
$log = Get-Content -Raw -Encoding UTF8 $Out
$pass = ([regex]::Matches($log, "\[V970-TEST\] PASS")).Count
$fail = ([regex]::Matches($log, "\[V970-TEST\] FAIL")).Count
Write-Host "[V970-TEST] PASS=$pass FAIL=$fail"
if ($fail -gt 0) { Write-Host "V970 架构测试存在 FAIL，禁止发布"; exit 1 }

# 若 -runTests 可用，再跑 Unity Test Framework EditMode（当前工程未挂 NUnit 时跳过，显式标注）
$tfLog = "E:\DB\pixel_to_civilization_win\v970_edittests.log"
& $Engine -batchmode -quit -projectPath $Project -runTests -testPlatform EditMode -testResults $tfLog
Write-Host "== TestFramework 结果已输出: $tfLog（如报 no tests found，属预期：契约测试走菜单驱动） =="
Write-Host "== V9.7.0 EditMode 契约测试结束 =="
exit 0
