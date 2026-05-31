using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart
{
    /// <summary>
    /// VisualElement renderer for TextureFXSettings.
    /// Uses TextureFXAnimator and TextureFXDrawHelper for reusable logic.
    /// </summary>
    public class TextureFXRenderer : VisualElement
    {
        private TextureFXSettings _settings;
        private float _time;

        public TextureFXRenderer()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
        }

        public void SetSettings(TextureFXSettings settings)
        {
            _settings = settings;
            MarkDirtyRepaint();
        }

        public void Tick(float deltaTime)
        {
            if (_settings == null || !_settings.enabled) return;
            
            if (TextureFXBridge.HasAnyAnimation(_settings.layers))
            {
                _time += deltaTime;
                MarkDirtyRepaint();
            }
        }

        public void ResetTime()
        {
            _time = 0f;
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_settings == null || !_settings.enabled) return;
            
            var fullRect = contentRect;
            if (fullRect.width <= 0 || fullRect.height <= 0) return;

            // Apply padding
            var rect = TextureFXBridge.ApplyPadding(fullRect, _settings.padding);
            if (rect.width <= 0 || rect.height <= 0) return;

            // Draw background color first
            TextureFXBridge.DrawSolidRect(mgc, rect, _settings.backgroundColor);

            // Draw all layers (Pro only)
            TextureFXBridge.DrawLayers(mgc, rect, _settings.layers, _time);
        }
    }
}
