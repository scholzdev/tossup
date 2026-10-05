using System;
using System.Globalization;
using System.Text;

namespace Tossup
{
    // Lua 5.1 number and string.format behaviour, so every text and log line reads exactly like the
    // original game (all Lua numbers are doubles: 3 prints "3", 0.15 prints "0.15").
    public static class GameText
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // tostring(number), i.e. "%.14g".
        public static string Num(double v)
        {
            if (double.IsNaN(v)) return "nan";
            if (double.IsInfinity(v)) return v > 0 ? "inf" : "-inf";
            if (v == Math.Floor(v) && Math.Abs(v) < 1e15) return ((long)v).ToString(Inv);
            return G(v, 14, false);
        }

        // C's %g: shortest of %e / %f for the given significant digits, trailing zeros removed.
        static string G(double v, int precision, bool upper)
        {
            if (precision == 0) precision = 1;
            if (v == 0) return "0";
            string e = v.ToString("E" + (precision - 1), Inv); // rounds to `precision` digits: d.dddE+xxx
            int cut = e.IndexOf('E');
            int exponent = int.Parse(e.Substring(cut + 1), Inv);
            if (exponent >= -4 && exponent < precision)
            {
                string f = v.ToString("F" + Math.Max(0, precision - 1 - exponent), Inv);
                return StripZeros(f);
            }
            string mantissa = StripZeros(e.Substring(0, cut));
            string sign = exponent < 0 ? "-" : "+";
            string digits = Math.Abs(exponent).ToString("00", Inv);
            return mantissa + (upper ? "E" : "e") + sign + digits;
        }

        static string StripZeros(string s)
        {
            if (s.IndexOf('.') < 0) return s;
            s = s.TrimEnd('0');
            return s.EndsWith(".") ? s.Substring(0, s.Length - 1) : s;
        }

        // string.format with the conversions the game uses: %d %i %s %f %g %e %q %x %c %% plus flags,
        // width and precision.
        public static string Format(string fmt, params object[] args)
        {
            var sb = new StringBuilder();
            int arg = 0;
            for (int i = 0; i < fmt.Length; i++)
            {
                char ch = fmt[i];
                if (ch != '%') { sb.Append(ch); continue; }
                i++;
                if (i >= fmt.Length) throw new FormatException("invalid format string (ends with '%')");
                if (fmt[i] == '%') { sb.Append('%'); continue; }
                int start = i;
                while (i < fmt.Length && "-+ #0".IndexOf(fmt[i]) >= 0) i++;
                string flags = fmt.Substring(start, i - start);
                int width = 0;
                while (i < fmt.Length && char.IsDigit(fmt[i])) width = width * 10 + (fmt[i++] - '0');
                int precision = -1;
                if (i < fmt.Length && fmt[i] == '.')
                {
                    i++;
                    precision = 0;
                    while (i < fmt.Length && char.IsDigit(fmt[i])) precision = precision * 10 + (fmt[i++] - '0');
                }
                if (i >= fmt.Length) throw new FormatException("invalid conversion in format string");
                char conv = fmt[i];
                if (arg >= args.Length) throw new FormatException("bad argument #" + (arg + 2) + " to 'format' (no value)");
                object value = args[arg++];
                string body;
                switch (conv)
                {
                    case 'd':
                    case 'i':
                        {
                            long n = (long)ToNumber(value);
                            body = Math.Abs(n).ToString(Inv);
                            if (precision >= 0) body = body.PadLeft(precision, '0');
                            body = (n < 0 ? "-" : flags.Contains("+") ? "+" : flags.Contains(" ") ? " " : "") + body;
                            break;
                        }
                    case 'f':
                        {
                            double d = ToNumber(value);
                            body = d.ToString("F" + (precision < 0 ? 6 : precision), Inv);
                            if (d >= 0 && flags.Contains("+")) body = "+" + body;
                            break;
                        }
                    case 'g':
                    case 'G':
                        body = G(ToNumber(value), precision < 0 ? 6 : precision, conv == 'G');
                        break;
                    case 'e':
                    case 'E':
                        {
                            string e = ToNumber(value).ToString((conv == 'e' ? "e" : "E") + (precision < 0 ? 6 : precision), Inv);
                            int cut = e.IndexOfAny(new[] { 'e', 'E' });
                            int exponent = int.Parse(e.Substring(cut + 1), Inv);
                            body = e.Substring(0, cut + 1) + (exponent < 0 ? "-" : "+") + Math.Abs(exponent).ToString("00", Inv);
                            break;
                        }
                    case 'x':
                    case 'X':
                        body = ((long)ToNumber(value)).ToString(conv == 'x' ? "x" : "X", Inv);
                        break;
                    case 'c':
                        body = ((char)(int)ToNumber(value)).ToString();
                        break;
                    case 's':
                        body = ToLuaString(value);
                        if (precision >= 0 && body.Length > precision) body = body.Substring(0, precision);
                        break;
                    case 'q':
                        body = Quote(ToLuaString(value));
                        break;
                    default:
                        throw new FormatException("invalid option '%" + conv + "' to 'format'");
                }
                if (body.Length < width)
                {
                    if (flags.Contains("-")) body = body.PadRight(width);
                    else if (flags.Contains("0") && conv != 's' && conv != 'q')
                    {
                        bool signed = body.Length > 0 && (body[0] == '-' || body[0] == '+' || body[0] == ' ');
                        body = signed ? body[0] + body.Substring(1).PadLeft(width - 1, '0') : body.PadLeft(width, '0');
                    }
                    else body = body.PadLeft(width);
                }
                sb.Append(body);
            }
            return sb.ToString();
        }

        // %q: a Lua string literal that reads back to the same text.
        public static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s)
            {
                if (c == '"') sb.Append("\\\"");
                else if (c == '\\') sb.Append("\\\\");
                else if (c == '\n') sb.Append("\\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\0') sb.Append("\\000");
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }

        public static string ToLuaString(object value)
        {
            switch (value)
            {
                case null: return "nil";
                case string s: return s;
                case bool b: return b ? "true" : "false";
                case double d: return Num(d);
                case float f: return Num(f);
                case int n: return n.ToString(Inv);
                case long n: return n.ToString(Inv);
                default: return Convert.ToString(value, Inv);
            }
        }

        static double ToNumber(object value)
        {
            switch (value)
            {
                case double d: return d;
                case float f: return f;
                case int n: return n;
                case long n: return n;
                case string s:
                    if (double.TryParse(s, NumberStyles.Float, Inv, out double parsed)) return parsed;
                    break;
            }
            throw new FormatException("bad argument to 'format' (number expected, got " + (value == null ? "nil" : value.GetType().Name) + ")");
        }
    }
}
