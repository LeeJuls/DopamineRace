using UnityEngine;

namespace EasyChart.Layers
{
    public struct TooltipItem
    {
        public string Name;
        public string Value;
        public Color Color;
    }

    public class TooltipContext
    {
        public Vector2 LocalPos;
        public float Width;
        public float Height;
    }
}
