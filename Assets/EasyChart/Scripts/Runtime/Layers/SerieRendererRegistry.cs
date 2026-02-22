using System;
using System.Collections.Generic;

namespace EasyChart.Layers
{
    public static class SerieRendererRegistry
    {
        private static readonly Dictionary<EasyChart.SerieType, Func<BaseSeriesRenderer>> s_factories = new Dictionary<EasyChart.SerieType, Func<BaseSeriesRenderer>>();

        public static void Register(EasyChart.SerieType type, Func<BaseSeriesRenderer> factory)
        {
            if (factory == null) return;
            s_factories[type] = factory;
        }

        public static bool TryCreate(EasyChart.SerieType type, out BaseSeriesRenderer renderer)
        {
            renderer = null;
            if (s_factories.TryGetValue(type, out var f) && f != null)
            {
                renderer = f();
                return renderer != null;
            }
            return false;
        }

        public static bool HasFactory(EasyChart.SerieType type)
        {
            return s_factories.ContainsKey(type);
        }
    }
}
