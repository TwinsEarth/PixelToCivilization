@echo off
chcp 936 >nul
title PixelToCivilization V9.3.10 Web Server
cd /d "%~dp0"
rem V6.7.1: auto pick free port so a stale old server cannot hijack 8000
set PORT=8000
:findport
netstat -ano -p tcp | findstr /R /C:":%PORT% .*LISTENING" >nul 2>nul && set /a PORT+=1 && goto findport
echo ================================================
echo   从像素到文明 V9.3.10 · 本地网页服务器
echo   URL: http://localhost:%PORT%/
echo   (8000 被旧版本占用时自动顺延到下一端口)
echo   关闭本窗口即停止服务
echo ================================================
where py >nul 2>nul
if %errorlevel%==0 (
  start "" "http://localhost:%PORT%/"
  py -m http.server %PORT%
  goto :end
)
where python >nul 2>nul
if %errorlevel%==0 (
  start "" "http://localhost:%PORT%/"
  python -m http.server %PORT%
  goto :end
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0serve_web.ps1"
:end
pause
