using System;
using System.Collections.Generic;

namespace Tossup.UI
{
    public struct Rgba
    {
        public float R, G, B, A;

        public Rgba(float r, float g, float b, float a = 1)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Rgba WithAlpha(float a) => new Rgba(R, G, B, a);

        public static Rgba Hex(string value) => new Rgba(
            Convert.ToInt32(value.Substring(0, 2), 16) / 255f,
            Convert.ToInt32(value.Substring(2, 2), 16) / 255f,
            Convert.ToInt32(value.Substring(4, 2), 16) / 255f);
    }

    // A loaded image: its pixel size plus the backend's texture.
    public sealed class Img
    {
        public string Key;
        public int Width, Height;
        public object Native;
    }

    public enum Align { Left, Center, Right }

    // One glyph of a baked font: its rectangle in the atlas, FreeType bearing and advance (pixels).
    public sealed class BakedGlyph
    {
        public int X, Y, W, H, BearingX, BearingY, Advance;
    }

    // A font at one pixel size, with LÖVE's metrics: Height is the line height, Ascent the baseline.
    // Either baked (Atlas + Glyphs, rasterized by LÖVE itself: Tools/fontbake) or drawn by the backend's own
    // font engine (Native).
    public sealed class PixFont
    {
        public readonly int Size, Ascent, Height;
        public object Native;
        public Img Atlas;
        public Dictionary<char, BakedGlyph> Glyphs;
        readonly Func<PixFont, char, int> advance;
        readonly Dictionary<char, int> advances = new Dictionary<char, int>();

        public PixFont(int size, int ascent, int height, Func<PixFont, char, int> advance, object native = null)
        {
            Size = size;
            Ascent = ascent;
            Height = height;
            this.advance = advance;
            Native = native;
        }

        // A font from Resources/fonts/baked/metrics.json; null if that size was not baked.
        public static PixFont Baked(string metricsJson, int size, Img atlas)
        {
            if (string.IsNullOrEmpty(metricsJson) || atlas == null) return null;
            var t = JsonData.Parse(metricsJson)[size.ToString()];
            var list = t?["glyphs"];
            if (t == null || list == null || list.Kind != JsonKind.Array) return null;
            var glyphs = new Dictionary<char, BakedGlyph>();
            foreach (var g in list.Array)
            {
                if (g.Kind != JsonKind.Array || g.Array.Count < 8) continue;
                int V(int k) => (int)g.Array[k].Number;
                int cp = V(0);
                if (cp > char.MaxValue) continue;
                glyphs[(char)cp] = new BakedGlyph { X = V(1), Y = V(2), W = V(3), H = V(4), BearingX = V(5), BearingY = V(6), Advance = V(7) };
            }
            var font = new PixFont(size, (int)t["ascent"].Number, (int)t["height"].Number,
                (f, c) => f.Glyphs.TryGetValue(c, out var glyph) ? glyph.Advance : 0) { Atlas = atlas, Glyphs = glyphs };
            return font;
        }

        public int Advance(char c)
        {
            if (!advances.TryGetValue(c, out int a)) advances[c] = a = advance(this, c);
            return a;
        }

        public int GetWidth(string text)
        {
            int width = 0, best = 0;
            foreach (char c in text)
            {
                if (c == '\n') { best = Math.Max(best, width); width = 0; continue; }
                width += Advance(c);
            }
            return Math.Max(best, width);
        }

