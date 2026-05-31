using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyChart
{
    /// <summary>
    /// Bridge for Pro TextureFX rendering. Pro bootstrap registers implementations at startup.
    /// Lite builds compile fine and all Draw calls are no-ops when Pro is not installed.
    /// </summary>
    public static class TextureFXBridge
    {
        // ── Registration delegates (set by Pro bootstrap) ──────────────────────

        public static System.Action<MeshGenerationContext, Rect, List<TextureFXLayer>, float>
            DrawLayersImpl;

        public static System.Action<MeshGenerationContext, Rect, List<TextureFXLayer>, float,
            bool, float, int, bool, bool>
            DrawBarLayersImpl;

        public static System.Action<MeshGenerationContext, List<Vector2>, float,
            List<TextureFXLayer>, float>
            DrawLineLayers1Impl;   // overload with float bottomY

        public static System.Action<MeshGenerationContext, List<Vector2>, List<float>,
            List<TextureFXLayer>, float>
            DrawLineLayers2Impl;   // overload with List<float> bottomYs

        public static System.Action<MeshGenerationContext, List<Vector2>, float,
            List<TextureFXLayer>, float>
            DrawAreaClippedLayers1Impl;

        public static System.Action<MeshGenerationContext, List<Vector2>, List<float>,
            List<TextureFXLayer>, float>
            DrawAreaClippedLayers2Impl;

        public static System.Action<MeshGenerationContext, List<Vector2>, Rect,
            List<TextureFXLayer>, float>
            DrawPolygonClippedLayersImpl;

        public static System.Action<MeshGenerationContext, List<Vector2>, float,
            List<TextureFXLayer>, float>
            DrawLineLayersImpl;

        public static System.Func<List<TextureFXLayer>, bool>
            HasAnyAnimationImpl;

        // ── Public API (called by renderers) ───────────────────────────────────

        public static void DrawLayers(
            MeshGenerationContext mgc, Rect rect,
            List<TextureFXLayer> layers, float time)
            => DrawLayersImpl?.Invoke(mgc, rect, layers, time);

        public static void DrawBarLayers(
            MeshGenerationContext mgc, Rect barRect,
            List<TextureFXLayer> layers, float time,
            bool isHorizontal = false,
            float cornerRadius = 0f, int cornerSegments = 4,
            bool roundTop = false, bool roundBottom = false)
            => DrawBarLayersImpl?.Invoke(mgc, barRect, layers, time,
                isHorizontal, cornerRadius, cornerSegments, roundTop, roundBottom);

        public static void DrawAreaClippedLayers(
            MeshGenerationContext mgc, List<Vector2> topVertices,
            float bottomY, List<TextureFXLayer> layers, float time)
            => DrawAreaClippedLayers1Impl?.Invoke(mgc, topVertices, bottomY, layers, time);

        public static void DrawAreaClippedLayers(
            MeshGenerationContext mgc, List<Vector2> topVertices,
            List<float> bottomYs, List<TextureFXLayer> layers, float time)
            => DrawAreaClippedLayers2Impl?.Invoke(mgc, topVertices, bottomYs, layers, time);

        public static void DrawPolygonClippedLayers(
            MeshGenerationContext mgc, List<Vector2> polygonVertices,
            Rect bounds, List<TextureFXLayer> layers, float time)
            => DrawPolygonClippedLayersImpl?.Invoke(mgc, polygonVertices, bounds, layers, time);

        public static void DrawLineLayers(
            MeshGenerationContext mgc, List<Vector2> points,
            float lineWidth, List<TextureFXLayer> layers, float time)
            => DrawLineLayersImpl?.Invoke(mgc, points, lineWidth, layers, time);

        public static bool HasAnyAnimation(List<TextureFXLayer> layers)
            => HasAnyAnimationImpl != null && HasAnyAnimationImpl(layers);

        public static float GetAnimationTime()
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
                return (float)UnityEditor.EditorApplication.timeSinceStartup;
#endif
            return UnityEngine.Time.time;
        }

        public static UnityEngine.Rect ApplyPadding(UnityEngine.Rect rect, UnityEngine.Vector4 padding)
        {
            return new UnityEngine.Rect(
                rect.x + padding.x,
                rect.y + padding.y,
                rect.width - padding.x - padding.z,
                rect.height - padding.y - padding.w);
        }

        public static void DrawSolidRect(MeshGenerationContext mgc, UnityEngine.Rect rect, UnityEngine.Color color)
        {
            if (mgc == null || color.a <= 0.001f || rect.width <= 0 || rect.height <= 0) return;
            var mesh = mgc.Allocate(4, 6);
            mesh.SetAllVertices(new[]
            {
                new Vertex { position = new UnityEngine.Vector3(rect.xMin, rect.yMin, Vertex.nearZ), tint = color },
                new Vertex { position = new UnityEngine.Vector3(rect.xMax, rect.yMin, Vertex.nearZ), tint = color },
                new Vertex { position = new UnityEngine.Vector3(rect.xMax, rect.yMax, Vertex.nearZ), tint = color },
                new Vertex { position = new UnityEngine.Vector3(rect.xMin, rect.yMax, Vertex.nearZ), tint = color }
            });
            mesh.SetAllIndices(new ushort[] { 0, 1, 2, 2, 3, 0 });
        }
    }
}
