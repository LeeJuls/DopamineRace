namespace EasyChart
{
    internal sealed class ChartCategoryScrollModule : IChartModule
    {
        private ChartElement _owner;
        private UnityEngine.UIElements.IVisualElementScheduledItem _item;

        public void Bind(ChartElement owner, ChartKernel kernel)
        {
            _owner = owner;
            if (_owner == null) return;
            _item = _owner.schedule.Execute(_owner.OnCategoryScrollUpdate).Every(16);
        }

        public void Unbind()
        {
            _item?.Pause();
            _item = null;
            _owner = null;
        }
    }
}
