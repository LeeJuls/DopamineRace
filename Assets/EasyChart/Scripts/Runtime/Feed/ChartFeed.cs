using System;
using UnityEngine;

namespace EasyChart
{
    [Serializable]
    public class ChartFeed
    {
        public string chartId;
        public string chartName;
        public AxisFeed[] axes;
        public SerieFeed[] series;
    }

    [Serializable]
    public class AxisFeed
    {
        public AxisId axisId;
        public string[] labels;
    }

    [Serializable]
    public class SerieFeed
    {
        public string serieId;
        public string name;
        public SerieType type;
        public DataFeed[] datas;
    }

    [Serializable]
    public class DataFeed
    {
        public string id;
        public float x;
        public float y;
        public float z;
        public float value;
        public string name;

        public bool useColor;
        public Color color;
    }
}
