@echo off
chcp 936 >nul
title PixelToCivilization V9.3.12 Web Server
cd /d "%~dp0"
rem V6.7.1: auto pick free port so a stale old server cannot hijack 8000
set PORT=8000
:findport
netstat -ano -p tcp | findstr /R /C:":%PORT% .*LISTENING" >nul 2>nul && set /a PORT+=1 && goto findport
echo ================================================
echo   �����ص����� V9.3.12 �� ������ҳ������
echo   URL: http://localhost:%PORT%/
echo   (8000 ���ɰ汾ռ��ʱ�Զ�˳�ӵ���һ�˿�)
echo   �رձ����ڼ�ֹͣ����
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
