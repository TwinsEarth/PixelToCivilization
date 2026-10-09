<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>实验性:</b> 此功能目前是试验性的，在以后的主要版本中可能会发生变化。要使用此功能，请在工程 Preferences 的 <b>Visual Effects</b> 选项卡中启用 <b>Experimental Operators/Blocks</b>。</div>

# Spawner 回调

Spawner Callbacks 是一个 C# API，允许您定义自定义运行时行为并创建新块以在 Spawn 上下文中使用。

Spawner 回调允许您：

* 控制 Spawn Context 状态（Playing、Stopped、Delayed）。
* 读/写 Output Spawn Count。
* 读/写 SpawnEvent 属性。

## 编写 Spawner 回调

完整的 Spawner Callbacks API 参考可在[此处](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXSpawnerCallbacks.html)获得。
