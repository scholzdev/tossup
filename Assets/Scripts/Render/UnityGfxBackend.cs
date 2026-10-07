using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tossup.UI
{
    // Collects the immediate canvas API into reusable mesh batches for the URP canvas pass.
    // Only consecutive primitives with the same texture, font mode and clip may share a batch.
    public sealed class UnityGfxBackend : IGfxBackend, IDisposable
    {
        readonly Material material;
        readonly Texture2D white;
        static readonly int AlphaOnlyId = Shader.PropertyToID("_AlphaOnly");

        static readonly int ClipId = Shader.PropertyToID("_ClipRect");
        static readonly int ProjectionId = Shader.PropertyToID("_CanvasProjection");
        static readonly Vector4 NoClip = new Vector4(-1e6f, -1e6f, 1e6f, 1e6f);
        readonly Mesh mesh;
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> indices = new List<int>();
        readonly List<Batch> batches = new List<Batch>();
        int batchCount;
        Vector4 clip;
        Color vertexColor;
        float scale = 1, ox, oy;

        sealed class Batch
        {
            public Texture Texture;
            public bool AlphaOnly;
            public Vector4 Clip;
            public int Start, Count;
            public readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
        }

        public UnityGfxBackend()
        {
            var shader = Resources.Load<Shader>("Shaders/Tossup2D");
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("Tossup's URP canvas shader is missing or unsupported.");
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            white = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            white.SetPixel(0, 0, Color.white);
            white.Apply();
            mesh = new Mesh { name = "Tossup canvas", indexFormat = IndexFormat.UInt32,
                hideFlags = HideFlags.HideAndDontSave };
            mesh.MarkDynamic();
        }

        public void BeginFrame()
        {
            Ui.Layout(out scale, out ox, out oy);
            vertices.Clear(); uvs.Clear(); colors.Clear(); indices.Clear();
            batchCount = 0;
            clip = NoClip;
        }

        public void EndFrame()
        {
            if (batchCount > 0 && batches[batchCount - 1].Count == 0) batchCount--;
            mesh.Clear();
            if (indices.Count == 0) return;
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
            mesh.subMeshCount = batchCount;
            for (int i = 0; i < batchCount; i++)
            {
                var b = batches[i];
                mesh.SetSubMesh(i, new SubMeshDescriptor(b.Start, b.Count, MeshTopology.Triangles),
                    MeshUpdateFlags.DontRecalculateBounds);
                b.Properties.SetTexture("_MainTex", b.Texture);
                b.Properties.SetFloat(AlphaOnlyId, b.AlphaOnly ? 1 : 0);
                b.Properties.SetVector(ClipId, b.Clip);
            }
            mesh.bounds = new Bounds(new Vector3(Screen.width / 2f, Screen.height / 2f, 0),
                new Vector3(Screen.width, Screen.height, 1));
        }

        internal void Render(RasterCommandBuffer cmd, Matrix4x4 projection)
        {
            for (int i = 0; i < batchCount; i++)
            {
                var b = batches[i];
                b.Properties.SetMatrix(ProjectionId, projection);
                cmd.DrawMesh(mesh, Matrix4x4.identity, material, i, 0, b.Properties);
            }
        }

        void Use(Texture texture, bool alphaOnly)
        {
            if (batchCount > 0)
            {
                var current = batches[batchCount - 1];
                if (current.Texture == texture && current.AlphaOnly == alphaOnly && current.Clip.Equals(clip)) return;
                // A glyph string may contain only spaces. Do not keep an empty submesh.
                if (current.Count == 0) batchCount--;
            }
            if (batchCount == batches.Count) batches.Add(new Batch());
            var b = batches[batchCount++];
            b.Texture = texture; b.AlphaOnly = alphaOnly; b.Clip = clip;
            b.Start = indices.Count; b.Count = 0;
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(mesh);
            UnityEngine.Object.Destroy(material);
            UnityEngine.Object.Destroy(white);
        }

        static Color ToColor(Rgba c) => new Color(c.R, c.G, c.B, c.A);

        void Vertex(float x, float y, float u, float v)
        {
            indices.Add(vertices.Count);
            vertices.Add(new Vector3(ox + x * scale, oy + y * scale, 0));
            uvs.Add(new Vector2(u, v));
            colors.Add(vertexColor);
            batches[batchCount - 1].Count++;
        }

        public void Triangles(List<float> xy, Rgba color)
        {
            if (xy.Count < 6) return;
            Use(white, false);
            vertexColor = ToColor(color);
            for (int k = 0; k + 1 < xy.Count; k += 2) Vertex(xy[k], xy[k + 1], .5f, .5f);
        }

        void Quad(float x0, float y0, float x1, float y1, Vector2 uvTopLeft, Vector2 uvTopRight, Vector2 uvBottomRight, Vector2 uvBottomLeft)
        {
            Vertex(x0, y0, uvTopLeft.x, uvTopLeft.y);
            Vertex(x1, y0, uvTopRight.x, uvTopRight.y);
            Vertex(x1, y1, uvBottomRight.x, uvBottomRight.y);
            Vertex(x0, y0, uvTopLeft.x, uvTopLeft.y);
            Vertex(x1, y1, uvBottomRight.x, uvBottomRight.y);
            Vertex(x0, y1, uvBottomLeft.x, uvBottomLeft.y);
        }

        public void Image(Img image, float[] quad, Rgba tint)
        {
            if (!(image?.Native is Texture texture)) return;
            Use(texture, false);
            vertexColor = ToColor(tint);
            Vertex(quad[0], quad[1], 0, 1); Vertex(quad[2], quad[3], 1, 1); Vertex(quad[4], quad[5], 1, 0);
            Vertex(quad[0], quad[1], 0, 1); Vertex(quad[4], quad[5], 1, 0); Vertex(quad[6], quad[7], 0, 0);
        }

        public void Clip(float x, float y, float w, float h)
        {
            clip = w < 0 || h < 0 ? NoClip :
                new Vector4(ox + x * scale, oy + y * scale, ox + (x + w) * scale, oy + (y + h) * scale);
        }

        public void Text(PixFont font, string text, float x, float y, float sx, float sy, Rgba color)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (font.Glyphs != null && font.Atlas?.Native is Texture atlas)
            {
                // baked glyphs: the bitmaps LÖVE itself rasterized, placed exactly as LÖVE places them
                Use(atlas, true);
                vertexColor = ToColor(color);
                float w = font.Atlas.Width, h = font.Atlas.Height;
                float penX = x, top = y + font.Ascent * sy;
                foreach (char c in text)
                {
                    if (!font.Glyphs.TryGetValue(c, out var g)) continue;
                    if (g.W > 0 && g.H > 0)
                    {
                        float u0 = g.X / w, u1 = (g.X + g.W) / w, v0 = 1 - g.Y / h, v1 = 1 - (g.Y + g.H) / h;
                        float gx = penX + g.BearingX * sx, gy = top - g.BearingY * sy;
                        Quad(gx, gy, gx + g.W * sx, gy + g.H * sy, new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1));
                    }
                    penX += g.Advance * sx;
                }
                return;
            }
            if (!(font.Native is Font unityFont)) return;
            unityFont.RequestCharactersInTexture(text, font.Size);
            var texture = unityFont.material.mainTexture;
            texture.filterMode = FilterMode.Point; // pixel font, like LÖVE's "nearest" filter
            Use(texture, true);
            vertexColor = ToColor(color);
            float pen = x, baseline = y + font.Ascent * sy;
            foreach (char c in text)
            {
                if (!unityFont.GetCharacterInfo(c, out CharacterInfo ci, font.Size)) continue;
                if (ci.maxX > ci.minX && ci.maxY > ci.minY)
                    Quad(pen + ci.minX * sx, baseline - ci.maxY * sy, pen + ci.maxX * sx, baseline - ci.minY * sy,
                        ci.uvTopLeft, ci.uvTopRight, ci.uvBottomRight, ci.uvBottomLeft);
                pen += ci.advance * sx;
            }
        }
    }
}
