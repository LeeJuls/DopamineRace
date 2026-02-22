namespace EasyChart
{
    public interface IChartRendererSelectionPolicy
    {
        RendererSelection Select(ChartData data);
    }
}
