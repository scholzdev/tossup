using System.Collections.Generic;
using UnityEngine;

namespace Tossup.UI
{
    // Draws the canvas with Unity's immediate-mode GL, batching consecutive primitives that share a texture.
    // Call between BeginFrame and EndFrame, from a camera's OnPostRender.
    public sealed class UnityGfxBackend : IGfxBackend
    {
        readonly Material material;
        readonly Texture2D white;
        static readonly int AlphaOnlyId = Shader.PropertyToID("_AlphaOnly");

        Texture current;
        bool currentAlphaOnly, begun;
        float scale = 1, ox, oy;

        public UnityGfxBackend()
        {
            var shader = Resources.Load<Shader>("Shaders/Tossup2D");
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            white = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            white.SetPixel(0, 0, Color.white);
            white.Apply();
        }

        public void BeginFrame()
        {
            Ui.Layout(out scale, out ox, out oy);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0); // y down, like LÖVE
            current = null;
            begun = false;
        }

        public void EndFrame()
        {
            if (begun) GL.End();
            begun = false;
            GL.PopMatrix();
        }

        void Use(Texture texture, bool alphaOnly)
        {
            if (begun && texture == current && alphaOnly == currentAlphaOnly) return;
            if (begun) GL.End();
            material.mainTexture = texture;
            material.SetFloat(AlphaOnlyId, alphaOnly ? 1 : 0);
            material.SetPass(0);
            GL.Begin(GL.TRIANGLES);
            current = texture;
            currentAlphaOnly = alphaOnly;
            begun = true;
        }

        static Color ToColor(Rgba c) => new Color(c.R, c.G, c.B, c.A);

        void Vertex(float x, float y, float u, float v)
        {
            GL.TexCoord2(u, v);
            GL.Vertex3(ox + x * scale, oy + y * scale, 0);
        }

        public void Triangles(List<float> xy, Rgba color)
        {
            if (xy.Count < 6) return;
            Use(white, false);
            GL.Color(ToColor(color));
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

        public void Image(Img image, float x0, float y0, float x1, float y1, Rgba tint)
        {
            if (!(image?.Native is Texture texture)) return;
            Use(texture, false);
            GL.Color(ToColor(tint));
            Quad(x0, y0, x1, y1, new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0));
        }

        public void Text(PixFont font, string text, float x, float y, float sx, float sy, Rgba color)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (font.Glyphs != null && font.Atlas?.Native is Texture atlas)
            {
                // baked glyphs: the bitmaps LÖVE itself rasterized, placed exactly as LÖVE places them
                Use(atlas, false);
                GL.Color(ToColor(color));
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
            GL.Color(ToColor(color));
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
