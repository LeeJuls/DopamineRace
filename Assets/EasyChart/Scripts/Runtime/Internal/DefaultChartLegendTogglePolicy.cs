using UnityEngine.UIElements;

namespace EasyChart
{
    public sealed class DefaultChartLegendTogglePolicy : IChartLegendTogglePolicy
    {
        public static readonly DefaultChartLegendTogglePolicy Instance = new DefaultChartLegendTogglePolicy();

        private DefaultChartLegendTogglePolicy() { }

        public bool GetNextVisibleState(bool isVisible, PointerDownEvent evt)
        {
            return !isVisible;
        }
    }
}
