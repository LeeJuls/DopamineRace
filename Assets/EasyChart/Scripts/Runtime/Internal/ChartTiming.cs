namespace EasyChart
{
    public static class ChartTiming
    {
        /// <summary>Target animation/update interval in milliseconds (read from Settings)</summary>
        public static int UpdateIntervalMs
        {
            get
            {
                var settings = EasyChartSettings.Instance;
                int fps = settings?.animationFps ?? 30;
                return 1000 / fps;
            }
        }

        /// <summary>Target animation/update interval in seconds (read from Settings)</summary>
        public static float UpdateIntervalSec => UpdateIntervalMs / 1000f;

        /// <summary>Fallback delta time when real dt is unavailable or out of range (≈60 FPS)</summary>
        public const float FallbackDeltaTime = 0.016f;
    }
}
