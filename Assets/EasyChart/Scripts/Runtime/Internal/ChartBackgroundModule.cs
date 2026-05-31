using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart
{
    internal sealed class ChartBackgroundModule : IChartModule
    {
        private ChartElement _owner;
        private VisualElement _layer;

        public void Bind(ChartElement owner, ChartKernel kernel)
        {
            _owner = owner;
            if (_owner == null) return;
            _layer = _owner.BackgroundLayerInternal;
            if (_layer == null) return;

            _layer.generateVisualContent += OnGenerateVisualContent;
        }

        public void Unbind()
        {
            if (_layer != null)
            {
                _layer.generateVisualContent -= OnGenerateVisualContent;
            }
            _layer = null;
            _owner = null;
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            if (_owner == null) return;

            var profile = _owner.Profile;
            if (profile == null) return;
            if (profile.background == null) return;

            var bg = profile.background;
            if (!bg.show) return;

            if (_layer == null) return;
            float width = _layer.contentRect.width;
            float height = _layer.contentRect.height;
            if (width <= 0 || height <= 0) return;

            var painter = ctx.painter2D;

            var fill = bg.textureFill;
            var tex = fill != null ? fill.texture : null;
            if (tex == null)
            {
                painter.fillColor = fill != null ? fill.color : Color.clear;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, 0));
                painter.LineTo(new Vector2(width, 0));
                painter.LineTo(new Vector2(width, height));
                painter.LineTo(new Vector2(0, height));
                painter.ClosePath();
                painter.Fill();
                return;
            }

            if (Application.isPlaying && tex.wrapMode != TextureWrapMode.Repeat)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
            }

            var mesh = ctx.Allocate(4, 6, tex);
            Color tint = fill != null ? fill.color : Color.white;

            Vector2 tiling = fill != null ? fill.tiling : Vector2.one;
            Vector2 offset = fill != null ? fill.offset : Vector2.zero;

            float u0 = 0f * tiling.x + offset.x;
            float u1 = 1f * tiling.x + offset.x;
            float v0 = 1f * tiling.y + offset.y;
            float v1 = 0f * tiling.y + offset.y;

            mesh.SetNextVertex(new Vertex { position = new Vector3(0, 0, Vertex.nearZ), tint = tint, uv = new Vector2(u0, v0) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(width, 0, Vertex.nearZ), tint = tint, uv = new Vector2(u1, v0) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(width, height, Vertex.nearZ), tint = tint, uv = new Vector2(u1, v1) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(0, height, Vertex.nearZ), tint = tint, uv = new Vector2(u0, v1) });

            mesh.SetNextIndex(0);
            mesh.SetNextIndex(1);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(3);
            mesh.SetNextIndex(0);
        }
    }
}
