namespace EasyChart
{
    internal interface IChartModule
    {
        void Bind(ChartElement owner, ChartKernel kernel);
        void Unbind();
    }
}
