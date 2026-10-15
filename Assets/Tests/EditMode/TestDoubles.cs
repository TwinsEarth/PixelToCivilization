using System;
using System.Collections.Generic;
using System.IO;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Core.Services;

namespace PxC.Tests.EditMode
{
    /// <summary>可配置的假模拟插件。</summary>
    public sealed class FakePlugin : ISimPlugin
    {
        public string Id { get; }
        public string DisplayName => Id;
        public int Version { get; set; } = 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.Core0;
        public SimulationLayer Layer => SimulationLayer.P0Core;

        public IReadOnlyList<PluginDependency> Dependencies { get; set; }
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities { get; set; }

        public bool Registered;
        public bool Initialized;
        public bool Shutdown;
        public bool InitializeShouldFail;
        public int Ticks;
        public int Years;

        public FakePlugin(string id, List<PluginDependency> deps = null, List<CapabilityClaim> caps = null)
        {
            Id = id;
            Dependencies = deps;
            RequiredCapabilities = caps;
        }

        public void OnRegister(IPluginContext context) => Registered = true;
        public void OnInitialize()
        {
            Initialized = true;
            if (InitializeShouldFail) throw new InvalidOperationException("fake init failure");
        }
        public void OnShutdown() => Shutdown = true;

        public void OnTick(float simDt) => Ticks++;
        public void OnYear(int gameYear) => Years++;
        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }
    }

    /// <summary>构造依赖列表。</summary>
    public static class Deps
    {
        public static List<PluginDependency> Of(params (string id, int ver)[] items)
        {
            var list = new List<PluginDependency>(items.Length);
            foreach (var it in items)
                list.Add(new PluginDependency { Id = it.id, MinVersion = it.ver });
            return list;
        }
    }

    /// <summary>构造能力声明。</summary>
    public static class Caps
    {
        public static List<CapabilityClaim> Of(params Capability[] items)
        {
            var list = new List<CapabilityClaim>(items.Length);
            foreach (var c in items)
                list.Add(new CapabilityClaim { Capability = c });
            return list;
        }
    }

    /// <summary>临时目录（测试结束自动删除）。</summary>
    public sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "pxc_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path)) Directory.Delete(Path, true);
            }
            catch { }
        }
    }

    /// <summary>测试用存档迁移。</summary>
    public sealed class FakeMigration : ISaveMigration
    {
        public int FromVersion { get; }
        public int ToVersion { get; }

        public FakeMigration(int from, int to)
        {
            FromVersion = from;
            ToVersion = to;
        }

        public SaveData Migrate(SaveData data) => data;
    }
}
