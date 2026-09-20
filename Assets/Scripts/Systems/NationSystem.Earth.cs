// V9.1.1 地球模式国家系统：一块可居住陆块一个主权国家，每国 1..3 座城市；WorldPhase="earth"，不分裂、不吞并、不灭国。
using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    public partial class NationSystem
    {
        /// <summary>地球模式全部城市运行时落点（含玩家/AI、首都/普通城），供城际网络与视图使用。</summary>
        public readonly List<EarthCityRT> EarthCities = new();

        /// <summary>地球模式：玩家中国（长江下游家园）+ 每陆块一国（每次新开局由 PopulateInitialEarth 调用一次）。</summary>
        public void InitEarthNations(Vector3 home)
        {
            if (_terrain == null) _terrain = Object.FindObjectOfType<WorldGenerator>();
            S.Nations.Clear(); S.VillageX.Clear(); S.VillageZ.Clear(); EarthCities.Clear();

            int homeLand = Mathf.Max(1, _terrain != null ? _terrain.ContinentAt(home.x, home.z) : 1);

            foreach (var country in EarthNations.All)
            {
                bool isPlayer = country.Id == "cn";
                float capX, capZ; int capLand;

                if (isPlayer)
                {
                    // 玩家首都即长江下游家园（固定），不做螺旋选址
                    capX = home.x; capZ = home.z; capLand = homeLand;
                }
                else
                {
                    var cap = country.Capital;
                    if (!EarthNationPlacement.Resolve(_terrain, cap.Lon, cap.Lat, country.LandId,
                                                      out capX, out capZ, out capLand,
                                                      country.MinLon, country.MaxLon, country.MinLat, country.MaxLat))
                    {
                        Debug.LogWarning($"[EarthNation] {country.Name} 首都选址失败");
                        continue;
                    }
                }

                int nationId = S.Nations.Count;
                var nation = new NationEntity
                {
                    Id = nationId,
                    Name = country.Name,
                    ColorHex = EarthNations.FactionHex(country.Faction),
                    Faction = country.Faction,
                    Cx = capX, Cz = capZ,
                    VillageId = S.VillageX.Count,
                    IsPlayer = isPlayer, Alive = true,
                    ContinentId = Mathf.Max(1, capLand),
                    Pop = isPlayer ? S.Pop : country.BasePop,
                    Note = country.Capital.Name,
                };
                nation.Power = nation.Pop;
                S.Nations.Add(nation);
                S.VillageX.Add(capX); S.VillageZ.Add(capZ);

                // 城市落点：首都 + 其余 1..2 城
                for (int ci = 0; ci < country.Cities.Count; ci++)
                {
                    var city = country.Cities[ci];
                    float x, z; int landUsed;
                    if (isPlayer && ci == 0) { x = home.x; z = home.z; landUsed = homeLand; }
                    else if (!EarthNationPlacement.Resolve(_terrain, city.Lon, city.Lat, country.LandId,
                                                           out x, out z, out landUsed,
                                                           country.MinLon, country.MaxLon, country.MinLat, country.MaxLat))
                    {
                        Debug.LogWarning($"[EarthNation] {country.Name}·{city.Name} 选址失败");
                        continue;
                    }
                    EarthCities.Add(new EarthCityRT
                    {
                        CountryId = country.Id,
                        CountryName = country.Name,
                        Name = city.Name,
                        NationId = nationId,
                        Faction = country.Faction,
                        LandId = Mathf.Max(1, landUsed),
                        Level = city.Level,
                        IsPlayer = isPlayer,
                        IsCapital = ci == 0,
                        X = x, Z = z,
                    });
                }
            }

            S.PlayerNationId = 0;
            S.WorldPhase = "earth";
            S.PhaseYearsLeft = 0;
            GM.AddEvent("info", $"🌍 地球格局：四大阵营、{S.Nations.Count} 国并立（一洲/分区至多三国、每国1~5城，固定国界不分不合）");
            Debug.Log($"[EarthNation] 国家={S.Nations.Count} 城市={EarthCities.Count}");
        }

        /// <summary>地球模式年度结算：人口/国力缓慢消长，国家存续不灭。</summary>
        void EarthOnYear()
        {
            foreach (var n in S.Nations)
            {
                if (!n.Alive) continue;
                if (n.IsPlayer) n.Pop = S.Pop;
                else
                {
                    float g = Random.value < 0.5f ? -0.004f : 0.004f;
                    n.Pop = Mathf.Clamp(Mathf.RoundToInt(n.Pop * (1f + g)), 8, 2000);
                }
                n.Power = n.Pop;
            }
        }
    }
}
