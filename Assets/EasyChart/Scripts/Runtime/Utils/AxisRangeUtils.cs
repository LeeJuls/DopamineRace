using UnityEngine;

namespace EasyChart
{
    /// <summary>
    /// Utility class for axis range calculations, including NiceNumbers algorithm.
    /// Shared by 2D and 3D chart renderers.
    /// </summary>
    public static class AxisRangeUtils
    {
        /// <summary>
        /// Apply NiceNumbers range adjustment to min/max values based on axis configuration.
        /// This ensures axis labels display clean, evenly-spaced values (e.g., 0, 2, 4, 6, 8 instead of 0, 1.5, 3, 4.5, 6, 7.5).
        /// </summary>
        /// <param name="axis">Axis configuration containing splitCount and autoRangeRounding settings</param>
        /// <param name="minV">Reference to minimum value to adjust</param>
        /// <param name="maxV">Reference to maximum value to adjust</param>
        public static void ApplyNiceNumberRange(AxisConfig axis, ref float minV, ref float maxV)
        {
            if (axis == null) return;
            if (!axis.autoRangeMin && !axis.autoRangeMax) return;
            if (axis.autoRangeRounding != AutoRangeRoundingMode.NiceNumbers) return;

            int splitCount = Mathf.Max(1, axis.splitCount);
            float range = maxV - minV;
            if (range <= 0) return;

            float rawStep = range / splitCount;
            float niceStep = CalculateNiceStep(rawStep);

            if (axis.autoRangeMin)
            {
                minV = Mathf.Floor(minV / niceStep) * niceStep;
            }
            if (axis.autoRangeMax)
            {
                maxV = Mathf.Ceil(maxV / niceStep) * niceStep;
            }

            float newRange = maxV - minV;
            int actualSplits = Mathf.RoundToInt(newRange / niceStep);
            if (actualSplits < splitCount && axis.autoRangeMax)
            {
                maxV = minV + niceStep * splitCount;
            }
        }

        /// <summary>
        /// Calculate a "nice" step value from a raw step.
        /// Nice steps are 1, 2, 5, or 10 multiplied by powers of 10.
        /// For example: 0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50, 100, etc.
        /// </summary>
        /// <param name="rawStep">The raw step value calculated from range/splitCount</param>
        /// <returns>A nice step value</returns>
        public static float CalculateNiceStep(float rawStep)
        {
            if (rawStep <= 0) return 1f;
            
            // Find the magnitude (power of 10)
            float magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(rawStep)));
            
            // Normalize to 1-10 range
            float normalized = rawStep / magnitude;
            
            // Round to nice number (1, 2, 5, 10)
            float niceNormalized;
            if (normalized <= 1f) niceNormalized = 1f;
            else if (normalized <= 2f) niceNormalized = 2f;
            else if (normalized <= 5f) niceNormalized = 5f;
            else niceNormalized = 10f;
            
            return niceNormalized * magnitude;
        }
    }
}
