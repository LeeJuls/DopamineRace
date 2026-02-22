using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyChart
{
    internal sealed class ChartAnimationController
    {
        public bool IsAnimating { get; private set; }
        public float Time { get; private set; }
        public float Progress { get; private set; } = 1f;
        public bool HasTextureFillAnimations { get; private set; }

        public void SetHasTextureFillAnimations(bool has)
        {
            HasTextureFillAnimations = has;
        }

#if UNITY_EDITOR
        private bool _editorAnimHooked;
        private double _editorAnimLastTime;
        private IVisualElementScheduledItem _editorAnimScheduledItem;

        private bool _editorTexAnimHooked;
        private IVisualElementScheduledItem _editorTexAnimScheduledItem;
#endif

        public void Play(VisualElement owner, bool isPlaying, IList<BaseSeriesRenderer> renderers, System.Action editorTick)
        {
            Progress = 0f;
            Time = 0f;
            IsAnimating = true;

#if UNITY_EDITOR
            if (!isPlaying)
            {
                if (!_editorAnimHooked)
                {
                    _editorAnimHooked = true;
                    _editorAnimLastTime = EditorApplication.timeSinceStartup;
                    _editorAnimScheduledItem = owner.schedule.Execute(editorTick).Every(16);
                }
            }
#endif

            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                r.AnimationProgress = 0f;
            }

            if (isPlaying) owner.MarkDirtyRepaint();
        }

#if UNITY_EDITOR
        public void StopEditorAnimation()
        {
            if (_editorAnimScheduledItem != null)
            {
                _editorAnimScheduledItem.Pause();
                _editorAnimScheduledItem = null;
            }
            _editorAnimHooked = false;
        }

        public bool TickEditorAnimation(VisualElement owner, ChartData data, IList<BaseSeriesRenderer> renderers)
        {
            if (!_editorAnimHooked) return false;

            if (!IsAnimating)
            {
                StopEditorAnimation();
                return false;
            }

            if (owner.panel == null || data == null)
            {
                IsAnimating = false;
                StopEditorAnimation();
                return false;
            }

            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _editorAnimLastTime);
            _editorAnimLastTime = now;

            float duration = data.animationDuration;
            if (duration <= 0) duration = 0.5f;

            Time += dt;
            Progress = Mathf.Clamp01(Time / duration);

            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (r.panel == null) continue;
                if (r.parent == null) continue;
                r.AnimationProgress = Progress;
            }

            if (Progress >= 1.0f)
            {
                IsAnimating = false;
                StopEditorAnimation();
                return true;
            }

            return false;
        }

        private void StopEditorTextureFillAnimation()
        {
            if (_editorTexAnimScheduledItem != null)
            {
                _editorTexAnimScheduledItem.Pause();
                _editorTexAnimScheduledItem = null;
            }
            _editorTexAnimHooked = false;
        }

        private void StartEditorTextureFillAnimation(VisualElement owner, System.Action editorTick)
        {
            if (Application.isPlaying) return;
            if (!HasTextureFillAnimations) return;
            if (owner.panel == null) return;
            if (_editorTexAnimHooked) return;

            _editorTexAnimHooked = true;
            _editorTexAnimScheduledItem = owner.schedule.Execute(editorTick).Every(16);
        }

        public void SetHasTextureFillAnimations(bool has, VisualElement owner, System.Action editorTick)
        {
            HasTextureFillAnimations = has;

            if (Application.isPlaying) return;

            if (HasTextureFillAnimations) StartEditorTextureFillAnimation(owner, editorTick);
            else StopEditorTextureFillAnimation();
        }

        public void OnAttachToPanel(VisualElement owner, System.Action editorTick)
        {
            if (Application.isPlaying) return;
            if (HasTextureFillAnimations) StartEditorTextureFillAnimation(owner, editorTick);
        }

        public void OnDetachFromPanel()
        {
            StopEditorTextureFillAnimation();
        }

        public void TickEditorTextureFillAnimation(VisualElement owner, IList<BaseSeriesRenderer> renderers, VisualElement backgroundLayer)
        {
            if (!_editorTexAnimHooked) return;
            if (owner.panel == null)
            {
                StopEditorTextureFillAnimation();
                return;
            }

            if (!HasTextureFillAnimations)
            {
                StopEditorTextureFillAnimation();
                return;
            }

            if (backgroundLayer != null) backgroundLayer.MarkDirtyRepaint();
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (r.panel == null) continue;
                if (r.parent == null) continue;
                r.MarkDirtyRepaint();
            }
        }
#endif

        public void TickRuntime(ChartData data, IList<BaseSeriesRenderer> renderers, VisualElement backgroundLayer)
        {
            if (!IsAnimating && !HasTextureFillAnimations) return;

            if (data == null) return;

            if (!IsAnimating)
            {
                if (backgroundLayer != null) backgroundLayer.MarkDirtyRepaint();
                for (int i = 0; i < renderers.Count; i++)
                {
                    var r = renderers[i];
                    if (r == null) continue;
                    r.MarkDirtyRepaint();
                }
                return;
            }

            float duration = data.animationDuration;
            if (duration <= 0) duration = 0.5f;

            Time += 0.016f;
            Progress = Mathf.Clamp01(Time / duration);

            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (r.panel == null) continue;
                if (r.parent == null) continue;
                r.AnimationProgress = Progress;
            }

            if (Progress >= 1.0f)
            {
                IsAnimating = false;
            }
        }

        public void ResetRuntimeState()
        {
            IsAnimating = false;
            Time = 0f;
            Progress = 1f;
        }
    }
}
