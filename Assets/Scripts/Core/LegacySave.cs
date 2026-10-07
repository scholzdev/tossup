using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Tossup
{
    // LÖVE saves are literal table data. Parse them without evaluating Lua or requiring a Lua runtime.
    public static class LegacySave
    {
        sealed class Table : Dictionary<string, object> { }

        public static ProfileData ProfileFromLua(string text)
        {
            try
            {
                var data = new Parser(text).Read() as Table;
                if (data == null || !data.ContainsKey("tokens") || !(Get(data,"unlocked") is Table)) return null;
                var profile = (ProfileData)ConvertData(data, typeof(ProfileData));
                if (Get(data,"loadouts") is Table loadouts)
                    foreach (var pair in loadouts)
                        if (Content.Characters.ContainsKey(pair.Key) && !profile.Sets.ContainsKey(pair.Key))
                            Profile.Sets(profile,pair.Key)[0].Coins = Profile.LimitedSet((List<string>)ConvertData(pair.Value,typeof(List<string>)),Game.StartMax);
                Profile.Sanitize(profile);
                return profile;
            }
            catch { return null; }
        }

        public static GameState RunFromLua(string text)
        {
            try
            {
                var root = new Parser(text).Read() as Table;
                if (root == null || !(Get(root,"version") is double version) || version != 1 || !(Get(root,"game") is Table data)) return null;
                var game = (GameState)ConvertData(data,typeof(GameState));
                if (game.Phase == Phase.Encounter || !RunSave.IsSafePoint(game)) return null; // a fight in progress cannot resume across the pouch rework
                // Use exactly the current validation/binding path, including catalog checks.
                return RunSave.Decode(RunSave.Encode(game));
            }
            catch { return null; }
        }

        // Import only when the Unity artifact is absent. Originals are kept untouched for rollback.
        public static bool ImportDirectory(string luaDirectory, string unityDirectory)
        {
            bool imported = false;
            Directory.CreateDirectory(unityDirectory);
            string profilePath = Path.Combine(unityDirectory,"profile.json"), runPath = Path.Combine(unityDirectory,"run.json");
            string oldProfile = Path.Combine(luaDirectory,"profile.lua"), oldRun = Path.Combine(luaDirectory,"run.lua");
            if (!File.Exists(profilePath) && File.Exists(oldProfile))
            {
                var profile = ProfileFromLua(File.ReadAllText(oldProfile));
                if (profile != null) { WriteNew(profilePath,Profile.Encode(profile)); imported = true; }
            }
            if (!File.Exists(runPath) && File.Exists(oldRun))
            {
                var game = RunFromLua(File.ReadAllText(oldRun));
                if (game != null) { WriteNew(runPath,RunSave.Encode(game)); imported = true; }
            }
            return imported;
        }

        static void WriteNew(string path,string text)
        {
            string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using (var stream = new FileStream(temporary,FileMode.CreateNew,FileAccess.Write))
                {
                    byte[] bytes=new UTF8Encoding(false).GetBytes(text);
                    stream.Write(bytes,0,bytes.Length);stream.Flush(true);
                }
                File.Move(temporary,path); // Atomic publication, refuses to replace a racing writer.
            }
            finally { if(File.Exists(temporary))File.Delete(temporary); }
        }

        static object Get(Table t,string key) => t.TryGetValue(key,out var value) ? value : null;
        static string Snake(string name)
        {
            var b = new StringBuilder();
            for (int i=0;i<name.Length;i++) { if (i>0 && char.IsUpper(name[i])) b.Append('_'); b.Append(char.ToLowerInvariant(name[i])); }
            return b.ToString();
        }
        static string LegacyField(string name)
        {
            switch(name)
            {
                case "Definition": return "id";
                case "Bonus": return "bonus";
                case "RefreshCost": return "refresh_cost";
                case "BestEndless": return "best_endless";
                case "GoldMult": return "gold_mult";
                case "PushUsed": return "pushing";
                default: return Snake(name);
            }
        }

        static object ConvertData(object value,Type type)
        {
            if (value == null || value is bool off && !off && type != typeof(bool)) return null;
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null) return ConvertData(value,underlying);
            if (type == typeof(CoinDef))
                return value is string id && Content.Coins.TryGetValue(id,out var coin) ? coin : throw new FormatException("unknown coin definition");
            if (type == typeof(string)) return value is string ? value : throw new FormatException("expected string");
            if (type == typeof(bool)) return value is bool ? value : throw new FormatException("expected bool");
            if (type.IsEnum)
            {
                if (!(value is string name)) throw new FormatException("expected phase");
                if (type == typeof(EffectType) || type == typeof(CoinType)) return DefinitionKeys.Parse(type,name);
                foreach (string candidate in Enum.GetNames(type))
                    if (candidate.ToUpperInvariant() == name.Replace("_","")) return Enum.Parse(type,candidate);
                throw new FormatException("unknown phase");
            }
            if (type == typeof(double)) return value is double ? value : throw new FormatException("expected number");
            if (type == typeof(int) || type == typeof(long))
            {
                if (!(value is double number) || number != Math.Floor(number)) throw new FormatException("expected integer");
                if (type == typeof(int)) return checked((int)number);
                return checked((long)number);
            }
            if (!(value is Table table)) throw new FormatException("expected table");
            if (type.IsGenericType)
            {
                Type generic = type.GetGenericTypeDefinition(), element = type.GetGenericArguments()[0];
                object result = Activator.CreateInstance(type);
                if (generic == typeof(List<>) || generic == typeof(HashSet<>))
                {
                    MethodInfo add = type.GetMethod("Add");
                    bool flags = generic == typeof(HashSet<>);
                    if (flags)
                    {
                        foreach (var pair in table)
                            if (pair.Value is bool on && on) add.Invoke(result,new[] { element == typeof(int) ? (object)int.Parse(pair.Key,CultureInfo.InvariantCulture) : pair.Key });
                    }
                    else
                    {
                        for (int i=1;i<=table.Count;i++)
                        {
                            if (!table.TryGetValue(i.ToString(CultureInfo.InvariantCulture),out var item)) throw new FormatException("sparse list");
                            add.Invoke(result,new[] { ConvertData(item,element) });
                        }
                    }
                    return result;
                }
                if (generic == typeof(Dictionary<,>))
                {
                    Type itemType = type.GetGenericArguments()[1]; var dictionary = (System.Collections.IDictionary)result;
                    foreach (var pair in table)
                        dictionary.Add(element == typeof(int) ? (object)int.Parse(pair.Key,CultureInfo.InvariantCulture) : pair.Key,ConvertData(pair.Value,itemType));
                    return dictionary;
                }
            }
            object instance = Activator.CreateInstance(type);
            foreach (var field in type.GetFields(BindingFlags.Public|BindingFlags.Instance))
            {
                string key = LegacyField(field.Name);
                if (table.TryGetValue(key,out var item))
                {
                    object converted = ConvertData(item,field.FieldType);
                    if (converted != null || !field.FieldType.IsValueType) field.SetValue(instance,converted);
                }
            }
            return instance;
        }

        sealed class Parser
        {
            readonly string text;
            int pos,nodes;
            public Parser(string source)
            {
                if (source == null || source.Length > 2*1024*1024) throw new FormatException("save too large");
                text = source;
            }
            char Current => pos<text.Length ? text[pos] : '\0';
            void Space() { while (char.IsWhiteSpace(Current)) pos++; }
            void Expect(char c) { Space(); if(Current!=c)throw new FormatException("expected "+c);pos++; }
            string Word()
            {
                int start=pos;
                while(char.IsLetterOrDigit(Current)||Current=='_')pos++;
                return text.Substring(start,pos-start);
            }
            public object Read()
            {
                Space(); if (char.IsLetter(Current)) { if(Word()!="return")throw new FormatException("save must contain literals"); }
                object value = Value(0); Space(); if(pos!=text.Length)throw new FormatException("trailing data"); return value;
            }
            object Value(int depth)
            {
                if(depth>64||++nodes>200000)throw new FormatException("save nesting limit");
                Space();
                if(Current=='{')return ReadTable(depth+1);
                if(Current=='\"'||Current=='\'')return ReadString();
                if(char.IsLetter(Current)||Current=='_')
                {
                    switch(Word()) { case "true":return true;case "false":return false;case "nil":return null;default:throw new FormatException("only literal values allowed"); }
                }
                int start=pos;
                if(Current=='+'||Current=='-')pos++;
                while(char.IsDigit(Current)||Current=='.')pos++;
                if(Current=='e'||Current=='E') { pos++;if(Current=='+'||Current=='-')pos++;while(char.IsDigit(Current))pos++; }
                if(!double.TryParse(text.Substring(start,pos-start),NumberStyles.Float,CultureInfo.InvariantCulture,out double n)||double.IsNaN(n)||double.IsInfinity(n))throw new FormatException("invalid number");
                return n;
            }
            Table ReadTable(int depth)
            {
                Expect('{'); var result=new Table();int index=1;Space();
                while(Current!='}')
                {
                    string key=null;
                    if(Current=='[')
                    {
                        pos++;object k=Value(depth);Expect(']');Expect('=');
                        if(k is string name)key=name;else if(k is double n && n==Math.Floor(n))key=n.ToString("0",CultureInfo.InvariantCulture);else throw new FormatException("invalid key");
                    }
                    else if(char.IsLetter(Current)||Current=='_')
                    {
                        int before=pos;string word=Word();Space();
                        if(Current=='=') { pos++;key=word; } else pos=before;
                    }
                    if(key==null)key=(index++).ToString(CultureInfo.InvariantCulture);
                    object value=Value(depth);if(value!=null)result[key]=value;Space();
                    if(Current==','||Current==';') { pos++;Space(); } else if(Current!='}')throw new FormatException("invalid table separator");
                }
                pos++;return result;
            }
            string ReadString()
            {
                char quote=Current;pos++;using(var bytes=new MemoryStream())
                {
                    while(Current!=quote)
                    {
                        if(pos>=text.Length)throw new FormatException("unterminated string");
                        char c=text[pos++];
                        if(c=='\\')
                        {
                            c=Current;pos++;
                            if(char.IsDigit(c))
                            {
                                int n=c-'0',count=1;while(count<3&&char.IsDigit(Current)){n=n*10+Current-'0';pos++;count++;}
                                if(n>255)throw new FormatException("invalid byte escape");bytes.WriteByte((byte)n);continue;
                            }
                            switch(c)
                            {
                                case 'a':c='\a';break;case 'b':c='\b';break;case 'f':c='\f';break;case 'n':c='\n';break;case 'r':c='\r';break;case 't':c='\t';break;case 'v':c='\v';break;
                                case '\\':case '\"':case '\'':case '\n':break;default:throw new FormatException("invalid escape");
                            }
                        }
                        else if(c=='\n'||c=='\r')throw new FormatException("newline in string");
                        string piece=c.ToString();if(char.IsHighSurrogate(c)&&char.IsLowSurrogate(Current))piece+=text[pos++];
                        byte[] encoded=Encoding.UTF8.GetBytes(piece);bytes.Write(encoded,0,encoded.Length);
                    }
                    pos++;return new UTF8Encoding(false,true).GetString(bytes.ToArray());
                }
            }
        }
    }
}
