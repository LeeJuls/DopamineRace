namespace EasyChart
{
    internal sealed class ChartCategoryScrollModule : IChartModule, IChartRefreshStageModule, IChartGeometryModule
    {
        private ChartElement _owner;
        private UnityEngine.UIElements.IVisualElementScheduledItem _item;

        public void Bind(ChartElement owner, ChartKernel kernel)
        {
            _owner = owner;
            if (_owner == null) return;
            UpdateUpdateLoopState();
        }

        public void Unbind()
        {
            _item?.Pause();
            _item = null;
            _owner = null;
        }

        private void StartUpdateLoop()
        {
            if (_owner == null) return;
            if (_item != null) return;
            if (_owner.panel == null) return;
            _item = _owner.schedule.Execute(_owner.OnCategoryScrollUpdate).Every(ChartTiming.UpdateIntervalMs);
        }

        private void StopUpdateLoop()
        {
            if (_item == null) return;
            _item.Pause();
            _item = null;
        }

        private static int ClampCategoryVisibleCount(AxisConfig axis, int labelsCount)
        {
            if (labelsCount <= 0) return 0;
            if (axis != null && axis.autoTicks) return labelsCount;
            int v = axis != null ? axis.splitCount : 0;
            if (v < 2) v = 2;
            if (v > labelsCount) v = labelsCount;
            return v;
        }

        private bool NeedsUpdateLoop()
        {
            if (_owner == null) return false;
            if (_owner.panel == null) return false;
            if (_owner.Data == null) return false;
            if (_owner.Data.CoordinateSystem != CoordinateSystemType.Cartesian2D) return false;

            var controller = _owner.CategoryScrollControllerInternal;
            if (controller != null && controller.SmoothTranslating) return true;

            AxisId xAxisId = _owner.GetMappedXAxisIdInternal();
            AxisId yAxisId = _owner.GetMappedYAxisIdInternal();

            var xAxis = _owner.GetAxisInternal(xAxisId);
            var yAxis = _owner.GetAxisInternal(yAxisId);

            bool needsX = false;
            if (xAxis != null && xAxis.axisType == AxisType.Category && xAxis.labels != null && xAxis.labels.Count > 0)
            {
                int count = xAxis.labels.Count;
                int visible = ClampCategoryVisibleCount(xAxis, count);
                bool overflow = count > visible;
                needsX = overflow && xAxis.categoryAutoScroll;
            }

            bool needsY = false;
            if (yAxis != null && yAxis.axisType == AxisType.Category && yAxis.labels != null && yAxis.labels.Count > 0)
            {
                int count = yAxis.labels.Count;
                int visible = ClampCategoryVisibleCount(yAxis, count);
                bool overflow = count > visible;
                needsY = overflow && yAxis.categoryAutoScroll;
            }

            return needsX || needsY;
        }

        private void UpdateUpdateLoopState()
        {
            if (!NeedsUpdateLoop())
            {
                StopUpdateLoop();
                return;
            }

            StartUpdateLoop();
        }

        public void OnGeometryChanged(UnityEngine.UIElements.GeometryChangedEvent evt)
        {
            UpdateUpdateLoopState();
        }

        public void OnRebuildRenderers() => UpdateUpdateLoopState();
        public void OnCalculateRange() => UpdateUpdateLoopState();
        public void OnRefreshAxisLayer() => UpdateUpdateLoopState();
        public void OnRefreshGridLayer() { }
        public void OnRefreshSeriesRenderers() => UpdateUpdateLoopState();
        public void OnRefreshLayersNoLegend() { }
        public void OnRefreshLegendDeferred() { }
        public void OnPlayAnimation() => UpdateUpdateLoopState();
    }
}
