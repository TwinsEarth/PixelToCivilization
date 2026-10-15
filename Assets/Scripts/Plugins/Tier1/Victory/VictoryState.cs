using System;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Victory
{
    /// <summary>
    /// 胜利领域状态（纯数据）：已达成类型与年份。
    /// 进度实时读取各服务，不重复存储。
    /// </summary>
    public sealed class VictoryState
    {
        public int AchievedType = (int)VictoryType.None;
        public int AchievedYear = -1;

        public bool IsAchieved => AchievedType != (int)VictoryType.None;

        public void Set(int type, int year)
        {
            AchievedType = type;
            AchievedYear = year;
        }

        public void Restore(int type, int year)
        {
            AchievedType = type;
            AchievedYear = year;
        }
    }
}
