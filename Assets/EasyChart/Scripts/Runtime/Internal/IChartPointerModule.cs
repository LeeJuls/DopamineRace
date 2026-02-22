using UnityEngine.UIElements;

namespace EasyChart
{
    internal interface IChartPointerModule
    {
        void OnPointerMove(PointerMoveEvent evt);
        void OnPointerLeave(PointerLeaveEvent evt);
    }
}
