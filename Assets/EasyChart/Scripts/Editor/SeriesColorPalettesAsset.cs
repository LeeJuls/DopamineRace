using System.Collections.Generic;
using UnityEngine;

namespace EasyChart.Editor
{
    /// <summary>
    /// ScriptableObject to store color palettes.
    /// </summary>
    [CreateAssetMenu(fileName = "SeriesColorPalettes", menuName = "EasyChart/Series Color Palettes")]
    public class SeriesColorPalettesAsset : ScriptableObject
    {
        public List<SeriesColorPalette> palettes = new List<SeriesColorPalette>();

        /// <summary>
        /// Get default palettes if none exist.
        /// </summary>
        public static List<SeriesColorPalette> GetDefaultPalettes()
        {
            return new List<SeriesColorPalette>
            {
                // 1. Google Charts - Material Design, most popular
                new SeriesColorPalette("Google Charts",
                    new Color32(100, 160, 255, 255),  // Google Blue (brightened)
                    new Color32(255, 100,  90, 255),  // Google Red (brightened)
                    new Color32(255, 210,  50, 255),  // Google Yellow (brightened)
                    new Color32( 60, 200, 130, 255),  // Google Green (brightened)
                    new Color32(200, 110, 220, 255),  // Purple (brightened)
                    new Color32( 50, 210, 230, 255),  // Cyan (brightened)
                    new Color32(255, 145, 100, 255),  // Deep Orange (brightened)
                    new Color32(200, 200,  80, 255)   // Lime (brightened)
                ),
                
                // 2. D3 Category10 - Web charts standard, extra bright
                new SeriesColorPalette("D3 Category",
                    new Color32(120, 190, 245, 255),  // Blue (extra bright)
                    new Color32(255, 190, 100, 255),  // Orange (extra bright)
                    new Color32(120, 225, 120, 255),  // Green (extra bright)
                    new Color32(255, 120, 120, 255),  // Red (extra bright)
                    new Color32(210, 175, 245, 255),  // Purple (extra bright)
                    new Color32(210, 165, 155, 255),  // Brown (extra bright)
                    new Color32(255, 180, 235, 255),  // Pink (extra bright)
                    new Color32(200, 200, 200, 255)   // Gray (extra bright)
                ),
                
                // 3. Tableau 10 - Industry standard, extra bright
                new SeriesColorPalette("Tableau 10",
                    new Color32(130, 180, 230, 255),  // Steel Blue (extra bright)
                    new Color32(255, 195, 120, 255),  // Orange (extra bright)
                    new Color32(150, 225, 150, 255),  // Green (extra bright)
                    new Color32(255, 150, 150, 255),  // Red (extra bright)
                    new Color32(180, 235, 230, 255),  // Teal (extra bright)
                    new Color32(225, 185, 215, 255),  // Purple (extra bright)
                    new Color32(255, 205, 215, 255),  // Pink (extra bright)
                    new Color32(215, 185, 165, 255)   // Brown (extra bright)
                ),
                
                // 4. Modern Blue - Clean modern style, brightened
                new SeriesColorPalette("Modern Blue",
                    new Color32( 50, 160, 255, 255),  // Blue (brightened)
                    new Color32(255, 180,  50, 255),  // Orange (brightened)
                    new Color32( 90, 230, 130, 255),  // Green (brightened)
                    new Color32(255, 100,  90, 255),  // Red (brightened)
                    new Color32(200, 120, 250, 255),  // Purple (brightened)
                    new Color32(120, 220, 255, 255),  // Sky Blue (brightened)
                    new Color32(255,  90, 130, 255),  // Pink (brightened)
                    new Color32(255, 230,  60, 255)   // Yellow (brightened)
                ),
                
                // 5. Cool Ocean - Cool tones, brightened
                new SeriesColorPalette("Cool Ocean",
                    new Color32( 90, 235, 130, 255),  // Emerald Green (brightened)
                    new Color32( 50, 230, 220, 255),  // Cyan Green (brightened)
                    new Color32( 50, 160, 255, 255),  // Blue (brightened)
                    new Color32( 90, 130, 210, 255),  // Deep Blue (brightened)
                    new Color32(140, 185, 250, 255),  // Light Blue (brightened)
                    new Color32(170, 120, 220, 255),  // Purple (brightened)
                    new Color32( 80, 180, 200, 255),  // Cyan (brightened)
                    new Color32(120, 210, 180, 255)   // Mint Green (brightened)
                ),
                
                // 6. Warm Sunset - Warm tones, brightened
                new SeriesColorPalette("Warm Sunset",
                    new Color32(255, 110, 100, 255),  // Coral Red (brightened)
                    new Color32(255, 190,  60, 255),  // Orange (brightened)
                    new Color32(255, 230,  60, 255),  // Gold Yellow (brightened)
                    new Color32(230, 140, 185, 255),  // Pink Purple (brightened)
                    new Color32(200, 100, 150, 255),  // Wine Red (brightened)
                    new Color32(255, 165, 120, 255),  // Orange Red (brightened)
                    new Color32(250, 130,  80, 255),  // Deep Orange (brightened)
                    new Color32(220, 180, 110, 255)   // Brown Yellow (brightened)
                )
            };
        }
    }
}
