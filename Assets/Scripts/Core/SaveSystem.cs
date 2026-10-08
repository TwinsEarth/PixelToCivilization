using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;
using PixelToCivilization.Platform;

namespace PixelToCivilization.Core
{
    /// <summary>可序列化存档数据（JsonUtility不支持Dictionary，用平行数组）。V6.1.2 对齐 v5.9.9 全量快照。</summary>
    [Serializable]
    public class SaveData
    {
        public string Version="9.8.0";
        public string SlotName="手动存档";
        public string DynastyName="";
        public int Year; public float Day; public int Era, DynastyIdx;
        public int Pop,MaxPop; public float Happiness, Housing, DynastyMorale;
        public float Speed; public int DebugLevel;
        public string RallyKind; public float RallyX, RallyZ;   // V9.6.0 紧急集结令（插旗）
        public float Children,Young,Middle,Old;
        // 军事 / 研究 / 社会
        public float MilSoldiers,MilCavalry,MilFirepower,MilDefense;
        public bool WarActive,Victory; public string VictoryType;
        public float ResearchProgress; public string CurrentResearch;
        public string[] SocialKeys; public float[] SocialVals;
        public string[] ResKeys; public double[] ResVals;   // V9.6.8 资源双精度（旧档 float 值 JSON 数值无损兼容，schema 保持 3）
        public string[] Techs; public string[] Policies;
        // 运河 / 潮汐 / 电力
        public int CanalSegments; public float CanalBonus,AiBonus,ElectricGrid,PowerCoverage;
        public float PowerSupply,PowerDemand,PowerRatio,Pollution,IndustryChainMult; // V9.0.4
        public float CityHealth,CityEducation,CitySafety,CityEmployment; // V9.0.5 城市指标
        public int CityTaxLevel; // V9.0.7 城市财政税率档位
        public bool CanalAutoBuild; public float CanalBuildTimer,TidePhase,TideLevel; public bool TideHigh; public int[] CanalCells;
        public int[] BridgeCells; public int[] BridgeRuns;   // V6.3.9 桥梁
        public float[] Piers;   // V9.4.6 高架柱（成对 x,z）
    public float[] GrownLands;   // V6.5.4 运行时实时增陆（每块7浮点 cx,cz,br,kind,p1,p2,p3）
        // 太空
        public float SpElevator,SpShips,SpDyson,SpLunar,SpMars;
        // 海洋 / 太空副本
        public bool OceanUnlocked,SpaceUnlocked;
        public string[] OceanDiscovered;
        public string[] OceanResKeys,SpaceResKeys; public float[] OceanResVals,SpaceResVals;
        // 船只（我方）与车辆
        public int ShipCount; public string[] ShipType; public float[] ShipX,ShipZ; public int[] ShipLvl; public bool[] ShipMil;
        public int CartCount; public string[] CartType; public float[] CartX,CartZ; public int[] CartLvl;
        // 人口（位置+家园+阶层+职业）
        public int AgentCount; public float[] AgentX,AgentZ,AgentHX,AgentHZ; public string[] AgentClass,AgentJob; public int[] AgentAge,AgentSeed; // V7.0.2
        // 建筑
        public int BuildingCount;
        public string[] BType; public float[] BX,BZ; public int[] BLvl; public int[] BAge; public float[] BHp;
        // V6.1.3 全要素：地形种子（读档还原同一张大地图）/ 散树 / 地图延展 / 大航海·宇宙里程碑
        public int TerrainSeed;
        public bool EarthMode;   // V9.1.0 存档地图模式（经典随机/真实地球）
        public int TreeCount; public float[] TreeX,TreeZ; public int[] TreeStage,TreeAge;
        public float WorldExpansion; public bool AgeOfSail,AgeOfSpace;
        // V6.1.3 多聚落 / 天下分合（JsonUtility 用平行数组）
        public string WorldPhase; public int PhaseYearsLeft,PlayerNationId,VilCount,NationCount;
        public float[] VilX,VilZ;
        public int[] NId; public string[] NName,NHex; public float[] NX,NZ; public int[] NPop; public bool[] NPlayer,NAlive; public int[] NFaction;   // V9.1.1 阵营
        public string[] NNote;   // V9.1.1 首都名
        // V6.1.3 全要素补全：诸子百家学派 / 科举 / 吏治腐败与君主 / 已触发历史事件(防重复领奖) / 船员与船血量 / 农田阶段
        public string Philosophy, MonarchName;
        public bool SchoolFounded, MonarchWise;
        public float Corruption;
        public string[] FiredEvents;
        public int[] ShipCrew, BFarmStage;
        public float[] ShipHp;
        // V6.1.4 我方步骑机动部队（视图读档后由军事系统重建）
        public int[] FuKind; public float[] FuX,FuZ,FuHp;
        public int FactionAnnexTimer;
        // V6.1.5 殖民地
        public int ColCount; public string[] ColId,ColName,ColRes; public int[] ColLvl;
        public float[] ColPop,ColLoy,ColX,ColZ; public bool ColonialAge;
        // V6.1.6 海洋/太空探索副本网格（嵌套可序列化，含迷雾/节点/位置/战力补给）
        public ExpeditionState OceanExpData, SpaceExpData;
        // V6.1.8 九智能体共治（开关/模式/Token/议政节奏/兜底次数/各神运行态）
        public bool AIEnabled=true, AIOnline; public long AITokens;
        public int AIInterval,AILastCouncil,AISafety; public string AIApiKey,AIModel;
        public int[] AIGodLast; public long[] AIGodAct;
        // V9.4.0 性格向量（Zeal/Intervene/Expand/Prudent）+ 文明历史日志（环形 32 条）
        public float[] AIGodZeal, AIGodIntv, AIGodExp, AIGodPrd; public string[] AiHistory;
        // V6.1.9 加速冷冻运行态（累计年数/是否冷冻/剩余现实秒）
        public float CryoAccum,CryoRemain; public bool CryoActive;
        // V9.2.2 人口周期律（马尔萨斯四阶段）
        public float LandIntegrity, PeakPop; public int DynastyAge, CyclePhase;
        // V6.8.0 世界奇观 / 文明成就（平行数组，视图读档后由 WonderSystem 自愈重建）
        public int WonderCount; public string[] WId; public int[] WYear; public float[] WX,WZ;
        public string[] Achievements; public bool WonderAuto;
        public long SaveTime;
        // V9.6.6 存档架构：schema 版本 / 格式标识 / 校验和 / 迁移链 / ISaveable 扩展区
        public int SaveSchema;                 // 0=旧档（V9.6.5 及以前）→ 3=当前（SaveVersionMigrator.CurrentSchema）
        public string SaveFormat;              // "PxC-SAVE-V1"
        public string ChecksumHex;             // FNV-1a 64 over 排除本字段的完整 JSON
        public string MigrationChain;          // 已应用迁移记录，如 "0->3"
        public string[] ExtKeys;               // ISaveable 注册系统键
        public string[] ExtVals;               // ISaveable 序列化 JSON
    }

    /// <summary>槽位摘要（供存档列表渲染，不反序列化全部）</summary>
    public class SlotSummary
    {
        public int Slot; public bool Exists,Damaged;
        public string Name,Dynasty; public int Year,BuildingCount; public long Time;
    }

    /// <summary>V9.6.4 内存架构：槽位摘要（极小 JSON，列表/最近槽只反序列化它，不再全量解析 SaveData）。
    /// 与完整档同写同删；旧档无摘要时 Summarize/LatestSlot 自动回退全量解析，行为兼容。</summary>
    [Serializable]
    public class SlotSummaryData
    {
        public int Slot;
        public int Year;
        public string DynastyName="";
        public int BuildingCount;
        public long SaveTime;
        public string SlotName="";
    }

    /// <summary>
    /// 存档系统 —— V6.1.2 对齐 v5.9.9：自动槽(auto,每5分钟现实时间)+5 个手动槽，
    /// 每槽可覆盖/读档/删除，列表显示年份·朝代·建筑数·时间；支持 JSON 导出/导入，全量快照。
    /// 槽位编号：0=自动槽，1..5=手动槽。
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        private GameManager _gm;
        private float _autoTimer;
        public const float AutoSaveInterval = 300f;   // v5.9.9：现实 5 分钟
        /// <summary>V9.7.3 运行时应用版本：写入存档 Version 字段，供版本追踪/迁移与崩溃报告定位。</summary>
        public const string RuntimeVersion = "9.8.0";
        public const int ManualSlots = 5;
        // V9.6.5 崩溃架构：回滚槽（自动档滚动保存，崩溃后依次回退）
        public const int RollbackSlots = 3;           // rollback0/1/2
        public float AutoCountdown => Mathf.Max(0f, AutoSaveInterval-_autoTimer);

