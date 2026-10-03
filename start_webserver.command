#!/bin/bash
# 从像素到文明 V9.3.11 - macOS 本地网页服务器
cd "$(dirname "$0")" || exit 1
PORT=8000
while lsof -iTCP:$PORT -sTCP:LISTEN -nP >/dev/null 2>&1; do PORT=$((PORT+1)); done
LANIP="$(ipconfig getifaddr en0 2>/dev/null)"
echo "================================================ "
echo "  从像素到文明 V9.3.11 · 本地网页服务器"
echo "  本机浏览器: http://localhost:$PORT/"
if [ -n "$LANIP" ]; then echo "  手机同网段: http://$LANIP:$PORT/  (默认横屏全屏)"; fi
echo "  关闭本窗口即停止服务"
echo "================================================ "
( sleep 1; open "http://localhost:$PORT/" ) >/dev/null 2>&1 &
if command -v python3 >/dev/null 2>&1; then
  exec python3 -m http.server "$PORT" --bind 0.0.0.0
fi
echo ""
echo "未检测到 python3。请任选其一后重试："
echo "  1) 终端执行一次: xcode-select --install  (安装苹果命令行工具)"
echo "  2) 已装 Homebrew: brew install python"
echo "  3) 或用任意静态服务器托管本文件夹"
echo ""
echo "按回车关闭窗口..."; read -r
