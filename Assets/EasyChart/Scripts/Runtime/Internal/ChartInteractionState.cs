using System.Collections.Generic;

namespace EasyChart
{
    public sealed class ChartInteractionState
    {
        private readonly HashSet<string> _hiddenPieSliceIds = new HashSet<string>();

        public ISet<string> HiddenPieSliceIds => _hiddenPieSliceIds;

        public void ClearHiddenPieSliceIds()
        {
            _hiddenPieSliceIds.Clear();
        }
    }
}