        // LÖVE 11's Font:getWrap: greedy wrapping at spaces; a word longer than the limit is broken; the
        // width of a line ignores its trailing spaces.
        public List<string> GetWrap(string text, float limit, List<float> widths = null)
        {
            var lines = new List<string>();
            foreach (string paragraph in text.Split('\n'))
            {
                var line = new System.Text.StringBuilder();
                float width = 0, widthBeforeLastSpace = 0;
                int lastSpace = -1;
                char prev = '\0';
                int i = 0;
                while (i < paragraph.Length)
                {
                    char c = paragraph[i];
                    float charWidth = Advance(c);
                    float newWidth = width + charWidth;
                    if (c != ' ' && newWidth > limit)
                    {
                        if (line.Length == 0) i++; // a single character wider than the limit is skipped
                        else if (lastSpace != -1)
                        {
                            int keep = line.Length;
                            while (keep > 0 && line[keep - 1] != ' ') keep--;
                            line.Length = keep;
                            width = widthBeforeLastSpace;
                            i = lastSpace + 1;
                        }
                        lines.Add(line.ToString());
                        widths?.Add(width);
                        line.Clear();
                        width = widthBeforeLastSpace = 0;
                        lastSpace = -1;
                        prev = '\0';
                        continue;
                    }
                    if (prev != ' ' && c == ' ') widthBeforeLastSpace = width;
                    width = newWidth;
                    prev = c;
                    line.Append(c);
                    if (c == ' ') lastSpace = i;
                    i++;
                }
                lines.Add(line.ToString());
                // trailing spaces do not count towards the width of the last line either
                widths?.Add(prev == ' ' ? widthBeforeLastSpace : width);
            }
            return lines;
        }
    }

    // What actually puts pixels on screen. Coordinates are on the 1280x800 canvas; the backend maps the
    // canvas onto the window.
    public interface IGfxBackend
    {
        void Triangles(List<float> xy, Rgba color); // triangle list: x0,y0,x1,y1,x2,y2,...
        void Image(Img image, float x0, float y0, float x1, float y1, Rgba tint);
        void Text(PixFont font, string text, float x, float y, float sx, float sy, Rgba color); // x,y: top left of the line
    }

    // An immediate-mode drawing API shaped like love.graphics, so the original views port line by line.
    // Transforms are translate + scale only (all the game uses).
    public static class Gfx
    {
        public static IGfxBackend Backend;

        static Rgba color = new Rgba(1, 1, 1);
        static float lineWidth = 1;
        static PixFont font;
        static float sx = 1, sy = 1, tx, ty;
        static readonly Stack<float[]> stack = new Stack<float[]>();
        static readonly List<float> buffer = new List<float>(256);
        static readonly List<float> path = new List<float>(128);

        public static void Reset()
        {
            color = new Rgba(1, 1, 1);
            lineWidth = 1;
            sx = sy = 1;
            tx = ty = 0;
            stack.Clear();
        }

        public static void SetColor(Rgba c) => color = c;
        public static void SetColor(float r, float g, float b, float a = 1) => color = new Rgba(r, g, b, a);
        public static void SetLineWidth(float width) => lineWidth = width;
        public static void SetFont(PixFont f) => font = f;
        public static PixFont Font => font;

        public static void Push() => stack.Push(new[] { sx, sy, tx, ty });

        public static void Pop()
        {
            var s = stack.Pop();
            sx = s[0];
            sy = s[1];
            tx = s[2];
            ty = s[3];
        }

        public static void Translate(float x, float y)
        {
            tx += x * sx;
            ty += y * sy;
        }

        public static void Scale(float x, float y)
        {
            sx *= x;
            sy *= y;
        }

        static float X(float x) => x * sx + tx;
        static float Y(float y) => y * sy + ty;

        static int Segments(float radius) => Math.Max(8, Math.Min(64, (int)Math.Ceiling(radius * Math.Max(Math.Abs(sx), Math.Abs(sy)) * 0.75f)));

        static int CornerSegments(float radius) => Math.Max(2, Segments(radius) / 4);

