using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Colors
{
    internal class NoColors : ColorProviderFromStyleSheet
    {
        public const string Title = "<None>";
        public override string GetTitle() => Title;

        public override bool AllowCustom() => false;
        public override bool ClearOnDirty() => false;

        protected override bool GetClassFromNode(AbstractMaterialNode node, out string ussClass)
        {
            ussClass = string.Empty;
            if (!(node.GetType().GetCustomAttributes(typeof(TitleAttribute), false).FirstOrDefault() is TitleAttribute title))
                return false;

            ussClass = title.title[0];

            return !string.IsNullOrEmpty(ussClass);
        }
    }
}
