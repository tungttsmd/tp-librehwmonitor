using System;
using System.Collections.Generic;
using System.IO;

public static class Env
{
    private static readonly Dictionary<string, string> _data = new();

    public static void Load(string path = ".env")
    {
        if (!File.Exists(path))
            return;

        foreach (var line in File.ReadAllLines(path))
        {
            var l = line.Trim();
            if (string.IsNullOrEmpty(l) || l.StartsWith("#"))
                continue;

            var idx = l.IndexOf('=');
            if (idx <= 0)
                continue;

            var key = l.Substring(0, idx).Trim();
            var val = l.Substring(idx + 1).Trim();

            _data[key] = val;
        }
    }

    public static string Get(string key, string def = "")
        => _data.TryGetValue(key, out var v) ? v : def;

    public static int GetInt(string key, int def)
        => int.TryParse(Get(key), out var v) ? v : def;

    public static bool GetBool(string key, bool def)
        => bool.TryParse(Get(key), out var v) ? v : def;
}
