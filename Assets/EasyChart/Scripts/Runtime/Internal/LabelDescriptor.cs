using UnityEngine;

namespace EasyChart
{
    public enum ChartLabelAnchor
    {
        Center,
        TopLeft,
        Top,
        TopRight,
        Left,
        Right,
        BottomLeft,
        Bottom,
        BottomRight,
    }

    public struct LabelDescriptor
    {
        public string key;
        public string text;

        public bool visible;

        public Vector2 anchorPx;
        public Vector2 offsetPx;

        public ChartLabelAnchor anchor;
        public ChartTextRole role;

        public int zOrder;
        public int priority;

        public bool clipToPlot;

        public float fontSizeOverride;
        public Color colorOverride;

        public float rotationDeg;

        public Color backgroundColor;
        public Texture2D backgroundTexture;
    }
}
