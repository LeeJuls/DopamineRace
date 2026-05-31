namespace EasyChart
{
    /// <summary>
    /// Interface for settings that provide pie-style legend configuration.
    /// Implemented by PieSettings, RingChartSettings, and Pie3DSettings.
    /// </summary>
    public interface IPieLegendProvider
    {
        PieLegendSettings PieLegend { get; }
    }

    /// <summary>
    /// Interface for settings that provide pie aggregation configuration.
    /// Implemented by PieSettings and Pie3DSettings.
    /// </summary>
    public interface IPieAggregationProvider
    {
        bool SortByValue { get; }
        PieAggregationSettings PieAggregation { get; }
    }
}
