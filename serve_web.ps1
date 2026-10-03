$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
function Get-FreePort($start){
  for($pp=$start;$pp -lt ($start+80);$pp++){
    try{ $tl=New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback,$pp); $tl.Start(); $tl.Stop(); return $pp }catch{ }
  }
  return $start
}
$port = Get-FreePort 8000
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$port/")
$listener.Start()
$mime = @{ '.html'='text/html; charset=utf-8';'.js'='application/javascript; charset=utf-8';'.wasm'='application/wasm';
  '.data'='application/octet-stream';'.json'='application/json';'.css'='text/css';'.png'='image/png';'.ico'='image/x-icon';
  '.br'='application/brotli';'.gz'='application/gzip';'.mem'='application/octet-stream';'.bin'='application/octet-stream';'.txt'='text/plain; charset=utf-8' }
Write-Host "服务器已启动: http://localhost:$port/  关闭窗口即停止" -ForegroundColor Yellow
Start-Process "http://localhost:$port/"
while ($listener.IsListening) {
  try {
    $ctx = $listener.GetContext()
    $url = [System.Uri]::UnescapeDataString($ctx.Request.Url.AbsolutePath.TrimStart('/'))
    if ([string]::IsNullOrEmpty($url)) { $url = 'index.html' }
    $path = Join-Path $root ($url -replace '/','\')
    if ((Test-Path $path -PathType Container)) { $path = Join-Path $path 'index.html' }
    if (Test-Path $path -PathType Leaf) {
      $ext = [System.IO.Path]::GetExtension($path).ToLower()
      $ctx.Response.ContentType = if ($mime.ContainsKey($ext)) { $mime[$ext] } else { 'application/octet-stream' }
      $bytes = [System.IO.File]::ReadAllBytes($path)
      $ctx.Response.ContentLength64 = $bytes.Length
      $ctx.Response.Headers.Add('Cross-Origin-Opener-Policy','same-origin')
      $ctx.Response.Headers.Add('Cross-Origin-Embedder-Policy','require-corp')
      $ctx.Response.OutputStream.Write($bytes,0,$bytes.Length); $ctx.Response.OutputStream.Close()
    } else { $ctx.Response.StatusCode=404; $ctx.Response.OutputStream.Close() }
  } catch { break }
}
