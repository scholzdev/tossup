namespace Tossup
{
    public static class BuildInfo
    {
        public static string Number { get; private set; } = "0.1.0";
        public static string Build { get; private set; } = "dev";
        public static void Load(string json)
        {
            var data = JsonData.Parse(json);
            if (data["number"]?.Kind == JsonKind.String) Number = data["number"].String;
            if (data["build"]?.Kind == JsonKind.String) Build = data["build"].String;
        }
    }
}
