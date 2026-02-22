using System.Collections.Generic;
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

        public List<Color> seriesColors = new List<Color>();
        public int paletteSeed = 0;

        public Color positiveColor = new Color(0.2f, 0.9f, 0.4f, 1f);
        public Color negativeColor = new Color(0.95f, 0.25f, 0.25f, 1f);
        public Color neutralColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        public float disabledAlpha = 0.35f;

        public Color backgroundColor = new Color(0f, 0f, 0f, 0f);
        [Padding4] public Vector4 panelPadding = Vector4.zero;
        public float panelRadius = 0f;

        public Color plotBackgroundColor = new Color(0f, 0f, 0f, 0f);
        public Color plotBorderColor = new Color(0f, 0f, 0f, 0f);
        public float plotBorderWidth = 0f;
    }
}
