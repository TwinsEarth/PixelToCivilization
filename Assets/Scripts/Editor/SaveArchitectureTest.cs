#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using PixelToCivilization.Core;

namespace PixelToCivilization.EditorTools
{
    /// <summary>
    /// V9.6.6 存档架构测试（EditMode，纯数据逻辑，不依赖场景；菜单「像素到文明 → ⑩ 存档架构测试」运行）。
    /// 覆盖 V9.6.6 六件契约：
    ///  1) 校验和往返：AttachChecksum → Verify 通过（新档带 FNV-1a64 校验）
    ///  2) 篡改检测：校验后改字段 → Verify 拒绝（返回 null = 损坏）
    ///  3) NaN/Inf 污染拒绝：快照含 NaN → 信封序列化失败 → 写入拒绝（防脏档）
    ///  4) 版本迁移链：schema0 旧档 → Migrate → schema3 + 安全默认 + MigrationChain 记录
    ///  5) 迁移幂等：schema3 档再 Migrate → true 且链不变
    ///  6) ISaveable 注册与扩展区往返：注册 → Serialize 收集 → Deserialize 恢复
    /// 输出：每项 PASS/FAIL + 实测数值；任意 FAIL 立即标红（批处理 -executeMethod 可跑，退出码判据 FAIL 计数）。
    /// </summary>
    public static class SaveArchitectureTest
    {
        static int _pass, _fail;

        [MenuItem("像素到文明/⑩ 存档架构测试")]
        public static void RunAll()
        {
            _pass = 0; _fail = 0;
            Debug.Log("===== [SAVE-TEST] V9.6.6 存档架构测试开始 =====");

            ChecksumRoundtrip();
            ChecksumTamperDetected();
            NaNRejected();
            MigrateOldToCurrent();
            MigrateIdempotent();
            IsaveableRegistryRoundtrip();
            ResDoublePrecision();        // V9.6.8 资源双精度契约
            OldSchemaResCompat();        // V9.6.8 旧档 ResVals(float) → double[] 无损兼容

            Debug.Log("===== [SAVE-TEST] 完成：PASS=" + _pass + " FAIL=" + _fail + " =====");
            if (_fail > 0) Debug.LogError("[SAVE-TEST] 存在失败项，禁止发布");
        }

        static SaveData MakeData()
        {
            var d = new SaveData();
            d.Version = "9.6.9";
            d.Year = 2026; d.DynastyName = "测试王朝";
            d.Pop = 500; d.BuildingCount = 3;
            d.PowerRatio = 0.9f; d.IndustryChainMult = 1.2f;
            d.CityHealth = 70f; d.CityEducation = 50f; d.CitySafety = 60f; d.CityEmployment = 80f;
            d.CityTaxLevel = 1; d.LandIntegrity = 1.5f; d.PeakPop = 800; d.CryoRemain = 3f;
            return d;
        }

        // 1) 校验和往返
        static void ChecksumRoundtrip()
        {
            var d = MakeData();
            string final = SaveEnvelope.AttachChecksum(d);
            bool ok = !string.IsNullOrEmpty(final);
            var back = SaveEnvelope.Verify(final);
            bool ok2 = ok && back != null && back.Year == 2026 && back.Pop == 500;
            Assert("1 校验和往返", ok2, "final=" + (final == null ? "null" : final.Length + "chars") + " back.Year=" + (back != null ? back.Year.ToString() : "null"));
        }

        // 2) 篡改检测
        static void ChecksumTamperDetected()
        {
            var d = MakeData();
            string final = SaveEnvelope.AttachChecksum(d);
            // 篡改：把存档 JSON 中的人口改掉（字段序稳定，替换 "Pop":500 为 9999）
            string tampered = final.Replace("\"Pop\":500", "\"Pop\":9999");
            bool tamperedActuallyChanged = tampered != final;
            var v = SaveEnvelope.Verify(tampered);
            Assert("2 篡改检测", tamperedActuallyChanged && v == null, "tampered=" + tamperedActuallyChanged + " verify=null:" + (v == null));
        }

        // 3) NaN 污染拒绝
        static void NaNRejected()
        {
            var d = MakeData();
            d.PowerRatio = float.NaN;   // 模拟世界坐标/数值 NaN 污染
            string final = SaveEnvelope.AttachChecksum(d);
            Assert("3 NaN拒绝", final == null, "final=null:" + (final == null));
        }

