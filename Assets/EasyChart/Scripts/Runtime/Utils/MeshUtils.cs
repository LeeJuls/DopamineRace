using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart
{
    public static class MeshUtils
    {
        public static void WriteTexturedQuad(MeshGenerationContext context,
            float xMin,
            float yMin,
            float xMax,
            float yMax,
            Texture texture,
            Color tint,
            float u0,
            float v0,
            float u1,
            float v1)
        {
            if (texture == null) return;

            var mesh = context.Allocate(4, 6, texture);

            mesh.SetNextVertex(new Vertex { position = new Vector3(xMin, yMin, Vertex.nearZ), tint = tint, uv = new Vector2(u0, v0) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(xMax, yMin, Vertex.nearZ), tint = tint, uv = new Vector2(u1, v0) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(xMax, yMax, Vertex.nearZ), tint = tint, uv = new Vector2(u1, v1) });
            mesh.SetNextVertex(new Vertex { position = new Vector3(xMin, yMax, Vertex.nearZ), tint = tint, uv = new Vector2(u0, v1) });

            mesh.SetNextIndex(0);
            mesh.SetNextIndex(1);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(3);
            mesh.SetNextIndex(0);
        }

        public static void WriteTexturedFan(MeshGenerationContext context,
            IList<Vector2> points,
            Texture texture,
            Color tint,
            Rect rect,
            float u0,
            float v0,
            float u1,
            float v1)
        {
            if (texture == null) return;
            if (points == null || points.Count < 3) return;

            int vCount = points.Count;
            int iCount = (vCount - 2) * 3;
            var mesh = context.Allocate(vCount, iCount, texture);

            for (int vi = 0; vi < vCount; vi++)
            {
                var p = points[vi];
                float tx = rect.width > 0f ? (p.x - rect.xMin) / rect.width : 0f;
                float ty = rect.height > 0f ? (p.y - rect.yMin) / rect.height : 0f;
                float uu = Mathf.Lerp(u0, u1, tx);
                float vv = Mathf.Lerp(v0, v1, ty);
                mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, Vertex.nearZ), tint = tint, uv = new Vector2(uu, vv) });
            }

            for (int tri = 1; tri < vCount - 1; tri++)
            {
                mesh.SetNextIndex((ushort)0);
                mesh.SetNextIndex((ushort)tri);
                mesh.SetNextIndex((ushort)(tri + 1));
            }
        }

        public static void WriteTexturedVerticalStrip(MeshGenerationContext context,
            IList<Vector2> topVertices,
            float bottomY,
            Texture texture,
            Color tint,
            Vector2 tiling,
            Vector2 offset,
            bool doubleSided = true)
        {
            if (texture == null) return;
            if (topVertices == null || topVertices.Count < 2) return;

            int segmentCount = topVertices.Count - 1;
            int vertexCount = topVertices.Count * 2;
            int indexCount = segmentCount * 6 * (doubleSided ? 2 : 1);

            var mesh = context.Allocate(vertexCount, indexCount, texture);

            float startX = topVertices[0].x;
            float endX = topVertices[topVertices.Count - 1].x;
            float rangeX = endX - startX;
            if (rangeX <= 0f) rangeX = 1f;

            for (int i = 0; i < topVertices.Count; i++)
            {
                Vector2 topPos = topVertices[i];
                Vector2 bottomPos = new Vector2(topPos.x, bottomY);

                float u = (topPos.x - startX) / rangeX;
                float uu = u * tiling.x + offset.x;

                mesh.SetNextVertex(new Vertex
                {
                    position = new Vector3(topPos.x, topPos.y, Vertex.nearZ),
                    tint = tint,
                    uv = new Vector2(uu, tiling.y + offset.y)
                });
                mesh.SetNextVertex(new Vertex
                {
                    position = new Vector3(bottomPos.x, bottomPos.y, Vertex.nearZ),
                    tint = tint,
                    uv = new Vector2(uu, offset.y)
                });
            }

            for (int i = 0; i < segmentCount; i++)
            {
                ushort top1 = (ushort)(i * 2);
                ushort bot1 = (ushort)(i * 2 + 1);
                ushort top2 = (ushort)((i + 1) * 2);
                ushort bot2 = (ushort)((i + 1) * 2 + 1);

                // front
                mesh.SetNextIndex(top1); mesh.SetNextIndex(bot1); mesh.SetNextIndex(top2);
                mesh.SetNextIndex(top2); mesh.SetNextIndex(bot1); mesh.SetNextIndex(bot2);

                if (doubleSided)
                {
                    // back
                    mesh.SetNextIndex(top1); mesh.SetNextIndex(top2); mesh.SetNextIndex(bot1);
                    mesh.SetNextIndex(top2); mesh.SetNextIndex(bot2); mesh.SetNextIndex(bot1);
                }
            }
        }
    }
}
