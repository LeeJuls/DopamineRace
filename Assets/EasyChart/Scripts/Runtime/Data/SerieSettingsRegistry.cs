using System;
using System.Collections.Generic;

namespace EasyChart
{
    public static class SerieSettingsRegistry
    {
        private static readonly Dictionary<SerieType, Func<BaseSerieSettings>> s_factories = new Dictionary<SerieType, Func<BaseSerieSettings>>();

        public static void Register(SerieType type, Func<BaseSerieSettings> factory)
        {
            if (factory == null) return;
            s_factories[type] = factory;
        }

        public static bool TryCreate(SerieType type, out BaseSerieSettings settings)
        {
            settings = null;
            if (s_factories.TryGetValue(type, out var f) && f != null)
            {
                settings = f();
                return settings != null;
            }
            return false;
        }

        public static bool HasFactory(SerieType type)
        {
            return s_factories.ContainsKey(type);
        }
    }
}
