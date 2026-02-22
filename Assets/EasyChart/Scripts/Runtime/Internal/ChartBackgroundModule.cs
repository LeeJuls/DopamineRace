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

            if (Application.isPlaying)
            {
                bool hasPro = ProPackage.IsInstalled;
                var desiredWrap = (hasPro && fill != null && fill.animationType == TextureFillAnimationType.TextureScale)
                    ? TextureWrapMode.Clamp
                    : TextureWrapMode.Repeat;

                if (tex.wrapMode != desiredWrap)
                    tex.wrapMode = desiredWrap;
            }

            var mesh = ctx.Allocate(4, 6, tex);
            Color tint = fill != null ? fill.color : Color.white;

            Vector2 tiling = fill != null ? fill.tiling : Vector2.one;
            Vector2 offset = fill != null ? fill.offset : Vector2.zero;

            if (fill != null && ProPackage.IsInstalled && fill.animationType != TextureFillAnimationType.None)
            {
                float t = Time.realtimeSinceStartup;
                if (fill.animationType == TextureFillAnimationType.TextureUvMove)
                {
                    offset -= fill.uvMoveSpeed * t;
                }
                else if (fill.animationType == TextureFillAnimationType.TextureScale)
                {
                    var baseTiling = tiling;

                    float u = t * fill.scaleSpeed;
                    float c01;

                    Vector2 factor;
                    switch (fill.scaleType)
                    {
                        case TextureFillScaleType.ZoomIn:
                        {
                            float p = Mathf.Repeat(u, 1f);
                            var s = Vector2.Lerp(fill.scaleFrom, fill.scaleTo, p);
                            s.x = Mathf.Max(0.0001f, s.x);
                            s.y = Mathf.Max(0.0001f, s.y);
                            factor = new Vector2(1f / s.x, 1f / s.y);
                            c01 = p;
                            break;
                        }
                        case TextureFillScaleType.ZoomOut:
                        {
                            float p = Mathf.Repeat(u, 1f);
                            var s = Vector2.Lerp(fill.scaleTo, fill.scaleFrom, p);
                            s.x = Mathf.Max(0.0001f, s.x);
                            s.y = Mathf.Max(0.0001f, s.y);
                            factor = new Vector2(1f / s.x, 1f / s.y);
                            c01 = p;
                            break;
                        }
                        case TextureFillScaleType.PingPong:
                        {
                            float p = Mathf.PingPong(u, 1f);
                            var s = Vector2.Lerp(fill.scaleFrom, fill.scaleTo, p);
                            s.x = Mathf.Max(0.0001f, s.x);
                            s.y = Mathf.Max(0.0001f, s.y);
                            factor = new Vector2(1f / s.x, 1f / s.y);
                            c01 = p;
                            break;
                        }
                        case TextureFillScaleType.Sin:
                        default:
                        {
                            float s = Mathf.Sin(u);
                            float p = (s + 1f) * 0.5f;
                            var sc = Vector2.Lerp(fill.scaleFrom, fill.scaleTo, p);
                            sc.x = Mathf.Max(0.0001f, sc.x);
                            sc.y = Mathf.Max(0.0001f, sc.y);
                            factor = new Vector2(1f / sc.x, 1f / sc.y);
                            c01 = p;
                            break;
                        }
                    }

                    factor.x = Mathf.Max(0.0001f, factor.x);
                    factor.y = Mathf.Max(0.0001f, factor.y);
                    tiling = new Vector2(baseTiling.x * factor.x, baseTiling.y * factor.y);
                    offset += (baseTiling - tiling) * 0.5f;

                    if (fill.colorFadeGradient != null)
                        tint *= fill.colorFadeGradient.Evaluate(c01);
                }
                else if (fill.animationType == TextureFillAnimationType.TextureFade)
                {
                    float u = t * fill.colorFadeSpeed;
                    float c01;
                    switch (fill.colorFadeWrap)
                    {
                        case TextureFillColorOverLifeWrap.Loop:
                            c01 = Mathf.Repeat(u, 1f);
                            break;
                        case TextureFillColorOverLifeWrap.Clamp:
                            c01 = Mathf.Clamp01(u);
                            break;
                        case TextureFillColorOverLifeWrap.PingPong:
                        default:
                            c01 = Mathf.PingPong(u, 1f);
                            break;
                    }

                    if (fill.colorFadeGradient != null)
                        tint = fill.colorFadeGradient.Evaluate(c01);
                    else
                        tint = Color.Lerp(fill.colorFadeStart, fill.colorFadeEnd, c01);
                }
            }

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
