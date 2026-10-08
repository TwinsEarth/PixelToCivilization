# V9.8.1 部署指南（Deployment Guide）

## 产物
- 发布包：`PixelToCivilization_V9.8.1_HTML5.zip`（54.6MB）
- 构建目录：`TuanjieCivilization_V9\BuildWebGL`（22 文件）
- 本地部署目录：`v981_web`

## 运行方式（用户侧）

### Windows
1. 解压 zip。
2. 双击 `start_webserver.bat`，浏览器自动打开 `http://127.0.0.1:8000/index.html`。

### macOS
1. 解压 zip。
2. 终端执行 `chmod +x start_webserver.command` 后双击。
3. 若提示"无法打开/已损坏"：系统设置 → 隐私与安全性 → 允许，或 `xattr -dr com.apple.quarantine start_webserver.command`。

### 任意系统（手动）
```
python3 -m http.server 8000
# 浏览器访问 http://127.0.0.1:8000/index.html
```
> WebGL 不能以 file:// 直接打开 index.html，必须经本地 HTTP 服务。

## 本地回归部署（开发侧）
```
E:\DB\pixel_to_civilization_win\v981_web          # 部署目录（22 文件）
python -m http.server 8064 --directory v981_web   # 已用端口 8064
浏览器访问 http://127.0.0.1:8064/index.html?nc=v981r1
```

## 从源码构建（WebGL）
```
& "E:\Unity\2022.3.62t12\Editor\Tuanjie.exe" `
  -batchmode -quit `
  -projectPath "<仓库路径>" `
  -executeMethod PixelToCivilization.EditorTools.WebGLBuilder.BuildCLI `
  -logFile build.log
```
产物输出 `BuildWebGL/`。

## 版本缓存说明
index.html 内 `pxc_build_ver=9.8.1-021609`：浏览器首次加载会自动清空 IndexedDB（UnityCache），避免旧包缓存导致的"进去了还是旧版本"问题。

## 构建日志
`E:\DB\pixel_to_civilization_win\v981_build.log`（二次构建 exit=0）
