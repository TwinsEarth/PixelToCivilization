namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 国策类型（治理路线）。
    /// </summary>
    public enum PolicyType
    {
        /// <summary>休养生息（轻徭薄赋，人口恢复快）。</summary>
        Rest = 0,

        /// <summary>重农抑商（粮食增产，金币略减）。</summary>
        Agriculture = 1,

        /// <summary>通商惠工（金币增产，粮食略减）。</summary>
        Commerce = 2,

        /// <summary>严刑峻法（秩序高，人口与文化略减）。</summary>
        Strict = 3,

        /// <summary>对外开放（金币、文化、科技提升）。</summary>
        Open = 4,

        /// <summary>闭关锁国（文化、科技下降）。</summary>
        Closed = 5,

        /// <summary>科举取士（文化、科技提升）。</summary>
        ImperialExam = 6,

        /// <summary>整军经武（军事优先，民生略减）。</summary>
        Martial = 7,
    }

    /// <summary>
    /// 政策服务（policy 插件实现，Tier-1）。提供当前国策的治理系数，
    /// 供 Economy/Population/Tech/Culture 读取；可切换（带冷却）。
    /// 所有系数为 1 表示无影响。
    /// </summary>
    public interface IPolicyService
    {
        /// <summary>某势力当前国策。</summary>
        PolicyType Current(int faction);

        /// <summary>上次切换国策的年份（-1 表示未切换，使用默认）。</summary>
        int LastSwitchYear(int faction);

        /// <summary>是否可切换（政策有效、冷却已过）。</summary>
        bool CanSwitch(int faction, PolicyType policy);

        /// <summary>尝试切换国策（成功 true）。</summary>
        bool TrySwitch(int faction, PolicyType policy);

        /// <summary>粮食产出系数。</summary>
        float FoodModifier(int faction);

        /// <summary>金币产出系数。</summary>
        float GoldModifier(int faction);

        /// <summary>出生率系数。</summary>
        float BirthModifier(int faction);

        /// <summary>死亡率系数。</summary>
        float DeathModifier(int faction);

        /// <summary>文化产出系数。</summary>
        float CultureModifier(int faction);

        /// <summary>科研产出系数。</summary>
        float ResearchModifier(int faction);
    }
}