        // ---------- V9.6.6 存档架构 ----------
        private readonly List<ISaveable> _saveables = new List<ISaveable>();
        private readonly Queue<PendingSave> _pendingEncode = new Queue<PendingSave>();
        private readonly object _pendingLock = new object();          // 异步入队（桌面端后台线程）与主线程提交的互斥
        private bool _busyAsync;
        private sealed class PendingSave { public int Slot; public string JsonNoChecksum; public string SumJson; public string Hex; }

        /// <summary>注册可存档系统（ISaveable）。重复注册同一 SaveKey 覆盖。</summary>
        public void Register(ISaveable s)
        {
            if (s == null) return;
            _saveables.RemoveAll(x => x.SaveKey == s.SaveKey);
            _saveables.Add(s);
        }

        /// <summary>V9.6.6 浏览器探针：槽位存在位/schema/校验状态/临时键/备份键/回滚槽（回归用）。</summary>
        public string WebSaveProbe()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("slot:");
            for (int i = 0; i <= ManualSlots; i++) sb.Append(HasSlot(i) ? "1" : "0");
            sb.Append("|roll:");
            for (int i = 0; i < RollbackSlots; i++) sb.Append(PxcStorage.BodyHasKey(RollKey(i)) ? "1" : "0");
            sb.Append("|schema:");
            int anySchema = 0;
            for (int i = 0; i <= ManualSlots; i++)
                if (HasSlot(i)) { var s = ReadSchemaForTest(i); if (s > 0) { anySchema = s; break; } }
            sb.Append(anySchema);
            sb.Append("|tmp:");
            bool hasTmp = false;
            for (int i = 0; i <= ManualSlots; i++) if (PxcStorage.BodyHasKey("PxC_Tmp_" + i)) hasTmp = true;
            sb.Append(hasTmp ? "1" : "0");
            sb.Append("|bak:");
            int baks = 0;
            for (int i = 1; i <= ManualSlots; i++) if (PxcStorage.BodyHasKey("PxC_Bak_" + i)) baks++;
            sb.Append(baks);
            return sb.ToString();
        }
        /// <summary>读取槽位存档 schema 版本（0=旧档/空；用于探针与迁移检查）。</summary>
        public int ReadSchemaForTest(int slot)
        {
            if (!HasSlot(slot)) return 0;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(PxcStorage.BodyGetString(Key(slot)));
                return d != null ? d.SaveSchema : 0;
            }
            catch { return 0; }
        }

        /// <summary>V9.6.6 启动清理：删除残留临时键（原子写中断留下的 PxC_Tmp_*）。</summary>
        public void CleanupTempKeys()
        {
            for (int i = 0; i <= ManualSlots; i++)
                if (PxcStorage.BodyHasKey("PxC_Tmp_" + i)) PxcStorage.BodyDeleteKey("PxC_Tmp_" + i);
            PxcStorage.BodyFlush();
            PlayerPrefs.Save();
        }

        /// <summary>V9.7.3 存档逐步诊断：定位 SaveToSlot 静默失败的具体步骤（存储写链路 / 快照 / 信封 / 校验）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebSaveDiag()
        {
            Debug.Log("[SAVE_DIAG] step0 ready=" + PxcStorage.Ready + " hybrid=" + PxcStorage.HybridMode
                      + " gm=" + (_gm != null) + " state=" + (_gm != null && _gm.State != null));
            // 1) 存储写链路（独立诊断键，验证 BodySet/Has/Get 三者闭环）
            try
            {
                PxcStorage.BodySetString("PxC_Diag", "diagbody");
                PxcStorage.BodyFlush();
                bool has = PxcStorage.BodyHasKey("PxC_Diag");
                string back = PxcStorage.BodyGetString("PxC_Diag", "");
                Debug.Log("[SAVE_DIAG] step1 storage has=" + has + " back=" + back);
                PxcStorage.BodyDeleteKey("PxC_Diag");
                PxcStorage.BodyFlush();
            }
            catch (Exception e) { Debug.LogError("[SAVE_DIAG] step1 EX " + e); }
            // 2) 快照
            SaveData data = null;
            try
            {
                data = Snapshot();
                Debug.Log("[SAVE_DIAG] step2 snapshot ok year=" + data.Year + " b=" + data.BuildingCount + " agents=" + data.AgentCount);
            }
            catch (Exception e) { Debug.LogError("[SAVE_DIAG] step2 EX " + e); return; }
            // 3) 信封 + 4) 校验往返
            try
            {
                string final = SaveEnvelope.AttachChecksum(data);
                Debug.Log("[SAVE_DIAG] step3 envelope null=" + (final == null) + " len=" + (final != null ? final.Length : 0));
                if (final != null)
                {
                    var verified = SaveEnvelope.Verify(final);
                    Debug.Log("[SAVE_DIAG] step4 verify null=" + (verified == null)
                              + (verified != null ? " year=" + verified.Year : ""));
                    if (verified == null)
                    {
                        // 详细对比：定位 checksum 不匹配的首处差异
                        var d2 = JsonUtility.FromJson<SaveData>(final);
                        string saved = d2.ChecksumHex ?? "";
                        d2.ChecksumHex = "";
                        string plain2 = JsonUtility.ToJson(d2);
                        // 原快照（空checksum）
                        data.ChecksumHex = "";
                        string plain1 = JsonUtility.ToJson(data);
                        string recompute = SaveChecksum.Fnv1a64Hex(plain2);
                        Debug.Log("[SAVE_DIAG] step5 saved=" + saved + " recompute=" + recompute
                                  + " len1=" + plain1.Length + " len2=" + plain2.Length);
                        int n = Math.Min(plain1.Length, plain2.Length), first = -1;
                        for (int i = 0; i < n; i++) if (plain1[i] != plain2[i]) { first = i; break; }
                        if (first < 0 && plain1.Length != plain2.Length) first = n;
                        if (first >= 0)
                        {
                            int a = Math.Max(0, first - 40), b = Math.Min(n, first + 40);
                            Debug.Log("[SAVE_DIAG] step6 firstdiff=" + first
                                + " P1=[" + plain1.Substring(a, b - a) + "]"
                                + " P2=[" + plain2.Substring(a, b - a) + "]");
                        }
                        else Debug.Log("[SAVE_DIAG] step6 strings identical but checksum mismatch");
                    }
                }
            }
            catch (Exception e) { Debug.LogError("[SAVE_DIAG] step3/4 EX " + e); }
        }

        /// <summary>V9.6.6 槽位列表（0..5 摘要，损坏也标出）。</summary>
        public List<SlotSummary> ListSlots()
        {
            var list = new List<SlotSummary>(ManualSlots + 1);
            for (int i = 0; i <= ManualSlots; i++) list.Add(Summarize(i));
            return list;
        }

        /// <summary>V9.6.6 写前备份：手动槽覆盖前把旧档备份到 PxC_Bak_&lt;slot&gt;（自动槽走回滚链）。</summary>
        private void BackupBeforeWrite(int slot)
        {
            if (slot == 0) return;
            string k = Key(slot);
            if (PxcStorage.BodyHasKey(k))
                PxcStorage.BodySetString("PxC_Bak_" + slot, PxcStorage.BodyGetString(k));
            if (PlayerPrefs.HasKey(SumKey(slot))) PlayerPrefs.SetString("PxC_BakSum_" + slot, PlayerPrefs.GetString(SumKey(slot)));
            PxcStorage.BodyFlush();
            PlayerPrefs.Save();
        }

        /// <summary>V9.6.6 从写前备份恢复指定手动槽（防覆盖误操作）。</summary>
        public bool RestoreBackup(int slot)
        {
            if (slot == 0) return false;
            string bk = "PxC_Bak_" + slot;
            if (!PxcStorage.BodyHasKey(bk)) { _gm?.AddEvent("bad", "存档位 " + slot + " 无写前备份"); return false; }
            PxcStorage.BodySetString(Key(slot), PxcStorage.BodyGetString(bk));
            if (PlayerPrefs.HasKey("PxC_BakSum_" + slot)) PlayerPrefs.SetString(SumKey(slot), PlayerPrefs.GetString("PxC_BakSum_" + slot));
            PxcStorage.BodyFlush();
            PlayerPrefs.Save();
            _gm?.AddEvent("good", "↩ 已从写前备份恢复存档位 " + slot);
            return true;
        }

        /// <summary>V9.6.6 原子写入：临时键（阶段1）→ 校验 → 正式键（阶段2）→ 清临时。
        /// 任意阶段中断，正式槽要么旧档完整、要么新档完整，绝无半写状态。</summary>
        private bool AtomicWrite(int slot, string finalJson, string sumJson)
        {
            string tmp = "PxC_Tmp_" + slot;
            PxcStorage.BodySetString(tmp, finalJson);
            PxcStorage.BodyFlush();                               // 阶段1：临时档先落盘（校验前）
            if (SaveEnvelope.Verify(finalJson) == null)           // 校验（新档校验和精确匹配 / 旧档结构放行）
            {
                PxcStorage.BodyDeleteKey(tmp);
                PxcStorage.BodyFlush();
                return false;                                     // 校验失败：丢弃，正式档不变
            }
            PxcStorage.BodySetString(Key(slot), finalJson);
            PxcStorage.BodyDeleteKey(tmp);
            if (!string.IsNullOrEmpty(sumJson)) PlayerPrefs.SetString(SumKey(slot), sumJson);
            PxcStorage.BodyFlush();                               // 阶段2：提交正文档
            PlayerPrefs.Save();                                   // 摘要档（PlayerPrefs）
            return true;
        }

        /// <summary>V9.6.6 版本打标 + 迁移（写入路径：旧 schema 自动升到当前；读档路径由 Decode 触发）。</summary>
        private static void MigrateAndStamp(SaveData d)
        {
            if (d.SaveSchema != SaveVersionMigrator.CurrentSchema)
            {
                if (!SaveVersionMigrator.Migrate(d))
                    Debug.LogWarning("[Save] 迁移失败，仍按原数据存档（schema=" + d.SaveSchema + "）");
            }
            else if (string.IsNullOrEmpty(d.SaveFormat)) d.SaveFormat = SaveVersionMigrator.FormatId;
        }

        /// <summary>V9.6.6 异步存档：主线程快照 + 后台算校验和 + 主线程提交（Update 队列 drain）。
        /// done 回调在主线程触发；一次仅允许一个在途异步存档。</summary>
        public void SaveAsync(int slot, Action<bool> done = null)
        {
            if (_busyAsync) { done?.Invoke(false); return; }
            try
            {
                var data = Snapshot();
                data.SlotName = slot == 0 ? "自动存档" : "存档" + slot;
                MigrateAndStamp(data);
                if (slot != 0) BackupBeforeWrite(slot);
                string jsonNoChecksum = JsonUtility.ToJson(data);     // 主线程（Unity 状态快照已在此收集）
                string sumJson = JsonUtility.ToJson(new SlotSummaryData
                {
                    Slot = slot, Year = data.Year, DynastyName = data.DynastyName,
                    BuildingCount = data.BuildingCount, SaveTime = data.SaveTime, SlotName = data.SlotName
                });
                _busyAsync = true;
#if UNITY_WEBGL
                // WebGL 单线程：无后台线程/线程池，Task.Run 不调度 → 校验和计算在主线程同步完成（FNV-1a64 全档 hash 毫秒级）；
                // 提交仍统一走 Update 队列主线程原子写，保证与桌面端同一提交路径。
                try
                {
                    string hex = SaveChecksum.Fnv1a64Hex(jsonNoChecksum);
                    if (string.IsNullOrEmpty(hex)) throw new InvalidOperationException("校验和计算失败");
                    lock (_pendingLock)
                        _pendingEncode.Enqueue(new PendingSave { Slot = slot, JsonNoChecksum = jsonNoChecksum, SumJson = sumJson, Hex = hex });
                }
                catch (Exception e)
                {
                    _busyAsync = false;
                    Debug.LogError("[SaveAsync] " + e.Message);
                    done?.Invoke(false);
                    return;
                }
#else
                // 桌面端：后台线程算校验和，避免大档 hash 阻塞主线程
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        string hex = SaveChecksum.Fnv1a64Hex(jsonNoChecksum);
                        lock (_pendingLock)
                            _pendingEncode.Enqueue(new PendingSave { Slot = slot, JsonNoChecksum = jsonNoChecksum, SumJson = sumJson, Hex = hex });
                    }
                    catch (Exception e) { Debug.LogError("[SaveAsync] " + e.Message); }
                });
