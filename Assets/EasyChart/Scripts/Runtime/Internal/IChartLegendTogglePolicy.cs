using UnityEngine.UIElements;

namespace EasyChart
{
    public interface IChartLegendTogglePolicy
    {
        bool GetNextVisibleState(bool isVisible, PointerDownEvent evt);
    }
}
