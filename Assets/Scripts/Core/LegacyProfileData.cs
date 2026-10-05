using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tossup
{
    // A parsed Lua table constructor. Positional entries are in Array (Lua index 1 = Array[0]); keyed
    // entries are in Hash (keys are strings or doubles).
    public sealed class LegacyProfileData
    {
        public readonly List<object> Array = new List<object>();
        public readonly Dictionary<object, object> Hash = new Dictionary<object, object>();

        public object this[string key] => Hash.TryGetValue(key, out var v) ? v : null;

        // Reads "return { ... }" (or a bare table) as written by the original game: the save file and the
        // locale tables. Values: strings, numbers, booleans, nil and nested tables. A bare word as a value
        // (the original wrote option strings unquoted, e.g. language = de) reads as that word.
        public static LegacyProfileData Parse(string text)
        {
            var parser = new Parser(text);
            parser.SkipSpace();
            if (parser.TryWord("return")) parser.SkipSpace();
            var value = parser.Value();
            parser.SkipSpace();
            if (!parser.AtEnd) throw parser.Error("unexpected text after the table");
            if (!(value is LegacyProfileData table)) throw parser.Error("expected a table");
            return table;
        }

        sealed class Parser
        {
            readonly string s;
            int i;

            public Parser(string text) { s = text ?? ""; }

            public bool AtEnd => i >= s.Length;

            public FormatException Error(string message)
            {
                int line = 1;
                for (int k = 0; k < i && k < s.Length; k++) if (s[k] == '\n') line++;
                return new FormatException("Lua table, line " + line + ": " + message);
            }

            public void SkipSpace()
            {
                while (i < s.Length)
                {
                    char c = s[i];
                    if (char.IsWhiteSpace(c) || c == '﻿') { i++; continue; }
                    if (c == '-' && i + 1 < s.Length && s[i + 1] == '-')
                    {
                        i += 2;
                        if (i + 1 < s.Length && s[i] == '[' && s[i + 1] == '[')
                        {
                            int end = s.IndexOf("]]", i + 2, StringComparison.Ordinal);
                            i = end < 0 ? s.Length : end + 2;
                        }
                        else
                        {
                            while (i < s.Length && s[i] != '\n') i++;
                        }
                        continue;
                    }
                    break;
                }
            }

            static bool IsNameStart(char c) => char.IsLetter(c) || c == '_';
            static bool IsNameChar(char c) => char.IsLetterOrDigit(c) || c == '_';

            public bool TryWord(string word)
            {
                if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) return false;
                int end = i + word.Length;
                if (end < s.Length && IsNameChar(s[end])) return false;
                i = end;
                return true;
            }

            string Name()
            {
                int start = i;
                while (i < s.Length && IsNameChar(s[i])) i++;
                return s.Substring(start, i - start);
            }

            public object Value()
            {
                SkipSpace();
                if (AtEnd) throw Error("unexpected end of text");
                char c = s[i];
                if (c == '{') return Table();
                if (c == '"' || c == '\'') return QuotedString();
                if (c == '[' && i + 1 < s.Length && (s[i + 1] == '[' || s[i + 1] == '=')) return LongString();
                if (char.IsDigit(c) || c == '.' || c == '-') return Number();
                if (IsNameStart(c))
                {
                    string word = Name();
                    if (word == "true") return true;
                    if (word == "false") return false;
                    if (word == "nil") return null;
                    return word;
                }
                throw Error("unexpected character '" + c + "'");
            }

            LegacyProfileData Table()
            {
                var table = new LegacyProfileData();
                i++; // {
                while (true)
                {
                    SkipSpace();
                    if (AtEnd) throw Error("unclosed table");
                    if (s[i] == '}') { i++; return table; }
                    if (s[i] == '[' && !(i + 1 < s.Length && (s[i + 1] == '[' || s[i + 1] == '=')))
                    {
                        i++;
                        object key = Value();
                        SkipSpace();
                        Expect(']');
                        SkipSpace();
                        Expect('=');
                        object value = Value();
                        if (key != null) table.Hash[key] = value;
                    }
                    else if (IsNameStart(s[i]) && NameIsKey())
                    {
                        string key = Name();
                        SkipSpace();
                        Expect('=');
                        table.Hash[key] = Value();
                    }
                    else
                    {
                        table.Array.Add(Value());
                    }
                    SkipSpace();
                    if (AtEnd) throw Error("unclosed table");
                    if (s[i] == ',' || s[i] == ';') { i++; continue; }
                    if (s[i] == '}') { i++; return table; }
                    throw Error("expected ',' or '}'");
                }
            }

            // Name = value (a key) rather than a bare word value.
            bool NameIsKey()
            {
                int k = i;
                while (k < s.Length && IsNameChar(s[k])) k++;
                while (k < s.Length && char.IsWhiteSpace(s[k])) k++;
                return k < s.Length && s[k] == '=' && !(k + 1 < s.Length && s[k + 1] == '=');
            }

            void Expect(char c)
            {
                if (AtEnd || s[i] != c) throw Error("expected '" + c + "'");
                i++;
            }

            string QuotedString()
            {
                char quote = s[i++];
                var sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd) throw Error("unfinished string");
                    char c = s[i++];
                    if (c == quote) return sb.ToString();
                    if (c == '\n') throw Error("unfinished string");
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw Error("unfinished string");
                    char esc = s[i++];
                    switch (esc)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'a': sb.Append('\a'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'v': sb.Append('\v'); break;
                        case '\n': sb.Append('\n'); break;
                        case '\r': sb.Append('\n'); if (!AtEnd && s[i] == '\n') i++; break;
                        default:
                            if (char.IsDigit(esc))
                            {
                                int code = esc - '0';
                                for (int k = 0; k < 2 && !AtEnd && char.IsDigit(s[i]); k++) code = code * 10 + (s[i++] - '0');
                                sb.Append((char)code);
                            }
                            else sb.Append(esc); // \\ \" \' and anything else
                            break;
                    }
                }
            }

            string LongString()
            {
                int start = i;
                i++; // [
                int level = 0;
                while (!AtEnd && s[i] == '=') { level++; i++; }
                if (AtEnd || s[i] != '[') { i = start; throw Error("invalid long string"); }
                i++;
                if (!AtEnd && s[i] == '\r') i++;
                if (!AtEnd && s[i] == '\n') i++;
                string close = "]" + new string('=', level) + "]";
                int end = s.IndexOf(close, i, StringComparison.Ordinal);
                if (end < 0) throw Error("unfinished long string");
                string body = s.Substring(i, end - i);
                i = end + close.Length;
                return body;
            }

            double Number()
            {
                int start = i;
                bool negative = false;
                if (s[i] == '-') { negative = true; i++; SkipSpace(); }
                if (i + 1 < s.Length && s[i] == '0' && (s[i + 1] == 'x' || s[i + 1] == 'X'))
                {
                    i += 2;
                    int hexStart = i;
                    while (i < s.Length && Uri.IsHexDigit(s[i])) i++;
                    double hex = long.Parse(s.Substring(hexStart, i - hexStart), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return negative ? -hex : hex;
                }
                int numStart = i;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E' ||
                    ((s[i] == '+' || s[i] == '-') && (s[i - 1] == 'e' || s[i - 1] == 'E')))) i++;
                if (numStart == i) { i = start; throw Error("malformed number"); }
                if (!double.TryParse(s.Substring(numStart, i - numStart), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    i = start;
                    throw Error("malformed number");
                }
                return negative ? -value : value;
            }
        }
    }
}
