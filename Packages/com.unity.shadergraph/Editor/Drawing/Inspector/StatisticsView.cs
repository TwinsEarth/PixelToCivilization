using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    class StatisticsView : VisualElement
    {
        Label m_Stats;

        public StatisticsView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/StatisticsView"));

            m_Stats = new Label() { name = "statsInfo" };
            m_Stats.text = string.Empty;

            Add(m_Stats);
        }
    }
}
