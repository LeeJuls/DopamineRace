using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    internal sealed class ChartTooltipModule : IChartModule
    {
        private ChartElement _owner;
        private VisualElement _cursorLine;
        private Label _tooltip;

        public void Bind(ChartElement owner, ChartKernel kernel)
        {
            _owner = owner;
            if (_owner == null) return;

            var chartArea = _owner.ChartAreaInternal;
            if (chartArea == null) return;

            if (_cursorLine == null)
            {
                _cursorLine = new DashedLineElement();
                _cursorLine.style.position = Position.Absolute;
                _cursorLine.style.width = 1;
                _cursorLine.style.top = 0;
                _cursorLine.style.bottom = 0;
                _cursorLine.visible = false;
                chartArea.Add(_cursorLine);
            }
            else if (_cursorLine.parent == null)
            {
                chartArea.Add(_cursorLine);
            }

            if (_tooltip == null)
            {
                _tooltip = new Label();
                _tooltip.style.position = Position.Absolute;
                _tooltip.style.backgroundColor = new Color(0, 0, 0, 0.8f);
                _tooltip.style.color = Color.white;
                _tooltip.style.paddingTop = 4;
                _tooltip.style.paddingBottom = 4;
                _tooltip.style.paddingLeft = 8;
                _tooltip.style.paddingRight = 8;
                _tooltip.style.borderTopLeftRadius = 4;
                _tooltip.style.borderTopRightRadius = 4;
                _tooltip.style.borderBottomLeftRadius = 4;
                _tooltip.style.borderBottomRightRadius = 4;
                _tooltip.visible = false;
                _tooltip.userData = _owner;
                ChartTextStyleApplier.ApplyLabel(_tooltip, _owner, ChartTextRole.Tooltip);
                chartArea.Add(_tooltip);
            }
            else if (_tooltip.parent == null)
            {
                _tooltip.userData = _owner;
                ChartTextStyleApplier.ApplyLabel(_tooltip, _owner, ChartTextRole.Tooltip);
                chartArea.Add(_tooltip);
            }

            _owner.SetTooltipVisualsInternal(_tooltip, _cursorLine);
        }

        public void Unbind()
        {
            if (_owner != null)
            {
                _owner.SetTooltipVisualsInternal(null, null);
            }

            _owner = null;
        }
    }
}
