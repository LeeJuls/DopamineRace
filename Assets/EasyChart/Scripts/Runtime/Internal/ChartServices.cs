using System;
using EasyChart.Layers;

namespace EasyChart
{
    public sealed class ChartServices
    {
        public IChartRendererSelectionPolicy RendererSelectionPolicy;
        public Func<SerieType, BaseSeriesRenderer> DynamicRendererFactory;
        public ChartInteractionState InteractionState;
        public IChartTooltipPolicy TooltipPolicy;
        public IChartHitTestPolicy HitTestPolicy;
        public IChartInteractionPolicy InteractionPolicy;
        public IChartLegendTogglePolicy LegendTogglePolicy;
        public IChartLayoutModel LayoutModel;

        public ChartServices Clone()
        {
            return new ChartServices
            {
                RendererSelectionPolicy = RendererSelectionPolicy,
                DynamicRendererFactory = DynamicRendererFactory,
                InteractionState = InteractionState,
                TooltipPolicy = TooltipPolicy,
                HitTestPolicy = HitTestPolicy,
                InteractionPolicy = InteractionPolicy,
                LegendTogglePolicy = LegendTogglePolicy,
                LayoutModel = LayoutModel,
            };
        }

        public void Normalize()
        {
            if (RendererSelectionPolicy == null) RendererSelectionPolicy = DefaultChartRendererSelectionPolicy.Instance;
            if (TooltipPolicy == null) TooltipPolicy = DefaultChartTooltipPolicy.Instance;
            if (HitTestPolicy == null) HitTestPolicy = DefaultChartHitTestPolicy.Instance;
            if (InteractionPolicy == null) InteractionPolicy = DefaultChartInteractionPolicy.Instance;
            if (LegendTogglePolicy == null) LegendTogglePolicy = DefaultChartLegendTogglePolicy.Instance;
            if (LayoutModel == null) LayoutModel = new DefaultChartLayoutModel();
            if (InteractionState == null) InteractionState = new ChartInteractionState();
        }

        public void MergeFrom(ChartServices other, bool overwriteExisting = false)
        {
            if (other == null) return;

            if (overwriteExisting || RendererSelectionPolicy == null)
            {
                if (other.RendererSelectionPolicy != null) RendererSelectionPolicy = other.RendererSelectionPolicy;
            }

            if (overwriteExisting || DynamicRendererFactory == null)
            {
                if (other.DynamicRendererFactory != null) DynamicRendererFactory = other.DynamicRendererFactory;
            }

            if (overwriteExisting || InteractionState == null)
            {
                if (other.InteractionState != null) InteractionState = other.InteractionState;
            }

            if (overwriteExisting || TooltipPolicy == null)
            {
                if (other.TooltipPolicy != null) TooltipPolicy = other.TooltipPolicy;
            }

            if (overwriteExisting || HitTestPolicy == null)
            {
                if (other.HitTestPolicy != null) HitTestPolicy = other.HitTestPolicy;
            }

            if (overwriteExisting || InteractionPolicy == null)
            {
                if (other.InteractionPolicy != null) InteractionPolicy = other.InteractionPolicy;
            }

            if (overwriteExisting || LegendTogglePolicy == null)
            {
                if (other.LegendTogglePolicy != null) LegendTogglePolicy = other.LegendTogglePolicy;
            }

            if (overwriteExisting || LayoutModel == null)
            {
                if (other.LayoutModel != null) LayoutModel = other.LayoutModel;
            }
        }

        public void ApplyTo(ChartElement chart, bool setAsServices = true)
        {
            if (chart == null) return;

            if (setAsServices)
            {
                chart.Services = this;
                return;
            }

            if (RendererSelectionPolicy != null) chart.RendererSelectionPolicy = RendererSelectionPolicy;
            if (DynamicRendererFactory != null) chart.DynamicRendererFactory = DynamicRendererFactory;
            if (InteractionState != null) chart.InteractionState = InteractionState;
            if (TooltipPolicy != null) chart.TooltipPolicy = TooltipPolicy;
            if (HitTestPolicy != null) chart.HitTestPolicy = HitTestPolicy;
            if (InteractionPolicy != null) chart.InteractionPolicy = InteractionPolicy;
            if (LegendTogglePolicy != null) chart.LegendTogglePolicy = LegendTogglePolicy;
            if (LayoutModel != null) chart.LayoutModel = LayoutModel;
        }
    }
}
