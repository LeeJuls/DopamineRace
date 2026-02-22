    using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart
{
    public sealed class ChartLabelController
    {
        private sealed class Entry
        {
            public Label Label;
            public bool Used;
            public int ZOrder;
            public bool ClipToPlot;

            public float Scale;
            public float ScaleFrom;
            public float ScaleTo;
            public float ScaleAnimStartTime;
            public bool ScaleAnimating;
            public bool HideOnScaleZero;
        }

        private readonly Dictionary<string, Entry> _active = new Dictionary<string, Entry>(256);
        private readonly Stack<Label> _pool = new Stack<Label>(256);

        private VisualElement _root;
        private VisualElement _overlayContainer;
        private VisualElement _plotClippedContainer;

        private IVisualElementScheduledItem _scaleAnimItem;
        private const float k_ScaleAnimDurationSec = 0.5f;

        private Rect _plotRect;
        private object _styleContext;
        private float _edgeFadePx;

        public void Bind(VisualElement root)
        {
            _root = root;
            EnsureContainers();
            EnsureScaleAnimTicker();
        }

        public void BeginFrame(Rect plotRect, object styleContext, float edgeFadePx = 0f)
        {
            _plotRect = plotRect;
            _styleContext = styleContext;
            _edgeFadePx = Mathf.Max(0f, edgeFadePx);

            EnsureContainers();
            EnsureScaleAnimTicker();

            if (_plotClippedContainer != null)
            {
                // Expand clipping area by edgeFadePx on all sides
                _plotClippedContainer.style.left = _plotRect.x - _edgeFadePx;
                _plotClippedContainer.style.top = _plotRect.y - _edgeFadePx;
                _plotClippedContainer.style.width = _plotRect.width + _edgeFadePx * 2f;
                _plotClippedContainer.style.height = _plotRect.height + _edgeFadePx * 2f;
            }

            foreach (var kv in _active)
            {
                kv.Value.Used = false;
            }
        }

        public void Submit(in LabelDescriptor desc)
        {
            if (_root == null) return;
            if (string.IsNullOrEmpty(desc.key)) return;

            EnsureContainers();

            if (!_active.TryGetValue(desc.key, out var entry))
            {
                entry = new Entry();
                entry.Label = GetPooledLabel();
                _active.Add(desc.key, entry);
            }

            entry.Used = true;
            entry.ZOrder = desc.zOrder;
            entry.ClipToPlot = desc.clipToPlot;

            var label = entry.Label;
            if (label == null) return;

            bool shouldBeVisible = desc.visible && !string.IsNullOrEmpty(desc.text);
            if (!shouldBeVisible)
            {
                RequestHide(entry);
                return;
            }

            EnsureShown(entry);

            if (label.parent != GetParentContainer(desc.clipToPlot))
            {
                label.RemoveFromHierarchy();
                GetParentContainer(desc.clipToPlot)?.Add(label);
            }

            label.text = desc.text;

            label.style.position = Position.Absolute;
            label.style.width = StyleKeyword.Auto;
            label.style.height = StyleKeyword.Auto;

            if (desc.colorOverride.a > 0f)
            {
                label.style.color = desc.colorOverride;
            }

            if (desc.fontSizeOverride > 0f)
            {
                label.style.fontSize = desc.fontSizeOverride;
            }
            else
            {
                label.style.fontSize = StyleKeyword.Null;
            }

            ChartTextStyleApplier.ApplyLabel(label, _styleContext != null ? _styleContext : _root, desc.role);

            // Apply background
            bool hasBackground = desc.backgroundColor.a > 0f || desc.backgroundTexture != null;
            if (hasBackground)
            {
                label.style.paddingLeft = 4f;
                label.style.paddingRight = 4f;
                label.style.paddingTop = 2f;
                label.style.paddingBottom = 2f;

                if (desc.backgroundTexture != null)
                {
                    // When texture is set, use it as background image (no solid color layer)
                    label.style.backgroundColor = StyleKeyword.Null;
                    label.style.backgroundImage = new StyleBackground(desc.backgroundTexture);
                    label.style.unityBackgroundImageTintColor = desc.backgroundColor.a > 0f ? desc.backgroundColor : Color.white;
                }
                else
                {
                    // Only solid color background
                    label.style.backgroundColor = desc.backgroundColor;
                    label.style.backgroundImage = StyleKeyword.Null;
                    label.style.unityBackgroundImageTintColor = StyleKeyword.Null;
                }
            }
            else
            {
                label.style.backgroundColor = StyleKeyword.Null;
                label.style.backgroundImage = StyleKeyword.Null;
                label.style.unityBackgroundImageTintColor = StyleKeyword.Null;
                label.style.paddingLeft = StyleKeyword.Null;
                label.style.paddingRight = StyleKeyword.Null;
                label.style.paddingTop = StyleKeyword.Null;
                label.style.paddingBottom = StyleKeyword.Null;
            }

            var resolvedOffset = new Vector2(desc.offsetPx.x, -desc.offsetPx.y);

            // For clipToPlot labels, offset by edgeFadePx since container is expanded
            float containerOffsetX = desc.clipToPlot ? _edgeFadePx : 0f;
            float containerOffsetY = desc.clipToPlot ? _edgeFadePx : 0f;

            float x = (desc.clipToPlot ? containerOffsetX : _plotRect.x) + desc.anchorPx.x + resolvedOffset.x;
            float y = (desc.clipToPlot ? containerOffsetY : _plotRect.y) + desc.anchorPx.y + resolvedOffset.y;

            ApplyAnchor(label, desc.anchor, x, y);

            // Apply edge fade transparency for clipToPlot labels
            if (desc.clipToPlot && _edgeFadePx > 0f)
            {
                float fadeAlpha = ComputeEdgeFadeAlpha(desc.anchorPx.x, desc.anchorPx.y, _plotRect.width, _plotRect.height, _edgeFadePx);
                label.style.opacity = fadeAlpha;
            }
            else
            {
                label.style.opacity = 1f;
            }
        }

        public void EndFrame()
        {
            foreach (var kv in _active)
            {
                if (kv.Value.Used) continue;
                RequestHide(kv.Value);
            }
        }

        private void EnsureScaleAnimTicker()
        {
            if (_root == null) return;
            if (_scaleAnimItem != null) return;
            _scaleAnimItem = _root.schedule.Execute(TickScaleAnimations).Every(16);
        }

        private void TickScaleAnimations()
        {
            if (_root == null) return;
            float now = Time.realtimeSinceStartup;

            foreach (var kv in _active)
            {
                var e = kv.Value;
                if (e == null) continue;
                if (!e.ScaleAnimating) continue;

                float t = (now - e.ScaleAnimStartTime) / k_ScaleAnimDurationSec;
                if (t >= 1f)
                {
                    e.Scale = e.ScaleTo;
                    ApplyScale(e);
                    e.ScaleAnimating = false;
                    if (e.HideOnScaleZero && e.ScaleTo <= 0f && e.Label != null)
                    {
                        e.Label.visible = false;
                        e.HideOnScaleZero = false;
                    }
                    continue;
                }

                t = Mathf.Clamp01(t);
                e.Scale = Mathf.Lerp(e.ScaleFrom, e.ScaleTo, t);
                ApplyScale(e);
            }
        }

        private static void ApplyScale(Entry entry)
        {
            if (entry == null) return;
            if (entry.Label == null) return;
            float s = Mathf.Clamp01(entry.Scale);
            entry.Label.transform.scale = new Vector3(s, s, 1f);
        }

        private static void EnsureShown(Entry entry)
        {
            if (entry == null) return;
            if (entry.Label == null) return;

            if (!entry.Label.visible)
            {
                entry.Label.visible = true;
            }

            entry.HideOnScaleZero = false;

            if (entry.ScaleAnimating)
            {
                if (entry.ScaleTo >= 1f) return;
            }
            else
            {
                if (entry.Scale >= 1f) return;
            }

            entry.ScaleFrom = entry.Scale;
            if (entry.ScaleFrom <= 0f) entry.ScaleFrom = 0f;
            entry.ScaleTo = 1f;
            entry.ScaleAnimStartTime = Time.realtimeSinceStartup;
            entry.ScaleAnimating = true;
            ApplyScale(entry);
        }

        private void RequestHide(Entry entry)
        {
            if (entry == null) return;
            if (entry.Label == null) return;
            if (!entry.Label.visible && entry.Scale <= 0f) return;

                // Freeze position while hiding: if the label is under the plot-clipped container,
            // its visual position would keep changing when plotRect/container layout changes.
            // Move it to the overlay container and convert coordinates once when hide starts.
            if (entry.Label.parent == _plotClippedContainer && _overlayContainer != null)
            {
                float x = entry.Label.resolvedStyle.left + _plotRect.x;
                float y = entry.Label.resolvedStyle.top + _plotRect.y;
                entry.Label.RemoveFromHierarchy();
                _overlayContainer.Add(entry.Label);
                entry.Label.style.left = x;
                entry.Label.style.top = y;
            }

            // Hide: no scale animation (per request). Reset scale to 0 so next show animates 0->1.
            entry.HideOnScaleZero = false;
            entry.ScaleAnimating = false;
            entry.ScaleFrom = 0f;
            entry.ScaleTo = 0f;
            entry.Scale = 0f;
            ApplyScale(entry);
            entry.Label.visible = false;
        }

        public void RequestScaleOut(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (!_active.TryGetValue(key, out var entry)) return;
            if (entry == null || entry.Label == null) return;
            if (!entry.Label.visible) return;

            // Already animating to 0
            if (entry.ScaleAnimating && entry.ScaleTo <= 0f) return;

            entry.ScaleFrom = entry.Scale;
            entry.ScaleTo = 0f;
            entry.ScaleAnimStartTime = Time.realtimeSinceStartup;
            entry.ScaleAnimating = true;
            entry.HideOnScaleZero = true;
            ApplyScale(entry);
        }

        private void EnsureContainers()
        {
            if (_root == null) return;

            if (_overlayContainer == null)
            {
                _overlayContainer = new VisualElement();
                _overlayContainer.name = "label-overlay-container";
                _overlayContainer.pickingMode = PickingMode.Ignore;
                _overlayContainer.style.position = Position.Absolute;
                _overlayContainer.style.left = 0;
                _overlayContainer.style.top = 0;
                _overlayContainer.style.right = 0;
                _overlayContainer.style.bottom = 0;
                _overlayContainer.style.overflow = Overflow.Visible;
                _root.Add(_overlayContainer);
            }

            if (_plotClippedContainer == null)
            {
                _plotClippedContainer = new VisualElement();
                _plotClippedContainer.name = "label-plot-clipped-container";
                _plotClippedContainer.pickingMode = PickingMode.Ignore;
                _plotClippedContainer.style.position = Position.Absolute;
                _plotClippedContainer.style.overflow = Overflow.Hidden;
                _root.Add(_plotClippedContainer);
            }
        }

        private VisualElement GetParentContainer(bool clipToPlot)
        {
            if (clipToPlot) return _plotClippedContainer;
            return _overlayContainer;
        }

        private Label GetPooledLabel()
        {
            Label label = _pool.Count > 0 ? _pool.Pop() : new Label();
            label.pickingMode = PickingMode.Ignore;
            return label;
        }

        private static float ComputeEdgeFadeAlpha(float anchorX, float anchorY, float plotWidth, float plotHeight, float fadePx)
        {
            if (fadePx <= 0f) return 1f;

            // Calculate distance from each edge (negative = inside, positive = outside)
            float distLeft = -anchorX;
            float distRight = anchorX - plotWidth;
            float distTop = -anchorY;
            float distBottom = anchorY - plotHeight;

            // Find the maximum overflow distance
            float maxOverflow = Mathf.Max(distLeft, distRight, distTop, distBottom);

            if (maxOverflow <= 0f) return 1f; // Inside plot area
            if (maxOverflow >= fadePx) return 0f; // Beyond fade zone

            // Linear fade from 1 to 0 as we go from edge to fadePx
            return 1f - (maxOverflow / fadePx);
        }

        private static void ApplyAnchor(Label label, ChartLabelAnchor anchor, float x, float y)
        {
            if (label == null) return;

            label.style.left = x;
            label.style.top = y;

            switch (anchor)
            {
                case ChartLabelAnchor.TopLeft:
                    label.style.translate = new StyleTranslate(new Translate(0, 0, 0));
                    label.style.unityTextAlign = TextAnchor.UpperLeft;
                    break;
                case ChartLabelAnchor.Top:
                    label.style.translate = new StyleTranslate(new Translate(new Length(-50, LengthUnit.Percent), 0, 0));
                    label.style.unityTextAlign = TextAnchor.UpperCenter;
                    break;
                case ChartLabelAnchor.TopRight:
                    label.style.translate = new StyleTranslate(new Translate(new Length(-100, LengthUnit.Percent), 0, 0));
                    label.style.unityTextAlign = TextAnchor.UpperRight;
                    break;
                case ChartLabelAnchor.Left:
                    label.style.translate = new StyleTranslate(new Translate(0, new Length(-50, LengthUnit.Percent), 0));
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    break;
                case ChartLabelAnchor.Right:
                    label.style.translate = new StyleTranslate(new Translate(new Length(-100, LengthUnit.Percent), new Length(-50, LengthUnit.Percent), 0));
                    label.style.unityTextAlign = TextAnchor.MiddleRight;
                    break;
                case ChartLabelAnchor.BottomLeft:
                    label.style.translate = new StyleTranslate(new Translate(0, new Length(-100, LengthUnit.Percent), 0));
                    label.style.unityTextAlign = TextAnchor.LowerLeft;
                    break;
                case ChartLabelAnchor.Bottom:
                    label.style.translate = new StyleTranslate(new Translate(new Length(-50, LengthUnit.Percent), new Length(-100, LengthUnit.Percent), 0));
                    label.style.unityTextAlign = TextAnchor.LowerCenter;
                    break;
                case ChartLabelAnchor.BottomRight:
                    label.style.translate = new StyleTranslate(new Translate(new Length(-100, LengthUnit.Percent), new Length(-100, LengthUnit.Percent), 0));
                    label.style.unityTextAlign = TextAnchor.LowerRight;
                    break;
                default:
                    label.style.translate = new StyleTranslate(new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent), 0));
                    label.style.unityTextAlign = TextAnchor.MiddleCenter;
                    break;
            }
        }
    }
}
