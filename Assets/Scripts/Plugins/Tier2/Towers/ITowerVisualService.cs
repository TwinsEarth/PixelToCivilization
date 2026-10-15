namespace PxC.Content.Towers
{
    /// <summary>
    /// 塔防视觉内容服务（TowersPack 实现，Tier-2）。
    /// 视图层据此为每型塔渲染多层收分外观。
    /// </summary>
    public interface ITowerVisualService
    {
        /// <summary>按建筑类型取视觉内容（无匹配 null）。</summary>
        TowerVisualProfile ForType(int typeId);
    }
}