#endif
                done?.Invoke(true);   // 已入队（实际落盘由 Update 队列原子提交完成）
            }
            catch (Exception e)
            {
                _busyAsync = false;
                Debug.LogError(e);
                _gm?.AddEvent("bad", "存档失败：" + e.Message);
                done?.Invoke(false);
            }
        }

        /// <summary>V9.6.6 同步存档（保留兼容旧调用点，内部走信封+原子写）。</summary>
        public void SaveToSlot(int slot)
        {
            try
            {
                var data = Snapshot();
                data.SlotName = slot == 0 ? "自动存档" : "存档" + slot;
                MigrateAndStamp(data);
                if (slot != 0) BackupBeforeWrite(slot);
                string final = SaveEnvelope.AttachChecksum(data);
                if (final == null) throw new Exception("信封序列化失败");
                string sumJson = JsonUtility.ToJson(new SlotSummaryData
                {
                    Slot = slot, Year = data.Year, DynastyName = data.DynastyName,
                    BuildingCount = data.BuildingCount, SaveTime = data.SaveTime, SlotName = data.SlotName
                });
                bool ok = AtomicWrite(slot, final, sumJson);
                _gm?.AddEvent(ok ? "good" : "bad", ok ? (slot == 0 ? "🤖 自动" : "💾 已") + "保存到" + (slot == 0 ? "自动槽" : "存档位 " + slot)
                                                     : "存档校验失败，已回滚");
            }
            catch (Exception e) { Debug.LogError(e); _gm?.AddEvent("bad", "存档失败：" + e.Message); }
        }

        /// <summary>V9.6.5 自动保存（含回滚滚动 + 健康标记）；V9.6.6 改为异步写（后台校验和 + 主线程原子提交）。
        /// 安全模式下降频至 20s。</summary>
        public void AutoSaveNow()
        {
            // 1) 先把当前自动档滚动进回滚链：rollback2 <- rollback1 <- rollback0 <- 旧 auto
            string cur = PxcStorage.BodyGetString(Key(0), "");
            if (!string.IsNullOrEmpty(cur))
            {
                for (int i = RollbackSlots - 1; i >= 1; i--)
                {
                    if (PxcStorage.BodyHasKey(RollKey(i - 1)))
                        PxcStorage.BodySetString(RollKey(i), PxcStorage.BodyGetString(RollKey(i - 1)));
                    else if (PxcStorage.BodyHasKey(RollKey(i)))
                        PxcStorage.BodyDeleteKey(RollKey(i));
                }
                PxcStorage.BodySetString(RollKey(0), cur);
                if (PlayerPrefs.HasKey(SumKey(0)))
                    PlayerPrefs.SetString(RollSumKey(0), PlayerPrefs.GetString(SumKey(0)));
                PxcStorage.BodyFlush();
                PlayerPrefs.Save();
            }
            // 2) 写新自动档（异步原子写）
            SaveAsync(0, ok =>
            {
                if (ok)
                {
                    // 3) 健康标记：最近一次"成功"自动存档时间戳（崩溃恢复的锚点）
                    PlayerPrefs.SetString(CrashGuardSystem.KeyGood, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                    PlayerPrefs.Save();
                }
            });
        }

        /// <summary>V9.6.5 崩溃恢复：按 auto → rollback0 → rollback1 → rollback2 依次尝试；成功返回所用槽位名。</summary>
        public bool LoadBestForRecovery(out string usedSlot)
        {
            usedSlot = "";
            int[] order = { 0, RollSlot(0), RollSlot(1), RollSlot(2) };
            for (int i = 0; i < order.Length; i++)
            {
                if (TryLoadQuiet(order[i]))
                {
                    usedSlot = order[i] == 0 ? "自动槽" : "回滚槽 " + (order[i] - RollSlot(0));
                    return true;
                }
            }
            return false;
        }

        private bool TryLoadQuiet(int slot)
        {
            if (slot >= 0 && slot <= ManualSlots && !HasSlot(slot)) return false;
            bool isRoll = slot >= RollSlot(0) && slot < RollSlot(0) + RollbackSlots;
            if (isRoll && !PxcStorage.BodyHasKey(RollKey(slot - RollSlot(0)))) return false;
            try
            {
                string json = isRoll ? PxcStorage.BodyGetString(RollKey(slot - RollSlot(0)))
                                     : PxcStorage.BodyGetString(Key(slot));
                // V9.6.6 信封校验：新档校验和精确匹配，旧档结构放行；篡改/损坏拒绝
                var d = SaveEnvelope.Verify(json);
                if (d == null) return false;                             // 校验失败 → 损坏
                if (!SaveVersionMigrator.Migrate(d)) return false;       // 版本迁移失败 → 拒绝
                if (d.Year == 0 && d.BuildingCount == 0 && d.AgentCount == 0 && d.Pop == 0) return false; // 空快照拒绝
                Apply(d);
                return true;
            }
            catch { return false; }
        }

        /// <summary>V9.6.5 模拟：损坏自动存档（供 WebCrashSimulate mode=2 崩溃恢复测试）。</summary>
        public void WebCrashCorruptAuto()
        {
            PxcStorage.BodySetString(Key(0), "{\"Version\":\"CORRUPTED\",\"");   // 非法 JSON
            PlayerPrefs.DeleteKey(SumKey(0));
            PxcStorage.BodyFlush();
            PlayerPrefs.Save();
            Debug.LogWarning("[CrashSim] 已故意损坏自动槽（模拟崩溃时写坏档）");
        }

        /// <summary>回滚槽全量键（100=rollback0 起，避开 0..5 手动槽编号）</summary>
        private static int RollSlot(int i) => 100 + i;
        private static string RollKey(int i) => "PxC_Roll_" + i;
        private static string RollSumKey(int i) => "PxC_RollSum_" + i;

        public void Init(GameManager gm){ _gm=gm; }

        private static string Key(int slot)=>"PxC_Save_"+(slot==0?"auto":slot.ToString());
        /// <summary>V9.6.4 槽位摘要键（独立小 JSON，与完整档同生命周期）</summary>
        private static string SumKey(int slot)=>"PxC_SaveSum_"+(slot==0?"auto":slot.ToString());
        /// <summary>读槽位摘要：优先小 JSON；无（旧档/被清）则回退全量解析。</summary>
        private SlotSummaryData ReadSummary(int slot)
        {
            if (!HasSlot(slot)) return null;
            if (PlayerPrefs.HasKey(SumKey(slot)))
            {
                try
                {
                    var d=JsonUtility.FromJson<SlotSummaryData>(PlayerPrefs.GetString(SumKey(slot)));
                    if (d!=null && d.SaveTime>0) return d;
                }
                catch { /* 摘要损坏 → 回退全量解析 */ }
            }
            try
            {
                // V9.6.6 全量回退走信封校验（损坏档不参与摘要/最近槽）
                var full=SaveEnvelope.Verify(PxcStorage.BodyGetString(Key(slot)));
                if (full==null) return null;
                return new SlotSummaryData{ Slot=slot, Year=full.Year, DynastyName=full.DynastyName,
                    BuildingCount=full.BuildingCount, SaveTime=full.SaveTime,
                    SlotName=slot==0?"自动存档":"存档"+slot };
            }
            catch { return null; }
        }

        private SaveData Snapshot()
        {
            var s=_gm.State;
            var _gt=UnityEngine.Object.FindObjectOfType<WorldGenerator>();
            var _grown=new List<float>();
            if(_gt!=null) foreach(var L in _gt.GrownLands){_grown.Add(L.Cx);_grown.Add(L.Cz);_grown.Add(L.Br);_grown.Add(L.Kind);_grown.Add(L.P1);_grown.Add(L.P2);_grown.Add(L.P3);}
            float[] grownArr=_grown.ToArray();
            var d=new SaveData
            {
                Version=RuntimeVersion,
                Year=s.Year,Day=s.Day,Era=s.Era,DynastyIdx=s.DynastyIdx,
                DynastyName=_gm.Time!=null?_gm.Time.DynastyName:"",
                Pop=s.Pop,MaxPop=s.MaxPop,Happiness=s.Happiness,
                Housing=s.Housing,DynastyMorale=s.DynastyMorale,Speed=s.Speed,DebugLevel=s.DebugLevel,
                RallyKind=s.RallyKind,RallyX=s.RallyX,RallyZ=s.RallyZ,   // V9.6.0 紧急集结令随档持久化
                Children=s.Children,Young=s.Young,Middle=s.Middle,Old=s.Old,
                MilSoldiers=s.MilSoldiers,MilCavalry=s.MilCavalry,MilFirepower=s.MilFirepower,MilDefense=s.MilDefense,
                WarActive=s.WarActive,Victory=s.Victory,VictoryType=s.VictoryType,
                ResearchProgress=s.ResearchProgress,CurrentResearch=s.CurrentResearch,
                SocialKeys=s.SocialClasses.Keys.ToArray(),SocialVals=s.SocialClasses.Values.ToArray(),
                ResKeys=s.Res.Keys.ToArray(),ResVals=s.Res.Values.ToArray(),
                Techs=s.ResearchedTechs.ToArray(),Policies=s.Policies.ToArray(),
                CanalSegments=s.CanalSegments,CanalBonus=s.CanalBonus,AiBonus=s.AiBonus,
                ElectricGrid=s.ElectricGrid,PowerCoverage=s.PowerCoverage,
                PowerSupply=s.PowerSupply,PowerDemand=s.PowerDemand,PowerRatio=s.PowerRatio,
                Pollution=s.Pollution,IndustryChainMult=s.IndustryChainMult,
                CityHealth=s.CityHealth,CityEducation=s.CityEducation,CitySafety=s.CitySafety,CityEmployment=s.CityEmployment,
                CityTaxLevel=s.CityTaxLevel,
                SpElevator=s.SpElevator,SpShips=s.SpShips,SpDyson=s.SpDyson,SpLunar=s.SpLunar,SpMars=s.SpMars,
                OceanUnlocked=s.OceanUnlocked,SpaceUnlocked=s.SpaceUnlocked,
                OceanDiscovered=s.OceanDiscovered.ToArray(),
                CanalAutoBuild=s.CanalAutoBuild,CanalBuildTimer=s.CanalBuildTimer,TidePhase=s.TidePhase,
                TideLevel=s.TideLevel,TideHigh=s.TideHigh,
                CanalCells=s.CanalCells.ToArray(),
            BridgeCells=s.BridgeCells.ToArray(), BridgeRuns=s.BridgeRuns.ToArray(),
            Piers=s.Piers.ToArray(),
            GrownLands=grownArr,
                OceanResKeys=s.OceanResources.Keys.ToArray(),OceanResVals=s.OceanResources.Values.ToArray(),
                SpaceResKeys=s.SpaceResources.Keys.ToArray(),SpaceResVals=s.SpaceResources.Values.ToArray(),
                ShipCount=s.Ships.Count,
                ShipType=s.Ships.Select(p=>p.ShipTypeId).ToArray(),
                ShipX=s.Ships.Select(p=>p.X).ToArray(),ShipZ=s.Ships.Select(p=>p.Z).ToArray(),
                ShipLvl=s.Ships.Select(p=>p.Level).ToArray(),ShipMil=s.Ships.Select(p=>p.Military).ToArray(),
                ShipCrew=s.Ships.Select(p=>p.Crew).ToArray(),ShipHp=s.Ships.Select(p=>p.Hp).ToArray(),
                CartCount=s.Carts.Count,
                CartType=s.Carts.Select(c=>c.CartTypeId).ToArray(),
                CartX=s.Carts.Select(c=>c.X).ToArray(),CartZ=s.Carts.Select(c=>c.Z).ToArray(),
                CartLvl=s.Carts.Select(c=>c.Level).ToArray(),
                AgentCount=s.Agents.Count,
                AgentX=s.Agents.Select(a=>a.X).ToArray(),AgentZ=s.Agents.Select(a=>a.Z).ToArray(),
                AgentHX=s.Agents.Select(a=>a.HomeX).ToArray(),AgentHZ=s.Agents.Select(a=>a.HomeZ).ToArray(),
                AgentClass=s.Agents.Select(a=>a.SocialClass).ToArray(),AgentJob=s.Agents.Select(a=>a.Job).ToArray(),
                AgentAge=s.Agents.Select(a=>a.Age).ToArray(),AgentSeed=s.Agents.Select(a=>a.ColorSeed).ToArray(),
                BuildingCount=s.Buildings.Count,
                AgeOfSail=s.AgeOfSail,AgeOfSpace=s.AgeOfSpace,WorldExpansion=s.WorldExpansion,
                // V6.1.3 全要素补全
                Philosophy=s.Philosophy,SchoolFounded=s.SchoolFounded,Corruption=s.Corruption,
                MonarchWise=s.MonarchWise,MonarchName=s.MonarchName,
                FiredEvents=s.FiredEvents.ToArray(),
                SaveTime=DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            // V6.1.3 地形种子（保证读档回到同一张大地图）与玩家散树
            var ter=UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            d.TerrainSeed=ter!=null?ter.Seed:0;
            d.EarthMode=s.EarthMode;   // V9.1.0
            d.TreeCount=s.Trees.Count;
            d.TreeX=s.Trees.Select(t=>t.X).ToArray(); d.TreeZ=s.Trees.Select(t=>t.Z).ToArray();
            d.TreeStage=s.Trees.Select(t=>t.Stage).ToArray(); d.TreeAge=s.Trees.Select(t=>t.Age).ToArray();
            d.BType=s.Buildings.Select(b=>b.Type).ToArray();
            d.BX=s.Buildings.Select(b=>b.X).ToArray();
            d.BZ=s.Buildings.Select(b=>b.Z).ToArray();
            d.BLvl=s.Buildings.Select(b=>b.Level).ToArray();
            d.BAge=s.Buildings.Select(b=>b.Age).ToArray();
            d.BHp=s.Buildings.Select(b=>b.Hp).ToArray();
            d.BFarmStage=s.Buildings.Select(b=>b.FarmStage).ToArray();
            // V6.1.3 聚落中心 + 国家/天下分合
            d.WorldPhase=s.WorldPhase;d.PhaseYearsLeft=s.PhaseYearsLeft;d.PlayerNationId=s.PlayerNationId;
            d.VilCount=s.VillageX.Count; d.VilX=s.VillageX.ToArray(); d.VilZ=s.VillageZ.ToArray();
            d.NationCount=s.Nations.Count;
            d.NId=s.Nations.Select(n=>n.Id).ToArray();
            d.NName=s.Nations.Select(n=>n.Name).ToArray();
            d.NHex=s.Nations.Select(n=>n.ColorHex).ToArray();
            d.NX=s.Nations.Select(n=>n.Cx).ToArray(); d.NZ=s.Nations.Select(n=>n.Cz).ToArray();
            d.NPop=s.Nations.Select(n=>n.Pop).ToArray();
            d.NPlayer=s.Nations.Select(n=>n.IsPlayer).ToArray();
            d.NAlive=s.Nations.Select(n=>n.Alive).ToArray();
            d.NFaction=s.Nations.Select(n=>n.Faction).ToArray();   // V9.1.1
            d.NNote=s.Nations.Select(n=>n.Note).ToArray();   // V9.1.1
            // V6.1.4 我方步骑部队
            d.FuKind=s.FriendlyUnits.Select(u=>u.Kind).ToArray();
            d.FuX=s.FriendlyUnits.Select(u=>u.X).ToArray();
            d.FuZ=s.FriendlyUnits.Select(u=>u.Z).ToArray();
            d.FuHp=s.FriendlyUnits.Select(u=>u.Hp).ToArray();
            d.FactionAnnexTimer=s.FactionAnnexTimer;
            // V6.1.5 殖民地
            d.ColCount=s.Colonies.Count;
            d.ColId=s.Colonies.Select(c=>c.Id).ToArray();
            d.ColName=s.Colonies.Select(c=>c.Name).ToArray();
            d.ColRes=s.Colonies.Select(c=>c.ResId).ToArray();
            d.ColLvl=s.Colonies.Select(c=>c.Level).ToArray();
            d.ColPop=s.Colonies.Select(c=>c.Pop).ToArray();
            d.ColLoy=s.Colonies.Select(c=>c.Loyalty).ToArray();
            d.ColX=s.Colonies.Select(c=>c.X).ToArray();
            d.ColZ=s.Colonies.Select(c=>c.Z).ToArray();
            d.ColonialAge=s.ColonialAge;
            // V6.1.6 副本网格
            d.OceanExpData=CloneExp(s.OceanExp); d.SpaceExpData=CloneExp(s.SpaceExp);
            // V6.1.8 九智能体共治运行态
            var cou=_gm.Council;
            if(cou!=null){
                d.AIEnabled=cou.Enabled;d.AIOnline=cou.Online;d.AITokens=cou.TokensUsed;
                d.AIInterval=cou.IntervalYears;d.AILastCouncil=cou.LastCouncilYear;d.AISafety=cou.SafetyCount;
                d.AIApiKey=cou.ApiKey;d.AIModel=cou.Model;
                d.AIGodLast=cou.Gods.Select(g=>g.LastYear).ToArray();
                d.AIGodAct=cou.Gods.Select(g=>g.Actions).ToArray();
                d.AIGodZeal=cou.Gods.Select(g=>g.Zeal).ToArray();
                d.AIGodIntv=cou.Gods.Select(g=>g.Intervene).ToArray();
                d.AIGodExp=cou.Gods.Select(g=>g.Expand).ToArray();
                d.AIGodPrd=cou.Gods.Select(g=>g.Prudent).ToArray();
                d.AiHistory=cou.History.ToArray();
            }
            // V6.1.9 加速冷冻运行态
            d.CryoAccum=s.CryoAccumYears; d.CryoActive=s.CryoActive; d.CryoRemain=s.CryoRemainSec;
            // V9.2.2 人口周期律
            d.LandIntegrity=s.LandIntegrity; d.PeakPop=s.PeakPop; d.DynastyAge=s.DynastyAge; d.CyclePhase=s.CyclePhase;
            // V6.8.0 奇观 / 成就
            d.WonderCount=s.Wonders.Count;
            d.WId=s.Wonders.Select(w=>w.Id).ToArray();
            d.WYear=s.Wonders.Select(w=>w.BuiltYear).ToArray();
            d.WX=s.Wonders.Select(w=>w.X).ToArray();
            d.WZ=s.Wonders.Select(w=>w.Z).ToArray();
            d.Achievements=s.Achievements.ToArray(); d.WonderAuto=s.WonderAuto;
            // V9.6.6 ISaveable 扩展区：注册系统的序列化 JSON（纯数据，失败单系统跳过）
            if (_saveables.Count > 0)
            {
                var exK = new List<string>(_saveables.Count);
                var exV = new List<string>(_saveables.Count);
                for (int i = 0; i < _saveables.Count; i++)
                {
                    try
                    {
                        string j = _saveables[i].Serialize();
                        if (!string.IsNullOrEmpty(j)) { exK.Add(_saveables[i].SaveKey); exV.Add(j); }
                    }
                    catch (Exception e) { Debug.LogWarning("[Save] ISaveable serialize fail " + _saveables[i].SaveKey + ": " + e.Message); }
                }
                d.ExtKeys = exK.ToArray(); d.ExtVals = exV.ToArray();
            }
            return d;
        }

        /// <summary>深拷贝副本状态用于存档（剥离运行时，只留数据）</summary>
        private static ExpeditionState CloneExp(ExpeditionState e)
        {
            if (e==null||!e.Inited) return null;
            return new ExpeditionState
            {
                MapType=e.MapType,N=e.N,Inited=e.Inited,Seed=e.Seed,PosX=e.PosX,PosY=e.PosY,
                Power=e.Power,MaxPower=e.MaxPower,Supply=e.Supply,
                Seen=e.Seen?.ToArray(),NodeKind=e.NodeKind?.ToArray(),NodeUsed=e.NodeUsed?.ToArray(),
                LastEvent=e.LastEvent
            };
        }

        // ---------- 槽位读写 ----------
        public bool HasSlot(int slot) => PxcStorage.BodyHasKey(Key(slot));

        /// <summary>返回 0(自动)..5(手动) 中存档时间最新的非空槽位，损坏槽跳过；没有任何有效存档返回 -1。
        /// V9.6.4 改为读槽位摘要（无摘要旧档自动回退全量解析），不再每槽全量反序列化。</summary>
        public int LatestSlot()
        {
            int best=-1; long bestTime=long.MinValue;
            for(int slot=0;slot<=ManualSlots;slot++)
            {
                if(!HasSlot(slot)) continue;
                var sum=ReadSummary(slot);
                if(sum!=null && sum.SaveTime>=bestTime){bestTime=sum.SaveTime;best=slot;}
            }
            return best;
        }
        public void DeleteSlot(int slot)
        {
            if(slot==0) return;                 // 自动槽不允许删除
            // V9.6.6 删除前备份（防误删，可 RestoreBackup 找回）
            BackupBeforeWrite(slot);
            PxcStorage.BodyDeleteKey(Key(slot));
            PlayerPrefs.DeleteKey(SumKey(slot)); // 摘要随档同删
            PxcStorage.BodyFlush();
            PlayerPrefs.Save();
            _gm.AddEvent("info","已删除存档位 "+slot);
        }
        public bool LoadFromSlot(int slot)
        {
            if(!HasSlot(slot)){ _gm.AddEvent("bad",(slot==0?"自动槽":"存档位 "+slot)+" 为空");return false; }
            try
            {
                // V9.6.6 信封校验 + 版本迁移（新档校验和精确匹配；篡改/损坏拒绝）
                var d = SaveEnvelope.Verify(PxcStorage.BodyGetString(Key(slot)));
                if (d == null) { _gm.AddEvent("bad", "存档位 " + slot + " 校验失败（损坏/篡改），已拒绝加载"); return false; }
                if (!SaveVersionMigrator.Migrate(d)) { _gm.AddEvent("bad", "存档位 " + slot + " 版本迁移失败，已拒绝加载"); return false; }
                Apply(d);
                _gm.AddEvent("good","📂 已读取"+(slot==0?"自动存档":"存档位 "+slot));
                return true;
            }
            catch(Exception e){ _gm.AddEvent("bad","读档失败："+e.Message);return false; }
        }

        /// <summary>读取槽位摘要（列表用，损坏也能识别）。V9.6.4 优先小 JSON，旧档回退全量解析。</summary>
        public SlotSummary Summarize(int slot)
        {
            var sum=new SlotSummary{Slot=slot,Exists=HasSlot(slot)};
            if(!sum.Exists) return sum;
            var d=ReadSummary(slot);
            if(d==null){ sum.Damaged=true; return sum; }
            sum.Year=d.Year;sum.Dynasty=d.DynastyName;sum.BuildingCount=d.BuildingCount;
            sum.Time=d.SaveTime;sum.Name=slot==0?"🤖 自动存档":"💾 存档"+slot;
            return sum;
        }

        private void Apply(SaveData d)
        {
            var s=_gm.State;
            // 0) V6.1.3 按存档地形种子还原同一张大地图（植被/村址/相机/小地图同步），保证建筑与单位坐标不漂移
            var ter=UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.WorldGenerator>();
            s.EarthMode=d.EarthMode;   // V9.1.0 先恢复地图模式，再决定地形重建路径
            if(ter!=null && d.TerrainSeed!=0 && (ter.Seed!=d.TerrainSeed || ter.EarthMode!=d.EarthMode))
            {
                var village=d.EarthMode?ter.RegenerateEarth(d.TerrainSeed):ter.Regenerate(d.TerrainSeed);
                var veg=UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.VegetationSystem>();
                veg?.Regrow(ter,d.TerrainSeed);
                var rig=UnityEngine.Object.FindObjectOfType<PixelToCivilization.World.CameraRig>();
                rig?.Retarget(village);
                PixelToCivilization.UI.UIManager.Instance?.InvalidateMinimapBase();
            }
            // 1) 清空现有实体视图与数据
            // V6.1.3 先清掉上一局/随机局残留的聚落装饰节点（道路/旗帜/码头；祭坛棚屋属建筑视图，随后按存档重建）
            foreach(Transform child in _gm.transform)
                if (child.name.StartsWith("Settlement")) Destroy(child.gameObject);
            foreach(var b in s.Buildings) if(b.View)Destroy(b.View);
            s.Buildings.Clear();
            foreach(var old in s.Ships) if(old.View)Destroy(old.View);
            s.Ships.Clear();
            foreach(var old in s.Carts) if(old.View)Destroy(old.View);
            s.Carts.Clear();
            foreach(var a in s.Agents) if(a.View)Destroy(a.View);
            s.Agents.Clear();
            // V6.1.3 清空散树（视图+数据），随后按存档重建
            _gm.Env?.ClearTrees();
            // 2) 标量状态
            s.Year=d.Year;s.Day=d.Day;s.Era=d.Era;s.DynastyIdx=d.DynastyIdx;
            s.Pop=d.Pop;s.MaxPop=Mathf.Max(100,d.MaxPop);s.Happiness=d.Happiness;
            s.Housing=d.Housing;s.DynastyMorale=d.DynastyMorale;s.Speed=d.Speed;s.DebugLevel=d.DebugLevel;
            s.RallyKind=d.RallyKind??""; s.RallyX=d.RallyX; s.RallyZ=d.RallyZ;   // V9.6.0 集结令（旧档无字段→无旗）
            _gm.Rally?.RestoreFlag();   // V9.6.0 读档重建军旗模型
            s.Children=d.Children;s.Young=d.Young;s.Middle=d.Middle;s.Old=d.Old;
            s.MilSoldiers=d.MilSoldiers;s.MilCavalry=d.MilCavalry;s.MilFirepower=d.MilFirepower;s.MilDefense=d.MilDefense;
            s.WarActive=d.WarActive;s.Victory=d.Victory;s.VictoryType=d.VictoryType;
            s.ResearchProgress=d.ResearchProgress;s.CurrentResearch=d.CurrentResearch;
            // 3) 字典 / 集合
            s.Res.Clear();
            if(d.ResKeys!=null)for(int i=0;i<d.ResKeys.Length;i++) s.Res[d.ResKeys[i]]=d.ResVals[i];
            s.ResearchedTechs=new HashSet<string>(d.Techs??Array.Empty<string>());
            s.Policies=new HashSet<string>(d.Policies??Array.Empty<string>());
            s.SocialClasses.Clear();
            if(d.SocialKeys!=null)for(int i=0;i<d.SocialKeys.Length;i++) s.SocialClasses[d.SocialKeys[i]]=d.SocialVals[i];
            s.CanalSegments=d.CanalSegments;s.CanalBonus=d.CanalBonus;s.AiBonus=d.AiBonus;
            s.ElectricGrid=d.ElectricGrid;s.PowerCoverage=d.PowerCoverage;
            // V9.0.4 旧存档缺这些字段时反序列化为0，需安全默认（PowerRatio/产业链=1 才不会误减产）
            s.PowerSupply=d.PowerSupply;s.PowerDemand=d.PowerDemand;
            s.PowerRatio=d.PowerRatio<=0f?1f:d.PowerRatio;
            s.Pollution=d.Pollution;s.IndustryChainMult=d.IndustryChainMult<=0f?1f:d.IndustryChainMult;
            // V9.0.5 旧档缺城市指标（反序列化为0）时给安全默认，避免面板全红
            s.CityHealth=d.CityHealth<=0f?60f:d.CityHealth;
            s.CityEducation=d.CityEducation<=0f?40f:d.CityEducation;
            s.CitySafety=d.CitySafety<=0f?55f:d.CitySafety;
            s.CityEmployment=d.CityEmployment<=0f?85f:d.CityEmployment;
            s.CityTaxLevel=(d.CityTaxLevel>=0&&d.CityTaxLevel<=2)?d.CityTaxLevel:1; // V9.0.7 旧档默认标准税
            s.SpElevator=d.SpElevator;s.SpShips=d.SpShips;s.SpDyson=d.SpDyson;s.SpLunar=d.SpLunar;s.SpMars=d.SpMars;
            s.OceanUnlocked=d.OceanUnlocked;s.SpaceUnlocked=d.SpaceUnlocked;
            s.OceanDiscovered=new List<string>(d.OceanDiscovered??Array.Empty<string>());
            RestoreDict(s.OceanResources,d.OceanResKeys,d.OceanResVals);
            RestoreDict(s.SpaceResources,d.SpaceResKeys,d.SpaceResVals);
            s.CanalAutoBuild=d.CanalAutoBuild;s.CanalBuildTimer=d.CanalBuildTimer;s.TidePhase=d.TidePhase;
            s.TideLevel=d.TideLevel;s.TideHigh=d.TideHigh;
            s.CanalCells=new List<int>(d.CanalCells??Array.Empty<int>());
            s.BridgeCells=new HashSet<int>(d.BridgeCells??Array.Empty<int>()); s.BridgeRuns=new List<int>(d.BridgeRuns??Array.Empty<int>());
            s.Piers=new List<float>(d.Piers??Array.Empty<float>());   // V9.4.6 高架柱
            // V6.1.3 地图延展 + 大航海/宇宙里程碑
            s.AgeOfSail=d.AgeOfSail;s.AgeOfSpace=d.AgeOfSpace;s.WorldExpansion=Mathf.Max(1f,d.WorldExpansion);
            if(d.EarthMode){ s.WorldExpansion=1f; }   // V9.1.0 地球模式全球已揭示，不能被 SnapExpansion 重置回 480
            else ter?.SnapExpansion(s.WorldExpansion);
            // V6.1.3 全要素补全：学派 / 科举 / 吏治 / 君主 / 已触发历史事件（防读档后重复发奖）
            s.Philosophy=d.Philosophy; s.SchoolFounded=d.SchoolFounded; s.Corruption=d.Corruption;
            s.MonarchWise=d.MonarchWise; s.MonarchName=string.IsNullOrEmpty(d.MonarchName)?"禅让贤者":d.MonarchName;
            s.FiredEvents=new HashSet<string>(d.FiredEvents??Array.Empty<string>());
            // 4) 重建建筑
            for(int i=0;i<d.BuildingCount;i++)
            {
                var def=_gm.Def(d.BType[i]);
                if(def==null)continue;
                int fs=(d.BFarmStage!=null&&i<d.BFarmStage.Length)?d.BFarmStage[i]:0;
                var b=new BuildingEntity{Type=d.BType[i],Def=def,X=d.BX[i],Z=d.BZ[i],Level=d.BLvl[i],Age=d.BAge[i],Hp=d.BHp[i],FarmStage=fs};
                s.Buildings.Add(b); _gm.Building.SpawnView(b);
            }
            // 5) 重建船只 / 车辆
            if(d.ShipType!=null)
                for(int i=0;i<d.ShipType.Length;i++)
                {
                    var sh=_gm.Naval.RestoreShip(d.ShipType[i],d.ShipX[i],d.ShipZ[i],d.ShipLvl[i],d.ShipMil[i]);
                    if(sh!=null)
                    {   // 回填船员（否则 SpeedOf=0 船不动、攻击加成丢失）与存档血量
                        if(d.ShipCrew!=null&&i<d.ShipCrew.Length) sh.Crew=d.ShipCrew[i];
                        if(d.ShipHp!=null&&i<d.ShipHp.Length&&d.ShipHp[i]>0) sh.Hp=d.ShipHp[i];
                    }
                }
            if(d.CartType!=null)
                for(int i=0;i<d.CartType.Length;i++){
                    int clv=(d.CartLvl!=null&&i<d.CartLvl.Length)?d.CartLvl[i]:1;
                    _gm.Cart.SpawnCart(d.CartType[i],d.CartX[i],d.CartZ[i],clv);
                }
            // 6) 重建人口（按存档阶层/职业）
            if(d.AgentX!=null && _gm.Env!=null)
                for(int i=0;i<d.AgentX.Length;i++)
                {
                    string cls=d.AgentClass!=null&&i<d.AgentClass.Length?d.AgentClass[i]:"commoner";
                    string job=d.AgentJob!=null&&i<d.AgentJob.Length?d.AgentJob[i]:"idle";
                    int age=d.AgentAge!=null&&i<d.AgentAge.Length?d.AgentAge[i]:-1;
                    int seed=d.AgentSeed!=null&&i<d.AgentSeed.Length?d.AgentSeed[i]:0;
                    _gm.Env.SpawnAgent(d.AgentX[i],d.AgentZ[i],d.AgentHX[i],d.AgentHZ[i],cls,job,age,seed);
                }
            // 7) 运河视觉
            _gm.Canal.RebuildViews();
            _gm.Bridge?.RebuildViews();
            // V6.5.4 恢复运行时实时增陆（地形按种子重建后补盖戳）
            if(d.GrownLands!=null){
                var tg=UnityEngine.Object.FindObjectOfType<WorldGenerator>();
                if(tg!=null){var rr=new System.Random(d.TerrainSeed+777);
                    for(int gi=0;gi+6<d.GrownLands.Length;gi+=7)
                        tg.RestoreGrownLand(d.GrownLands[gi],d.GrownLands[gi+1],d.GrownLands[gi+2],(int)d.GrownLands[gi+3],d.GrownLands[gi+4],d.GrownLands[gi+5],d.GrownLands[gi+6],rr);
                    tg.EndRestoreGrown();}}
            // 8) V6.1.3 重建玩家散树 + 恢复飞鸟/鱼群（纯视觉）
            if(d.TreeX!=null && _gm.Env!=null)
                for(int i=0;i<d.TreeX.Length;i++)
                {
                    int st=(d.TreeStage!=null&&i<d.TreeStage.Length)?d.TreeStage[i]:1;
                    _gm.Env.SpawnTree(d.TreeX[i],d.TreeZ[i],st);
                }
            _gm.Env?.ReinitWildlife();
            // 9) V6.1.3 聚落中心 + 国家/天下分合恢复
            s.VillageX.Clear(); s.VillageZ.Clear();
            if (d.VilX!=null) for(int i=0;i<d.VilX.Length;i++){ s.VillageX.Add(d.VilX[i]); s.VillageZ.Add(d.VilZ[i]); }
            s.Nations.Clear();
            if (d.NId!=null)
                for(int i=0;i<d.NId.Length;i++)
                {
                    var n=new NationEntity
                    {
                        Id=d.NId[i],
                        Name=(d.NName!=null&&i<d.NName.Length)?d.NName[i]:"方国",
                        ColorHex=(d.NHex!=null&&i<d.NHex.Length)?d.NHex[i]:"888888",
                        Cx=d.NX[i],Cz=d.NZ[i],
                        Pop=(d.NPop!=null&&i<d.NPop.Length)?d.NPop[i]:0,
                        IsPlayer=d.NPlayer!=null&&i<d.NPlayer.Length&&d.NPlayer[i],
                        Alive=d.NAlive==null||i>=d.NAlive.Length||d.NAlive[i],
                        Faction=(d.NFaction!=null&&i<d.NFaction.Length)?d.NFaction[i]:0,   // V9.1.1
                        Note=(d.NNote!=null&&i<d.NNote.Length)?d.NNote[i]:"",   // V9.1.1
                        // V6.1.7 大陆归属由重建后的地形现算（地形按同种子确定性重生成）
                        ContinentId=ter!=null?Mathf.Max(1,ter.ContinentAt(d.NX[i],d.NZ[i])):1,
                    };
                    n.Power=n.Pop; s.Nations.Add(n);
                }
            if (d.EarthMode && ter!=null)
                PixelToCivilization.World.InitialSettlementBuilder.RebuildEarthLandmarks(_gm, ter);   // V9.1.1 阵营地标读档重建
            s.WorldPhase=string.IsNullOrEmpty(d.WorldPhase)?"split":d.WorldPhase;
            s.PhaseYearsLeft=d.PhaseYearsLeft; s.PlayerNationId=d.PlayerNationId;
            // V6.1.4 恢复我方步骑部队（数据；视图由 MilitarySystem.Tick 的 RebuildAndPumpUnits 重建）
            s.FriendlyUnits.Clear();
            if(d.FuKind!=null)
                for(int i=0;i<d.FuKind.Length;i++)
                {
                    bool cav=d.FuKind[i]==1;
                    float x=d.FuX!=null&&i<d.FuX.Length?d.FuX[i]:0, z=d.FuZ!=null&&i<d.FuZ.Length?d.FuZ[i]:0;
                    s.FriendlyUnits.Add(new FriendlyUnit{
                        Kind=d.FuKind[i],X=x,Z=z,HomeX=x,HomeZ=z,State=0,
                        Hp=d.FuHp!=null&&i<d.FuHp.Length&&d.FuHp[i]>0?d.FuHp[i]:(cav?60:50),MaxHp=cav?60:50,
                        Attack=cav?9:6,Speed=cav?3.2f:1.6f});
                }
            s.FactionAnnexTimer=d.FactionAnnexTimer;
            // V6.1.5 恢复殖民地
            s.Colonies.Clear();
            if(d.ColId!=null)
                for(int i=0;i<d.ColId.Length;i++)
                    s.Colonies.Add(new Colony{
                        Id=d.ColId[i],
                        Name=d.ColName!=null&&i<d.ColName.Length?d.ColName[i]:"殖民地",
                        ResId=d.ColRes!=null&&i<d.ColRes.Length?d.ColRes[i]:"spice",
                        Level=d.ColLvl!=null&&i<d.ColLvl.Length?d.ColLvl[i]:1,
                        Pop=d.ColPop!=null&&i<d.ColPop.Length?d.ColPop[i]:10,
                        Loyalty=d.ColLoy!=null&&i<d.ColLoy.Length?d.ColLoy[i]:100,
                        X=d.ColX!=null&&i<d.ColX.Length?d.ColX[i]:0,
                        Z=d.ColZ!=null&&i<d.ColZ.Length?d.ColZ[i]:0});
            s.ColonialAge=d.ColonialAge;
            // V6.1.6 恢复探索副本网格（旧存档无则给空白新图）
            s.OceanExp=d.OceanExpData??new ExpeditionState(){MapType="ocean"};
            s.SpaceExp=d.SpaceExpData??new ExpeditionState(){MapType="space"};
            // V6.1.8 恢复九智能体共治运行态（旧存档无则保持默认开启·离线）
            if(_gm.Council!=null){
                var c=_gm.Council;
                c.Enabled=d.AIEnabled; c.SetOnline(d.AIOnline); c.TokensUsed=d.AITokens;
                c.IntervalYears=d.AIInterval<=0?3:d.AIInterval; c.LastCouncilYear=d.AILastCouncil; c.SafetyCount=d.AISafety;
                if(!string.IsNullOrEmpty(d.AIApiKey))c.ApiKey=d.AIApiKey;
                // V6.1.9：旧档写死的失效模型(seed-1-6-250615)自动升级为实测可用模型并开启联网；其余尊重存档选择
                bool staleModel=string.IsNullOrEmpty(d.AIModel)||d.AIModel=="doubao-seed-1-6-250615";
                if(staleModel){ c.Model="deepseek-v4-flash-ga-260731"; c.SetOnline(true); }
                else c.Model=d.AIModel;
                if(d.AIGodLast!=null)for(int i=0;i<Mathf.Min(d.AIGodLast.Length,c.Gods.Count);i++)c.Gods[i].LastYear=d.AIGodLast[i];
                if(d.AIGodAct!=null)for(int i=0;i<Mathf.Min(d.AIGodAct.Length,c.Gods.Count);i++)c.Gods[i].Actions=d.AIGodAct[i];
                // V9.4.0 恢复性格向量与历史日志（旧档无字段则保持默认性格/空历史）
                if(d.AIGodZeal!=null)for(int i=0;i<Mathf.Min(d.AIGodZeal.Length,c.Gods.Count);i++)c.Gods[i].Zeal=d.AIGodZeal[i];
                if(d.AIGodIntv!=null)for(int i=0;i<Mathf.Min(d.AIGodIntv.Length,c.Gods.Count);i++)c.Gods[i].Intervene=d.AIGodIntv[i];
                if(d.AIGodExp!=null)for(int i=0;i<Mathf.Min(d.AIGodExp.Length,c.Gods.Count);i++)c.Gods[i].Expand=d.AIGodExp[i];
                if(d.AIGodPrd!=null)for(int i=0;i<Mathf.Min(d.AIGodPrd.Length,c.Gods.Count);i++)c.Gods[i].Prudent=d.AIGodPrd[i];
                c.History.Clear();
                if(d.AiHistory!=null)c.History.AddRange(d.AiHistory);
            }
            // V6.1.9 恢复加速冷冻运行态（旧存档无字段则默认不冷冻、累计0）
            s.CryoAccumYears=d.CryoAccum; s.CryoActive=d.CryoActive; s.CryoRemainSec=Mathf.Max(0,d.CryoRemain);
            // V9.2.2 人口周期律（旧档无字段给安全默认：土地系数1.2、周期0恢复）
            s.LandIntegrity=d.LandIntegrity<=0f?1.2f:d.LandIntegrity;
            s.PeakPop=d.PeakPop<=0f?s.Pop:d.PeakPop;
            s.DynastyAge=d.DynastyAge; s.CyclePhase=d.CyclePhase;
            _gm.Population?.BindAfterLoad();   // 对齐朝代时钟，防读档首年误判新朝
            // V6.8.0 恢复奇观 / 成就（旧档无字段给空，不报错；视图由 WonderSystem.Tick 自愈）
            s.Wonders.Clear();
            if(d.WId!=null)
                for(int i=0;i<d.WId.Length;i++)
                    s.Wonders.Add(new WonderRuntime{
                        Id=d.WId[i],
                        BuiltYear=d.WYear!=null&&i<d.WYear.Length?d.WYear[i]:0,
                        X=d.WX!=null&&i<d.WX.Length?d.WX[i]:0,
                        Z=d.WZ!=null&&i<d.WZ.Length?d.WZ[i]:0});
            s.Achievements=new System.Collections.Generic.List<string>(d.Achievements??System.Array.Empty<string>());
            s.WonderAuto=d.WonderAuto;
            // V9.6.6 ISaveable 扩展区恢复：逐个按键匹配注册系统，损坏单系统跳过不影响主流程
            if (d.ExtKeys != null && d.ExtKeys.Length > 0 && d.ExtVals != null)
            {
                for (int i = 0; i < d.ExtKeys.Length && i < d.ExtVals.Length; i++)
                {
                    var sys = _saveables.Find(x => x.SaveKey == d.ExtKeys[i]);
                    if (sys == null) continue;
                    try { sys.Deserialize(d.ExtVals[i]); }
                    catch (Exception e) { Debug.LogWarning("[Save] ISaveable restore fail " + d.ExtKeys[i] + ": " + e.Message); }
                }
            }
            s.Running=true;
        }

        private static void RestoreDict(Dictionary<string,float> target,string[] keys,float[] vals)
        {
            target.Clear();
            if(keys==null||vals==null)return;
            int n=Mathf.Min(keys.Length,vals.Length);
            for(int i=0;i<n;i++)target[keys[i]]=vals[i];
        }

        // ---------- 导出 / 导入 ----------
        public string Export()=>JsonUtility.ToJson(Snapshot(),true);
        public bool Import(string json)
        {
            try{ Apply(JsonUtility.FromJson<SaveData>(json));_gm.AddEvent("good","📥 存档导入成功");return true; }
            catch(Exception e){_gm.AddEvent("bad","导入失败："+e.Message);return false;}
        }

        private void Update()
        {
            // V9.6.6 异步存档提交队列：后台算完校验和 → 主线程原子写（PlayerPrefs 主线程）；锁保护桌面端跨线程入队
            while (true)
            {
                PendingSave e;
                lock (_pendingLock)
                {
                    if (_pendingEncode.Count == 0) break;
                    e = _pendingEncode.Dequeue();
                }
                _busyAsync = false;
                if (string.IsNullOrEmpty(e.Hex)) { _gm?.AddEvent("bad", "存档后台序列化失败"); continue; }
                try
                {
                    var d = JsonUtility.FromJson<SaveData>(e.JsonNoChecksum);
                    if (d == null) continue;
                    d.ChecksumHex = e.Hex;
                    string final = JsonUtility.ToJson(d);        // 主线程拼装最终存储 JSON
                    bool ok = AtomicWrite(e.Slot, final, e.SumJson);
                    _gm?.AddEvent(ok ? "good" : "bad", ok ? (e.Slot == 0 ? "🤖 自动" : "💾 已") + "保存到" + (e.Slot == 0 ? "自动槽" : "存档位 " + e.Slot)
                                                           : "存档校验失败，已回滚");
                }
                catch (Exception ex) { Debug.LogError(ex); _gm?.AddEvent("bad", "存档提交失败：" + ex.Message); }
            }
            if(_gm==null||_gm.State==null||!_gm.State.Running||_gm.State.Paused)return;
            _autoTimer+=Time.unscaledDeltaTime;   // 现实时间计时，不受倍速影响
            // V9.6.5 安全模式下自动保存降频至 20s（崩溃恢复更快锚点）
            float interval = CrashGuardSystem.SafeMode ? 20f : AutoSaveInterval;
            if(_autoTimer>=interval)
            {
                _autoTimer=0;
                // V9.7.0 脏标记节流：无数据变更时跳过本轮自动档（同时间隔重置，避免忙轮询）
                if (SaveDirtySystem.ShouldSkipAutoSave()) return;
                AutoSaveNow();
                SaveDirtySystem.MarkClean();
            }
        }
    }
}