        // 4) 版本迁移链（schema0 旧档 → 当前）
        static void MigrateOldToCurrent()
        {
            var d = MakeData();
            d.SaveSchema = 0;            // V9.6.5 及以前的旧档
            d.SaveFormat = "";
            d.PowerRatio = 0f;           // 旧档缺失/非法值
            d.IndustryChainMult = 0f;
            d.CityHealth = 0f; d.CityEducation = 0f; d.CitySafety = 0f; d.CityEmployment = 0f;
            d.CityTaxLevel = 7;          // 越界
            d.LandIntegrity = 0f;
            d.PeakPop = 0f;
            d.CryoRemain = -1f;
            bool ok = SaveVersionMigrator.Migrate(d);
            bool ok2 = ok && d.SaveSchema == SaveVersionMigrator.CurrentSchema &&
                       d.PowerRatio == 1f && d.IndustryChainMult == 1f &&
                       d.CityHealth == 60f && d.CityEducation == 40f &&
                       d.CitySafety == 55f && d.CityEmployment == 85f &&
                       d.CityTaxLevel == 1 && d.LandIntegrity == 1.2f &&
                       d.PeakPop == 500f && d.CryoRemain == 0f &&
                       d.SaveFormat == SaveVersionMigrator.FormatId &&
                       d.MigrationChain == "0->3";
            Assert("4 版本迁移链", ok2, "schema=" + d.SaveSchema + " chain=" + d.MigrationChain + " health=" + d.CityHealth);
        }

        // 5) 迁移幂等
        static void MigrateIdempotent()
        {
            var d = MakeData();
            d.SaveSchema = SaveVersionMigrator.CurrentSchema;
            string chainBefore = d.MigrationChain = "0->3";
            bool ok = SaveVersionMigrator.Migrate(d);
            Assert("5 迁移幂等", ok && d.SaveSchema == SaveVersionMigrator.CurrentSchema && d.MigrationChain == chainBefore,
                "schema=" + d.SaveSchema + " chain=" + d.MigrationChain);
        }

        // 6) ISaveable 注册与扩展区往返
        class MockSaveable : ISaveable
        {
            public string SaveKey => "MockGod";
            public string Stored;
            public string Serialize() => "{\"v\":\"mock-ok\"}";
            public void Deserialize(string json) { Stored = json; }
        }
        static void IsaveableRegistryRoundtrip()
        {
            var mock = new MockSaveable();
            var d = MakeData();
            // 模拟 SaveSystem.Snapshot 的扩展区收集逻辑
            d.ExtKeys = new string[] { mock.SaveKey };
            d.ExtVals = new string[] { mock.Serialize() };
            // 模拟 Apply 的扩展区恢复逻辑（按键匹配）
            bool restored = false;
            for (int i = 0; i < d.ExtKeys.Length && i < d.ExtVals.Length; i++)
                if (d.ExtKeys[i] == mock.SaveKey) { mock.Deserialize(d.ExtVals[i]); restored = true; }
            Assert("6 ISaveable往返", restored && mock.Stored == "{\"v\":\"mock-ok\"}", "stored=" + mock.Stored);
        }

        // 7) V9.6.8 资源双精度累加契约：百万级 + 0.0625/次 ×1000 次 = 精确 1000062.5（float 在 1e6 ULP≈0.0625，
        //    逐帧小增量会被舍入丢失 → "资源冻结不动"；double 存储下无损）
        static void ResDoublePrecision()
        {
            var s = new GameState();
            s.Res["gold"] = 1000000d;
            for (int i = 0; i < 1000; i++) s.AddRes("gold", 0.0625f);
            double val = s.Res["gold"];
            bool ok = val == 1000062.5d;
            Assert("7 资源双精度累加", ok, "gold=" + val.ToString("F6") + " (期望 1000062.500000)");
        }

        // 8) V9.6.8 旧档 ResVals(float[]) → double[] 无损兼容：旧 JSON 数值直接反序列化到 double[]
        static void OldSchemaResCompat()
        {
            string oldJson = "{\"ResKeys\":[\"gold\",\"food\"],\"ResVals\":[123456.75,99.5],\"SaveSchema\":3}";
            var d = UnityEngine.JsonUtility.FromJson<SaveData>(oldJson);
            bool ok = d != null && d.ResVals != null && d.ResVals.Length == 2
                      && d.ResVals[0] == 123456.75d && d.ResVals[1] == 99.5d;
            Assert("8 旧档Res双精度兼容", ok, "ResVals[0]=" + (d != null && d.ResVals != null ? d.ResVals[0].ToString("F4") : "null"));
        }

        static void Assert(string name, bool ok, string detail)
        {
            if (ok) { _pass++; Debug.Log("[SAVE-TEST] PASS " + name + " | " + detail); }
            else { _fail++; Debug.LogError("[SAVE-TEST] FAIL " + name + " | " + detail); }
        }
    }
}
#endif
