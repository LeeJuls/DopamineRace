using UnityEngine.UIElements;

namespace EasyChart
{
    internal sealed class ChartPointerInteractionModule : IChartModule, IChartPointerModule
    {
        private ChartElement _owner;

        public void Bind(ChartElement owner, ChartKernel kernel)
        {
            _owner = owner;
        }

        public void Unbind()
        {
            _owner = null;
        }

        public void OnPointerMove(PointerMoveEvent evt)
        {
            if (_owner == null) return;

            var p = _owner.InteractionPolicy ?? DefaultChartInteractionPolicy.Instance;
            p.OnPointerMove(
                _owner.TooltipControllerInternal,
                _owner,
                evt.position,
                _owner.Data,
                _owner.RenderersInternal,
                _owner.ChartAreaInternal,
                _owner.PaddingInternal,
                _owner.IsCartesianTransposedInternal,
                _owner.IsCategorySmoothTranslatingInternal,
                _owner.TooltipLabelInternal,
                _owner.CursorLineInternal);
        }

        public void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (_owner == null) return;

            var p = _owner.InteractionPolicy ?? DefaultChartInteractionPolicy.Instance;
            p.OnPointerLeave(_owner.TooltipControllerInternal, _owner.RenderersInternal, _owner.TooltipLabelInternal, _owner.CursorLineInternal);
        }
    }
}
