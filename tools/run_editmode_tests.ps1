# 无 Unity 编译 + EditMode 反射测试（自包含，可在没有团结引擎许可的机器上跑）。
# 用法：
#   powershell -ExecutionPolicy Bypass -File tools\run_editmode_tests.ps1
#   powershell -ExecutionPolicy Bypass -File tools\run_editmode_tests.ps1 -CscPath X:\csc.exe
#   powershell -ExecutionPolicy Bypass -File tools\run_editmode_tests.ps1 -SkipBuild
[CmdletBinding()]
param(
    [string]$CscPath = "",
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
$tools = $PSScriptRoot
$repo  = Split-Path $tools -Parent
$stub  = Join-Path $tools 'unity_stub'
$out   = Join-Path $tools 'build_out'
$fw    = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

if (-not $CscPath) {
    $cands = @(
        (Join-Path $tools 'roslyn\tasks\net472\csc.exe'),
        "E:\DB\tools\roslyn480\tasks\net472\csc.exe"
    )
    foreach ($c in $cands) { if (Test-Path $c) { $CscPath = $c; break } }
}
if (-not $CscPath -or -not (Test-Path $CscPath)) {
    Write-Error "csc.exe (Roslyn 4.x) not found. Pass -CscPath, or extract NuGet package Microsoft.Net.Compilers.Toolset and point to tasks\net472\csc.exe."
    exit 2
}
New-Item -ItemType Directory -Force $out | Out-Null

function Collect($dir) {
    Get-ChildItem $dir -Recurse -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '\\[^\\]*~' } |
        Sort-Object FullName
}

function Build-Rsp($dll, $defines, $srcs, $extraRefs, [bool]$IncludeUnity) {
    $refs = @(
        "$fw\mscorlib.dll",
        "$fw\System.dll",
        "$fw\System.Core.dll",
        "$fw\System.Runtime.Serialization.dll"
    )
    if ($IncludeUnity) { $refs += "$out\UnityEngine.dll" }
    if ($extraRefs) { $refs += $extraRefs }
    $lines = @(
        "/target:library",
        "/out:$dll",
        "/langversion:latest",
        "/nologo",
        "/nostdlib+",
        "/define:$defines"
    )
    foreach ($r in $refs) { $lines += "/reference:$r" }
    foreach ($s in $srcs) { $lines += $s.FullName }
    $name = [IO.Path]::GetFileNameWithoutExtension($dll)
    $rsp = Join-Path $out "$name.rsp"
    [IO.File]::WriteAllText($rsp, ($lines -join "`r`n") + "`r`n",
        (New-Object Text.UTF8Encoding($false)))
    return $rsp
}

function Run-Csc($rsp) {
    $err = $rsp -replace '\.rsp$', '_err.txt'
    if (Test-Path $err) { Remove-Item -Force $err }
    # PowerShell 重定向即使在编译器无输出时也会创建文件（管道 Set-Content 可能不创建）。
    & $CscPath "@$rsp" > $err 2>&1
    $code = $LASTEXITCODE
    $txt = [IO.File]::ReadAllText($err)
    if ($txt.Trim().Length -gt 0) { Write-Host $txt }
    return $code
}

