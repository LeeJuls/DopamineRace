using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace EasyChart
{
    internal sealed class ChartKernel
    {
        private readonly ChartElement _owner;
        private readonly ChartRefreshPipeline _pipeline;
        private readonly ChartRefreshCallbacks _refreshCallbacks;

        private readonly List<IChartModule> _modules = new List<IChartModule>(8);
        private readonly List<IChartPointerModule> _pointerModules = new List<IChartPointerModule>(2);
        private readonly List<IChartGeometryModule> _geometryModules = new List<IChartGeometryModule>(2);
        private readonly List<IChartRefreshStageModule> _refreshStageModules = new List<IChartRefreshStageModule>(2);

        private bool _modulesInstalled;
        private bool _attached;

        public ChartKernel(ChartElement owner, ChartRefreshPipeline pipeline, ChartRefreshCallbacks refreshCallbacks)
        {
            _owner = owner;
            _pipeline = pipeline;
            _refreshCallbacks = refreshCallbacks;
        }

        public void ExecuteRebuildRenderers()
        {
            for (int i = 0; i < _refreshStageModules.Count; i++)
            {
                _refreshStageModules[i]?.OnRebuildRenderers();
            }

            _refreshCallbacks.RebuildRenderers?.Invoke();
        }

        public void ExecuteCalculateRange()
        {
            for (int i = 0; i < _refreshStageModules.Count; i++)
            {
                _refreshStageModules[i]?.OnCalculateRange();
            }

            _refreshCallbacks.CalculateRange?.Invoke();
        }

        public void ExecuteRefreshAxisLayer()
        {
            for (int i = 0; i < _refreshStageModules.Count; i++)
            {
                _refreshStageModules[i]?.OnRefreshAxisLayer();
            }

            _refreshCallbacks.RefreshAxisLayer?.Invoke();
        }

        public void ExecuteRefreshGridLayer()
        {
            for (int i = 0; i < _refreshStageModules.Count; i++)
            {
                _refreshStageModules[i]?.OnRefreshGridLayer();
            }

            _refreshCallbacks.RefreshGridLayer?.Invoke();
        }

        public void ExecuteRefreshSeriesRenderers()
        {
            for (int i = 0; i < _refreshStageModules.Count; i++)
            {
                _refreshStageModules[i]?.OnRefreshSeriesRenderers();
            }

            _refreshCallbacks.RefreshSeriesRenderers?.Invoke();
        }

        public void ExecuteRefreshLayersNoLegend()
        {
            for (int i = 0; i < _refreshStageModules.Count; i++)
            {
                _refreshStageModules[i]?.OnRefreshLayersNoLegend();
            }

            _refreshCallbacks.RefreshLayersNoLegend?.Invoke();
        }

        public void ExecuteRefreshLegendDeferred()
        {
            for (int i = 0; i < _refreshStageModules.Count; i++)
            {
                _refreshStageModules[i]?.OnRefreshLegendDeferred();
            }

            _refreshCallbacks.RefreshLegendDeferred?.Invoke();
        }

        public void ExecutePlayAnimation()
        {
            for (int i = 0; i < _refreshStageModules.Count; i++)
            {
                _refreshStageModules[i]?.OnPlayAnimation();
            }

            _refreshCallbacks.PlayAnimation?.Invoke();
        }

        public void InstallDefaultModules()
        {
            if (_modulesInstalled) return;
            _modulesInstalled = true;

            AddModule(new ChartCategoryScrollModule());
            AddModule(new ChartTooltipModule());
            AddModule(new ChartPointerInteractionModule());
            AddModule(new ChartGeometryModule());
            AddModule(new ChartBackgroundModule());
            AddModule(new ChartRefreshStagesModule());
        }

        public void AddModule(IChartModule module)
        {
            if (module == null) return;
            _modules.Add(module);
            if (module is IChartPointerModule pm) _pointerModules.Add(pm);
            if (module is IChartGeometryModule gm) _geometryModules.Add(gm);
            if (module is IChartRefreshStageModule rm) _refreshStageModules.Add(rm);
            if (_attached) module.Bind(_owner, this);
        }

        public void OnAttachToPanel()
        {
            if (_attached) return;
            _attached = true;

            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i]?.Bind(_owner, this);
            }
        }

        public void OnDetachFromPanel()
        {
            if (!_attached) return;
            _attached = false;

            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i]?.Unbind();
            }
        }

        public void OnPointerMove(PointerMoveEvent evt)
        {
            for (int i = 0; i < _pointerModules.Count; i++)
            {
                _pointerModules[i]?.OnPointerMove(evt);
            }
        }

        public void OnPointerLeave(PointerLeaveEvent evt)
        {
            for (int i = 0; i < _pointerModules.Count; i++)
            {
                _pointerModules[i]?.OnPointerLeave(evt);
            }
        }

        public void OnGeometryChanged(GeometryChangedEvent evt)
        {
            for (int i = 0; i < _geometryModules.Count; i++)
            {
                _geometryModules[i]?.OnGeometryChanged(evt);
            }
        }

        public void Invalidate(ChartDirtyReason reason, bool immediate)
        {
            if (_owner == null) return;
            if (_pipeline == null) return;

            var stage = MapToStages(reason);
            if (stage == ChartRefreshStage.None) return;

            if (immediate) _pipeline.RequestImmediate(stage);
            else _pipeline.Request(stage);
        }

        public void Queue(ChartDirtyReason reason)
        {
            if (_owner == null) return;
            if (_pipeline == null) return;

            var stage = MapToStages(reason);
            if (stage == ChartRefreshStage.None) return;

            QueueStages(stage);
        }

        public void QueueStages(ChartRefreshStage stage)
        {
            if (_owner == null) return;
            if (_pipeline == null) return;
            if (stage == ChartRefreshStage.None) return;

            _pipeline.Add(stage);
        }

        private ChartRefreshStage MapToStages(ChartDirtyReason reason)
        {
            if (reason == ChartDirtyReason.None) return ChartRefreshStage.None;

            ChartRefreshStage stage = ChartRefreshStage.None;

            if ((reason & ChartDirtyReason.DataAssigned) != 0)
                stage |= ChartRefreshStage.RebuildRenderers | ChartRefreshStage.CalculateRange | ChartRefreshStage.LayersNoLegend | ChartRefreshStage.Legend;

            if ((reason & ChartDirtyReason.DataMutated) != 0)
                stage |= ChartRefreshStage.CalculateRange | ChartRefreshStage.LayersNoLegend | ChartRefreshStage.Legend;

            if ((reason & ChartDirtyReason.GeometryChanged) != 0)
                stage |= ChartRefreshStage.SeriesRenderers | ChartRefreshStage.Legend;

            if ((reason & ChartDirtyReason.CategoryWindowChanged) != 0)
                stage |= ChartRefreshStage.AxisLayer | ChartRefreshStage.Grid | ChartRefreshStage.SeriesRenderers;

            if ((reason & ChartDirtyReason.LegendTogglePieSlice) != 0)
                stage |= ChartRefreshStage.SeriesRenderers | ChartRefreshStage.Legend;

            if ((reason & ChartDirtyReason.LegendToggleSeriesVisibility) != 0)
                stage |= ChartRefreshStage.RebuildRenderers | ChartRefreshStage.CalculateRange | ChartRefreshStage.AxisLayer | ChartRefreshStage.Grid | ChartRefreshStage.SeriesRenderers | ChartRefreshStage.Legend;

            return stage;
        }

        public void InvalidateDataAssignedWithAnimation(ChartData data, bool immediate)
        {
            var stage = MapToStages(ChartDirtyReason.DataAssigned);
            if (data != null && data.animationDuration > 0f) stage |= ChartRefreshStage.PlayAnimation;

            if (immediate) _pipeline.RequestImmediate(stage);
            else _pipeline.Request(stage);
        }

        public void InvalidateDataMutated(bool rebuildRenderers, bool playAnimation, bool immediate)
        {
            var stage = MapToStages(rebuildRenderers ? ChartDirtyReason.DataAssigned : ChartDirtyReason.DataMutated);
            if (playAnimation) stage |= ChartRefreshStage.PlayAnimation;

            if (immediate) _pipeline.RequestImmediate(stage);
            else _pipeline.Request(stage);
        }

        public void InvalidateGeometryChanged(bool showCartesian, bool immediate)
        {
            var stage = MapToStages(ChartDirtyReason.GeometryChanged);
            if (showCartesian) stage |= ChartRefreshStage.AxisLayer | ChartRefreshStage.Grid;

            if (immediate) _pipeline.RequestImmediate(stage);
            else _pipeline.Request(stage);
        }
    }
}
