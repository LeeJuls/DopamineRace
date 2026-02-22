using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    internal sealed class ChartGeometryModule : IChartModule, IChartGeometryModule
    {
        private ChartElement _owner;
        private ChartKernel _kernel;

        public void Bind(ChartElement owner, ChartKernel kernel)
        {
            _owner = owner;
            _kernel = kernel;
        }

        public void Unbind()
        {
            _owner = null;
            _kernel = null;
        }

        public void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (_owner == null) return;

            if (_owner.EditorHandleGeometryChangedInternal(evt)) return;

            bool showCartesian = _owner.Data != null && _owner.Data.CoordinateSystem == CoordinateSystemType.Cartesian2D;

            GridLayer grid = _owner.GridLayerInternal;
            if (grid != null) grid.visible = showCartesian;

            AxisLayer axis = _owner.AxisLayerInternal;
            if (axis != null) axis.visible = showCartesian;

            float tx = _owner.IsCategorySmoothTranslatingInternal ? _owner.CategoryScrollOffsetXInternal : 0f;
            float ty = _owner.IsCategorySmoothTranslatingInternal ? _owner.CategoryScrollOffsetYInternal : 0f;
            _owner.ApplyCategoryScrollTranslateInternal(tx, ty);

            _kernel?.InvalidateGeometryChanged(showCartesian, immediate: true);
        }
    }
}
