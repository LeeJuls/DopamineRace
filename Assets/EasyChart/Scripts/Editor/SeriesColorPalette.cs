using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyChart.Editor
{
    /// <summary>
    /// Color set for a single series, containing colors for different chart element types.
    /// </summary>
    [System.Serializable]
    public class SeriesColorSet
    {
        [Tooltip("Line stroke color")]
        public Color lineColor = Color.white;
        
        [Tooltip("Area fill color (typically semi-transparent)")]
        public Color areaColor = new Color(1f, 1f, 1f, 180f / 255f);
        
        [Tooltip("Bar fill color")]
        public Color barColor = Color.white;
        
        [Tooltip("Point/marker color")]
        public Color pointColor = Color.white;

        public SeriesColorSet() { }

        public SeriesColorSet(Color baseColor)
        {
            lineColor = baseColor;
            barColor = baseColor;
            pointColor = baseColor;
            areaColor = new Color(baseColor.r, baseColor.g, baseColor.b, 180f / 255f);
        }

        public SeriesColorSet(Color line, Color area, Color bar, Color point)
        {
            lineColor = line;
            areaColor = area;
            barColor = bar;
            pointColor = point;
        }
    }

    /// <summary>
    /// A complete color palette containing multiple series color sets.
    /// </summary>
    [System.Serializable]
    public class SeriesColorPalette
    {
        public string name = "New Palette";
        
        [Tooltip("Color sets for each series (index 0 = first series, etc.)")]
        public List<SeriesColorSet> colorSets = new List<SeriesColorSet>();

        public SeriesColorPalette() { }

        public SeriesColorPalette(string name, params Color[] baseColors)
        {
            this.name = name;
            colorSets = new List<SeriesColorSet>();
            foreach (var color in baseColors)
            {
                colorSets.Add(new SeriesColorSet(color));
            }
        }

        /// <summary>
        /// Get color set for a series index (wraps around if index exceeds available sets).
        /// </summary>
        public SeriesColorSet GetColorSet(int seriesIndex)
        {
            if (colorSets == null || colorSets.Count == 0)
                return new SeriesColorSet(Color.white);
            
            return colorSets[seriesIndex % colorSets.Count];
        }
    }

}
