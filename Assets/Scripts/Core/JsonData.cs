using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tossup
{
    // Small dependency-free JSON tree used by engine-independent game code.
    public sealed class JsonData
    {
        public Dictionary<string, JsonData> Object;
        public List<JsonData> Array;
        public string String;
        public double Number;
        public bool Boolean;
        public JsonKind Kind;

        public JsonData this[string key] => Object != null && Object.TryGetValue(key, out var value) ? value : null;

        public static JsonData Parse(string text)
        {
            var parser = new Parser(text ?? "");
            var value = parser.Value();
            parser.Space();
            if (!parser.End) throw parser.Error("unexpected trailing text");
            return value;
        }

        public static string Quote(string value)
        {
            var result = new StringBuilder("\"");
            foreach (char c in value ?? "")
            {
                switch (c)
                {
                    case '\"': result.Append("\\\""); break;
                    case '\\': result.Append("\\\\"); break;
                    case '\b': result.Append("\\b"); break;
                    case '\f': result.Append("\\f"); break;
                    case '\n': result.Append("\\n"); break;
                    case '\r': result.Append("\\r"); break;
                    case '\t': result.Append("\\t"); break;
                    default:
                        if (c < 32) result.Append("\\u").Append(((int)c).ToString("x4"));
                        else result.Append(c);
                        break;
                }
            }
            return result.Append('\"').ToString();
        }

        sealed class Parser
        {
            readonly string source;
            int index;
            public Parser(string source) { this.source = source; }
            public bool End => index >= source.Length;

            public FormatException Error(string message)
            {
                int line = 1;
                for (int i = 0; i < index; i++) if (source[i] == '\n') line++;
                return new FormatException("JSON, line " + line + ": " + message);
            }

            public void Space()
            {
                while (!End && (char.IsWhiteSpace(source[index]) || source[index] == '\ufeff')) index++;
            }

            public JsonData Value()
            {
                Space();
                if (End) throw Error("unexpected end of text");
                if (source[index] == '{') return ObjectValue();
                if (source[index] == '[') return ArrayValue();
                if (source[index] == '\"') return new JsonData { Kind = JsonKind.String, String = StringValue() };
                if (Match("true")) return new JsonData { Kind = JsonKind.Boolean, Boolean = true };
                if (Match("false")) return new JsonData { Kind = JsonKind.Boolean, Boolean = false };
                if (Match("null")) return new JsonData { Kind = JsonKind.Null };
                return NumberValue();
            }

            JsonData ObjectValue()
            {
                index++;
                var result = new JsonData { Kind = JsonKind.Object, Object = new Dictionary<string, JsonData>() };
                Space();
                if (Take('}')) return result;
                while (true)
                {
                    Space();
                    if (End || source[index] != '\"') throw Error("expected object key");
                    string key = StringValue();
                    Space();
                    Expect(':');
                    result.Object[key] = Value();
                    Space();
                    if (Take('}')) return result;
                    Expect(',');
                }
            }

            JsonData ArrayValue()
            {
                index++;
                var result = new JsonData { Kind = JsonKind.Array, Array = new List<JsonData>() };
                Space();
                if (Take(']')) return result;
                while (true)
                {
                    result.Array.Add(Value());
                    Space();
                    if (Take(']')) return result;
                    Expect(',');
                }
            }

            string StringValue()
            {
                Expect('\"');
                var result = new StringBuilder();
                while (!End)
                {
                    char c = source[index++];
                    if (c == '\"') return result.ToString();
                    if (c != '\\') { result.Append(c); continue; }
                    if (End) throw Error("unfinished escape");
                    c = source[index++];
                    switch (c)
                    {
                        case '\"': case '\\': case '/': result.Append(c); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u':
                            if (index + 4 > source.Length) throw Error("unfinished unicode escape");
                            result.Append((char)int.Parse(source.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            index += 4;
                            break;
                        default: throw Error("invalid escape");
                    }
                }
                throw Error("unfinished string");
            }

            JsonData NumberValue()
            {
                int start = index;
                if (!End && source[index] == '-') index++;
                while (!End && char.IsDigit(source[index])) index++;
                if (!End && source[index] == '.') { index++; while (!End && char.IsDigit(source[index])) index++; }
                if (!End && (source[index] == 'e' || source[index] == 'E'))
                {
                    index++;
                    if (!End && (source[index] == '+' || source[index] == '-')) index++;
                    while (!End && char.IsDigit(source[index])) index++;
                }
                if (!double.TryParse(source.Substring(start, index - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    throw Error("invalid value");
                return new JsonData { Kind = JsonKind.Number, Number = value };
            }

            bool Match(string text)
            {
                if (string.CompareOrdinal(source, index, text, 0, text.Length) != 0) return false;
                index += text.Length;
                return true;
            }

            bool Take(char c)
            {
                if (End || source[index] != c) return false;
                index++;
                return true;
            }

            void Expect(char c)
            {
                if (!Take(c)) throw Error("expected '" + c + "'");
            }
        }
    }

    public enum JsonKind { Null, Object, Array, String, Number, Boolean }
}
