namespace EasyChart
{
    public sealed class DefaultChartRendererSelectionPolicy : IChartRendererSelectionPolicy
    {
        public static readonly DefaultChartRendererSelectionPolicy Instance = new DefaultChartRendererSelectionPolicy();

        private DefaultChartRendererSelectionPolicy() { }

        public RendererSelection Select(ChartData data)
        {
            return ChartRendererManager.Select(data);
        }
    }
}
