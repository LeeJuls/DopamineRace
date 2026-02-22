using System;

namespace EasyChart
{
    public static class ProPackage
    {
        private static bool? s_isInstalled;

        public static bool IsInstalled
        {
            get
            {
                if (s_isInstalled.HasValue) return s_isInstalled.Value;
                var t = Type.GetType("EasyChart.Pro.EasyChartProBootstrap, EasyChart.Pro.Runtime");
                s_isInstalled = t != null;
                return s_isInstalled.Value;
            }
        }
    }
}
