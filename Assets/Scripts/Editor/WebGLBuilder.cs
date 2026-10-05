using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace PixelToCivilization.EditorTools
{
    public static class WebGLBuilder
    {
        public const string BuildVer = "9.5.2";
        public const string BuildDir = "BuildWebGL";

        public static void BuildCLI()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string outPath = Path.Combine(root, BuildDir);
            Directory.CreateDirectory(outPath);

            PlayerSettings.productName = "PixelToCivilization V9.5.2";
            PlayerSettings.companyName = "PixelToCivilization";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.memorySize = 512;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.decompressionFallback = true;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;

            string buildScene = FindBuildScene();
            if (string.IsNullOrEmpty(buildScene))
            {
                // Auto create a bootstrap scene with the auto bootstrap code
                buildScene = Path.Combine(root, "Assets", "Scenes", "Bootstrap.unity");
                Directory.CreateDirectory(Path.GetDirectoryName(buildScene));
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
                var go = new GameObject("GameBootstrap");
                go.AddComponent<Bootstrap.GameBootstrap>();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, buildScene);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { buildScene },
                locationPathName = outPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            bool success = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;

            WriteIndexHtml(outPath);
            WriteStarterFiles(outPath);

            Console.WriteLine(success ? "BUILD_SUCCESS" : "BUILD_FAILED");
            if (!success) EditorApplication.Exit(1);
        }

        static string FindBuildScene()
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene");
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith("Bootstrap.unity", StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith("MainScene.unity", StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.Contains("Scenes")) return p;
            }
            return null;
        }

        static void WriteIndexHtml(string dir)
        {
            string html = "<!DOCTYPE html><html lang='zh-CN'><head><meta charset='utf-8'>" +
                "<meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no'>" +
                "<title>从像素到文明 V" + BuildVer + "</title>" +
                "<style>" +
                "body{margin:0;background:#0a0e1a;color:#fff;font-family:'Microsoft YaHei',sans-serif;overflow:hidden}" +
                "#game{width:100vw;height:100vh}" +
                "#loading{position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;background:linear-gradient(135deg,#0a3d62,#1e6091)}" +
                "#bar{width:300px;height:8px;background:#ffffff22;border-radius:4px;overflow:hidden;margin-top:20px}" +
                "#fill{height:100%;background:#ffd700;width:0%;transition:width .2s}" +
                "h1{font-size:24px;margin:0}" +
                "#err{margin-top:20px;padding:16px;background:#00000055;border-radius:8px;max-width:600px;white-space:pre-wrap;font-size:12px;display:none}" +
                "</style></head><body>" +
                "<div id='game'></div>" +
                "<div id='loading'><h1>从像素到文明 V" + BuildVer + "</h1>" +
                "<div style='opacity:.7;margin-top:8px'>正在加载，文明即将开启…</div>" +
                "<div id='bar'><div id='fill'></div></div>" +
                "<div id='err'></div></div>" +
                "<script>" +
                "var s=document.createElement('script');s.src='Build/BuildWebGL.loader.js';" +
                "s.onload=function(){var el=document.getElementById('fill');" +
                "createUnityInstance(document.querySelector('#game'),{dataUrl:'Build/BuildWebGL.data',frameworkUrl:'Build/BuildWebGL.framework.js',codeUrl:'Build/BuildWebGL.wasm',streamingAssetsUrl:'StreamingAssets',companyName:'PixelToCivilization',productName:'PixelToCivilization V" + BuildVer + "',productVersion:'" + BuildVer + "'" +
                ",function(p){el.style.width=p+'%';}" +
                ").then(function(inst){document.getElementById('loading').style.display='none';" +
                "}).catch(function(e){var er=document.getElementById('err');er.style.display='block';er.textContent=e.toString()+'\n\n若直接双击打不开，请用附带的本地服务器脚本（start_webserver）通过 http 方式打开。';});" +
                "}" +
                "document.body.appendChild(s);" +
                "</script></body></html>";
            File.WriteAllText(Path.Combine(dir, "index.html"), html, new System.Text.UTF8Encoding(false));
        }

        static void WriteStarterFiles(string dir)
        {
            string bat = "@echo off\r\nchcp 65001 >nul\r\nset PORT=8000\r\ncd /d %~dp0\r\nstart \"\" \"http://127.0.0.1:%PORT%/index.html\"\r\npython -m http.server %PORT% --bind 127.0.0.1\r\nif errorlevel 1 (py -3 -m http.server %PORT% --bind 127.0.0.1)\r\npause\r\n";
            File.WriteAllText(Path.Combine(dir, "start_webserver.bat"), bat, new System.Text.UTF8Encoding(false));

            string command = "#!/bin/bash\ncd \"$(dirname \"$0\")\"\nPORT=8000\n(open \"http://127.0.0.1:$PORT/index.html\" || true)\nif command -v python3 >/dev/null 2>&1; then python3 -m http.server $PORT --bind 127.0.0.1;\nelif command -v python >/dev/null 2>&1; then python -m http.server $PORT --bind 127.0.0.1;\nfi\n";
            string cmdPath = Path.Combine(dir, "start_webserver.command");
            File.WriteAllText(cmdPath, command, new System.Text.UTF8Encoding(false));
        }

        [MenuItem("像素到文明/一键 WebGL 构建")]
        public static void BuildMenu()
        {
            BuildCLI();
        }

        [MenuItem("像素到文明/设置 WebGL 推荐配置")]
        public static void ConfigureMenu()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.memorySize = 512;
            PlayerSettings.WebGL.decompressionFallback = true;
            EditorUtility.DisplayDialog("WebGL 配置", "已设置为 V" + BuildVer + " 推荐配置：Gzip 压缩、显式异常、数据缓存、512MB 内存、解压回退。", "确定");
        }
    }
}