        // Outline of a rounded rectangle into `path`: the rectangle grown by `grow` on every side, with corner
        // radius r and `segments` steps per corner (0 segments: square corners).
        static void RoundedRectPath(float x, float y, float w, float h, float r, float grow, int segments)
        {
            path.Clear();
            x -= grow;
            y -= grow;
            w += grow * 2;
            h += grow * 2;
            if (segments == 0)
            {
                path.Add(x); path.Add(y);
                path.Add(x + w); path.Add(y);
                path.Add(x + w); path.Add(y + h);
                path.Add(x); path.Add(y + h);
                return;
            }
            r = Math.Max(0, r);
            void Corner(float cx, float cy, double start)
            {
                for (int k = 0; k <= segments; k++)
                {
                    double a = start + Math.PI / 2 * k / segments;
                    path.Add(cx + (float)Math.Cos(a) * r);
                    path.Add(cy + (float)Math.Sin(a) * r);
                }
            }
            Corner(x + w - r, y + r, -Math.PI / 2);
            Corner(x + w - r, y + h - r, 0);
            Corner(x + r, y + h - r, Math.PI / 2);
            Corner(x + r, y + r, Math.PI);
        }

        static void FillPath()
        {
            buffer.Clear();
            int n = path.Count / 2;
            float cx = 0, cy = 0;
            for (int k = 0; k < n; k++) { cx += path[k * 2]; cy += path[k * 2 + 1]; }
            cx /= n;
            cy /= n;
            for (int k = 0; k < n; k++)
            {
                int j = (k + 1) % n;
                buffer.Add(X(cx)); buffer.Add(Y(cy));
                buffer.Add(X(path[k * 2])); buffer.Add(Y(path[k * 2 + 1]));
                buffer.Add(X(path[j * 2])); buffer.Add(Y(path[j * 2 + 1]));
            }
            Backend.Triangles(buffer, color);
        }

        // A band between two closed outlines with the same number of points.
        static void Ring(List<float> outer, List<float> inner)
        {
            buffer.Clear();
            int n = outer.Count / 2;
            for (int k = 0; k < n; k++)
            {
                int j = (k + 1) % n;
                float ox0 = X(outer[k * 2]), oy0 = Y(outer[k * 2 + 1]), ox1 = X(outer[j * 2]), oy1 = Y(outer[j * 2 + 1]);
                float ix0 = X(inner[k * 2]), iy0 = Y(inner[k * 2 + 1]), ix1 = X(inner[j * 2]), iy1 = Y(inner[j * 2 + 1]);
                buffer.Add(ox0); buffer.Add(oy0); buffer.Add(ox1); buffer.Add(oy1); buffer.Add(ix1); buffer.Add(iy1);
                buffer.Add(ox0); buffer.Add(oy0); buffer.Add(ix1); buffer.Add(iy1); buffer.Add(ix0); buffer.Add(iy0);
            }
            Backend.Triangles(buffer, color);
        }

        // love.graphics.rectangle(mode, x, y, w, h, radius)
        public static void Rectangle(bool fill, float x, float y, float w, float h, float radius = 0)
        {
            if (w <= 0 || h <= 0) return;
            radius = Math.Max(0, Math.Min(radius, Math.Min(w / 2, h / 2)));
            int segments = radius > 0 ? CornerSegments(radius) : 0;
            if (fill)
            {
                RoundedRectPath(x, y, w, h, radius, 0, segments);
                FillPath();
                return;
            }
            // the stroke is centred on the outline, like LÖVE's
            float half = lineWidth / 2;
            RoundedRectPath(x, y, w, h, radius + half, half, segments);
            var outer = new List<float>(path);
            RoundedRectPath(x, y, w, h, radius - half, -half, segments);
            Ring(outer, path);
        }

        public static void Circle(bool fill, float cx, float cy, float radius)
        {
            int segments = Segments(radius);
            path.Clear();
            for (int k = 0; k < segments; k++)
            {
                double a = 2 * Math.PI * k / segments;
                path.Add(cx + (float)Math.Cos(a) * radius);
                path.Add(cy + (float)Math.Sin(a) * radius);
            }
            if (fill)
            {
                FillPath();
                return;
            }
            var outer = new List<float>();
            var inner = new List<float>();
            float half = lineWidth / 2;
            for (int k = 0; k < segments; k++)
            {
                double a = 2 * Math.PI * k / segments;
                outer.Add(cx + (float)Math.Cos(a) * (radius + half));
                outer.Add(cy + (float)Math.Sin(a) * (radius + half));
                inner.Add(cx + (float)Math.Cos(a) * (radius - half));
                inner.Add(cy + (float)Math.Sin(a) * (radius - half));
            }
            Ring(outer, inner);
        }