if (-not $SkipBuild) {
    $game = Collect (Join-Path $repo 'Assets\Scripts')
    $edit = Collect (Join-Path $repo 'Assets\Tests\EditMode')
    $play = Collect (Join-Path $repo 'Assets\Tests\PlayMode')
    $stubSrc = @()
    foreach ($s in (Collect $stub)) {
        if ($s.Name -ne 'NUnit.cs') { $stubSrc += $s }
    }

    "== 1/6 UnityEngine stub =="
    $c1 = Run-Csc (Build-Rsp "$out\UnityEngine.dll" "NET_STANDARD" $stubSrc @() $false)
    "== 2/6 NUnit stub =="
    $c2 = Run-Csc (Build-Rsp "$out\NUnit.dll" "NET_STANDARD" @(Get-Item "$stub\NUnit.cs") @() $false)
    "== 3/6 Game (desktop) =="
    $c3 = Run-Csc (Build-Rsp "$out\PxC.Game.dll" "UNITY_EDITOR;UNITY_2022;UNITY_STANDALONE;NET_STANDARD" $game @() $true)
    "== 4/6 Game (WebGL) =="
    $c4 = Run-Csc (Build-Rsp "$out\PxC.Game.WebGL.dll" "UNITY_WEBGL;UNITY_2022;UNITY_WEBGL_BUILD;NET_STANDARD" $game @() $true)
    "== 5/6 EditMode =="
    $c5 = Run-Csc (Build-Rsp "$out\PxC.Tests.EditMode.dll" "UNITY_EDITOR;UNITY_2022;UNITY_STANDALONE;NET_STANDARD;UNITY_INCLUDE_TESTS" ($game + $edit) @("$out\NUnit.dll") $true)
    "== 6/6 PlayMode =="
    $c6 = Run-Csc (Build-Rsp "$out\PxC.Tests.PlayMode.dll" "UNITY_2022;UNITY_STANDALONE;UNITY_INCLUDE_TESTS;NET_STANDARD" ($game + $play) @("$out\NUnit.dll") $true)

    "--------------------------------------------------"
    "Build exits  unity=$c1 nunit=$c2 desktop=$c3 webgl=$c4 edit=$c5 play=$c6"
    $sum = [int]$c1 + [int]$c2 + [int]$c3 + [int]$c4 + [int]$c5 + [int]$c6
    if ($sum -ne 0) {
        Write-Error "Build failed"
        exit 1
    }
}

# ---------------- 反射运行 ----------------
# 用 Load(byte[]) 而非 LoadFrom：本机 Application Control (WDAC) 策略
# 会按文件来源拦截 UnityEngine.dll 的 LoadFrom（0x800711C7），字节加载不受此限。
[void][System.Reflection.Assembly]::Load([IO.File]::ReadAllBytes("$out\UnityEngine.dll"))
[void][System.Reflection.Assembly]::Load([IO.File]::ReadAllBytes("$out\NUnit.dll"))
$asm = [System.Reflection.Assembly]::Load([IO.File]::ReadAllBytes("$out\PxC.Tests.EditMode.dll"))

$nunitTest = [NUnit.Framework.TestAttribute]
$nunitSetup = [NUnit.Framework.SetUpAttribute]
$nunitFixture = [NUnit.Framework.TestFixtureAttribute]

$passed = 0; $failed = 0; $testCount = 0
$failures = @(); $fixtures = @()

$allTypes = @()
try { $allTypes = $asm.GetTypes() }
catch [System.Reflection.ReflectionTypeLoadException] {
    foreach ($tt in $_.Exception.Types) { if ($tt -ne $null) { $allTypes += $tt } }
}

foreach ($t in $allTypes) {
    if (-not $t.IsClass -or $t.IsAbstract) { continue }
    $hasFixture = ($t.GetCustomAttributes($nunitFixture, $true).Count -gt 0)
    $hasTest = $false
    foreach ($m in $t.GetMethods()) {
        if ($m.GetCustomAttributes($nunitTest, $true).Count -gt 0) { $hasTest = $true; break }
    }
    if ($hasFixture -or $hasTest) { $fixtures += $t }
}

foreach ($fixture in $fixtures) {
    $setupMethods = @(); $testMethods = @()
    foreach ($m in $fixture.GetMethods()) {
        if ($m.GetCustomAttributes($nunitSetup, $true).Count -gt 0) { $setupMethods += $m }
        if ($m.GetCustomAttributes($nunitTest, $true).Count -gt 0) { $testMethods += $m }
    }
    foreach ($tm in $testMethods) {
        $testCount++
        $label = "$($fixture.Name).$($tm.Name)"
        try {
            $instance = [System.Activator]::CreateInstance($fixture)
            foreach ($sm in $setupMethods) { $sm.Invoke($instance, $null) }
            $tm.Invoke($instance, $null)
            $passed++
        }
        catch {
            $ex = $_.Exception
            while ($ex -is [System.Reflection.TargetInvocationException] -and $ex.InnerException) {
                $ex = $ex.InnerException
            }
            $failed++
            $failures += "FAIL  $label  -> $($ex.GetType().Name): $($ex.Message)"
        }
    }
}

"===== EditMode reflection test results ====="
foreach ($f in $failures) { $f }
"---------------------------------"
"Total: $testCount   Passed: $passed   Failed: $failed"
if ($failed -gt 0) { exit 1 } else { exit 0 }
