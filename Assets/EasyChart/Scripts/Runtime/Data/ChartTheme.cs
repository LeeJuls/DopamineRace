using UnityEngine;

namespace EasyChart
{
    [CreateAssetMenu(fileName = "NewChartTheme", menuName = "EasyChart/Chart Theme")]
    public sealed class ChartTheme : ScriptableObject
    {
        public Object primaryFont;
        public Object monoFont;
        public float fontScale = 1f;

        public float axisFontSize = -1f;
        public float legendFontSize = -1f;
        public float tooltipFontSize = -1f;
        public float seriesLabelFontSize = -1f;

        public float titleFontSize = -1f;
        public float subtitleFontSize = -1f;

        /// <summary>
        /// Base profile used as template when creating new profiles.
        /// If null, the first profile in current library will be used as fallback.
        /// </summary>
        public ChartProfile baseProfile;
    }
}
