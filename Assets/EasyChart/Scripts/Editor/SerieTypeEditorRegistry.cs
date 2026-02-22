using System.Collections.Generic;
using System;

namespace EasyChart.Editor
{
    public static class SerieTypeEditorRegistry
    {
        private static readonly List<SerieType> s_extraTypes = new List<SerieType>();

        public static void Register(SerieType type)
        {
            if (!s_extraTypes.Contains(type)) s_extraTypes.Add(type);
        }

        public static List<SerieType> GetAllowedTypes()
        {
            var list = new List<SerieType>();
            var names = Enum.GetNames(typeof(SerieType));
            for (int i = 0; i < names.Length; i++)
            {
                if (Enum.TryParse(names[i], out SerieType t) && !list.Contains(t)) list.Add(t);
            }

            for (int i = 0; i < s_extraTypes.Count; i++)
            {
                var t = s_extraTypes[i];
                if (!list.Contains(t)) list.Add(t);
            }

            return list;
        }
    }
}
