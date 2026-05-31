using UnityEngine;

namespace EasyChart
{
    /// <summary>
    /// Attribute to mark List&lt;TextureFXLayer&gt; fields for custom editor drawing
    /// with copy/paste functionality.
    /// </summary>
    public class TextureFXLayersAttribute : PropertyAttribute
    {
        public string Label { get; private set; }

        public TextureFXLayersAttribute(string label = "Texture FX Layers")
        {
            Label = label;
        }
    }
}