        // An open arc outline (love.graphics.arc("line", "open", ...)), angles in radians.
        public static void ArcLine(float cx, float cy, float radius, float a1, float a2)
        {
            int segments = Math.Max(4, Segments(radius) / 2);
            var points = new List<float>();
            for (int k = 0; k <= segments; k++)
            {
                double a = a1 + (a2 - a1) * k / segments;
                points.Add(cx + (float)Math.Cos(a) * radius);
                points.Add(cy + (float)Math.Sin(a) * radius);
            }
            Line(points.ToArray());
        }

        // A polyline with the current line width (love.graphics.line), joints filled with small squares.
        public static void Line(params float[] points)
        {
            buffer.Clear();
            float half = lineWidth / 2;
            for (int k = 0; k + 3 < points.Length; k += 2)
            {
                float x0 = points[k], y0 = points[k + 1], x1 = points[k + 2], y1 = points[k + 3];
                float dx = x1 - x0, dy = y1 - y0;
                float length = (float)Math.Sqrt(dx * dx + dy * dy);
                if (length <= 0) continue;
                float nx = -dy / length * half, ny = dx / length * half;
                Quad(x0 + nx, y0 + ny, x1 + nx, y1 + ny, x1 - nx, y1 - ny, x0 - nx, y0 - ny);
            }
            for (int k = 2; k + 3 < points.Length; k += 2) // joints
            {
                float x = points[k], y = points[k + 1];
                Quad(x - half, y - half, x + half, y - half, x + half, y + half, x - half, y + half);
            }
            Backend.Triangles(buffer, color);
        }

        static void Quad(float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
        {
            buffer.Add(X(x0)); buffer.Add(Y(y0)); buffer.Add(X(x1)); buffer.Add(Y(y1)); buffer.Add(X(x2)); buffer.Add(Y(y2));
            buffer.Add(X(x0)); buffer.Add(Y(y0)); buffer.Add(X(x2)); buffer.Add(Y(y2)); buffer.Add(X(x3)); buffer.Add(Y(y3));
        }

        // love.graphics.draw(image, x, y, 0, scaleX, scaleY)
        public static void Draw(Img image, float x, float y, float scaleX = 1, float scaleY = 1)
        {
            if (image == null) return;
            float x0 = X(x), y0 = Y(y), x1 = X(x + image.Width * scaleX), y1 = Y(y + image.Height * scaleY);
            Backend.Image(image, Math.Min(x0, x1), Math.Min(y0, y1), Math.Max(x0, x1), Math.Max(y0, y1), color);
        }

        // love.graphics.print(text, x, y, 0, scaleX, scaleY) with the current font.
        public static void Print(string text, float x, float y, float scaleX = 1, float scaleY = 1)
        {
            if (string.IsNullOrEmpty(text)) return;
            string[] lines = text.Split('\n');
            for (int k = 0; k < lines.Length; k++)
                Backend.Text(font, lines[k], X(x), Y(y + k * font.Height * scaleY), sx * scaleX, sy * scaleY, color);
        }

        // love.graphics.printf(text, x, y, limit, align): wrapped text.
        public static void Printf(string text, float x, float y, float limit, Align align = Align.Left)
        {
            if (string.IsNullOrEmpty(text)) return;
            var widths = new List<float>();
            var lines = font.GetWrap(text, limit, widths);
            for (int k = 0; k < lines.Count; k++)
            {
                float offset = 0;
                if (align == Align.Center) offset = (float)Math.Floor((limit - widths[k]) / 2);
                else if (align == Align.Right) offset = limit - widths[k];
                Backend.Text(font, lines[k], X(x + offset), Y(y + k * font.Height), sx, sy, color);
            }
        }
    }
}
