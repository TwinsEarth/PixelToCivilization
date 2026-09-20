// V9.1.1 地球模式开局落位：玩家中国（长江下游家园全套）+ 每陆块一国、每国 1..3 座城市（Lv1村落/Lv2城镇/Lv3城市）。
// 城市规模/人口随等级；首都配阵营地标；飞鸟鱼群照常；最后注册国家（确定性选址，与 NationSystem 复算结果一致）。
using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;
using PixelToCivilization.Actors;

namespace PixelToCivilization.Systems
{
    public partial class EnvironmentSystem
    {
        /// <summary>地球模式 PopulateInitial 分流：一陆块一国、每国至多三城。</summary>
        void PopulateInitialEarth()
        {
            AdoptSun();
            _village = _terrain.SettlementCenter;

            // 1) 玩家主村（中国，朱红全套：祭坛/棚屋/农田/设施/道路/旗/码头渔船/小车）
            InitialSettlementBuilder.Build(GM, _terrain, _village, EarthNations.FactionHex(1), true, 0);
            for (int i = 0; i < GameConstants.StartPop && i < 120; i++)
            {
                var p = VillagePoint(15f);
                SpawnAgent(p.x, p.y, _village.x, _village.z);
            }
            InitialSettlementBuilder.BuildLandmark(GM, _terrain, _village.x + 6f, _village.z - 6f, 1);

            // 2) 其余国家与城市（中国除首都外的北京/莫斯科也在此建）
            int settleIdx = 1;
            int citiesBuilt = 0, failed = 0;
            foreach (var country in EarthNations.All)
            {
                for (int ci = 0; ci < country.Cities.Count; ci++)
                {
                    var city = country.Cities[ci];
                    bool isPlayerCapital = country.Id == "cn" && ci == 0;
                    if (isPlayerCapital) continue; // 玩家首都=家园，已建

                    float ax, az; int landUsed;
                    if (!EarthNationPlacement.Resolve(_terrain, city.Lon, city.Lat, country.LandId,
                                                      out ax, out az, out landUsed,
                                                      country.MinLon, country.MaxLon, country.MinLat, country.MaxLat))
                    {
                        failed++;
                        Debug.LogWarning($"[PopulateEarth] {country.Name}·{city.Name} 找不到合格陆地，跳过");
                        continue;
                    }

                    var cap = new Vector3(ax, 0, az);
                    // V9.1.2 城市增多（38 城）：Lv3 用小型聚落(sizeTier2=7建筑)，Lv1/Lv2 用迷你聚落(sizeTier3=4建筑)，控制总建筑量与算力
                    int sizeTier = city.Level >= 3 ? 2 : 3;
                    InitialSettlementBuilder.Build(GM, _terrain, cap, EarthNations.FactionHex(country.Faction),
                                                   false, settleIdx++, sizeTier);
                    int pop = city.Level >= 3 ? Random.Range(24, 35)
                             : city.Level == 2 ? Random.Range(14, 23)
                             : Random.Range(7, 13);
                    for (int k = 0; k < pop; k++)
                    {
                        var p = PointAround(ax, az, city.Level >= 3 ? 13f : 11f);
                        SpawnAgent(p.x, p.y, ax, az);
                    }
                    if (city.Capital)
                        InitialSettlementBuilder.BuildLandmark(GM, _terrain, ax + 6f, az - 6f, country.Faction);
                    citiesBuilt++;
                }
            }

            // 3) 飞鸟群 / 鱼群（绕玩家家园）
            if (_birds == null)
            {
                _birds = GM.gameObject.GetComponent<WildlifeBirds>();
                if (_birds == null) _birds = GM.gameObject.AddComponent<WildlifeBirds>();
            }
            _birds.Init(_village);
            if (_fish == null)
            {
                _fish = GM.gameObject.GetComponent<WildlifeFish>();
                if (_fish == null) _fish = GM.gameObject.AddComponent<WildlifeFish>();
            }
            _fish.Init(_village, _terrain);

            // 4) 注册国家（一陆块一国 + 城市运行时落点）
            GM.Nation?.InitEarthNations(_village);
            Debug.Log($"[PopulateEarth] 建成城市 {citiesBuilt}（含玩家首都），失败 {failed}");
        }
    }
}
