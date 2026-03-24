namespace LataFile;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class LataFile
{
    public const string Version = "1.0-snapshot";
    private static readonly Action<string> Logger = (msg) => Console.WriteLine(msg);

    private readonly Dictionary<string, Dictionary<string, object>> _data = new();
    private List<string>? _readOnlySections = new();

    public void Load(string filePath)
    {
        if (!File.Exists(filePath)) return;

        string section = "default";
        foreach (var rawLine in File.ReadLines(filePath))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                section = line.Substring(1, line.Length - 2);
                if (!_data.ContainsKey(section)) _data[section] = new Dictionary<string, object>();
            }
            else if (line.Contains("="))
            {
                var parts = line.Split('=', 2);
                if (!_data.ContainsKey(section)) _data[section] = new Dictionary<string, object>();
                _data[section][parts[0].Trim()] = Parse(parts[1].Trim());
            }
        }
        UpdatePermissions();
        CheckAndFixVersion(filePath);
    }

    private void UpdatePermissions()
    {
        if (_data.TryGetValue("meta", out var meta) && meta.TryGetValue("readonly", out var roValue))
        {
            string? raw = Convert.ToString(roValue);
            _readOnlySections = raw?.Replace("[", "").Replace("]", "").Replace("\"", "")
                                  .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => s.Trim()).ToList();
        }
    }

    private void CheckAndFixVersion(string filePath)
    {
        if (!_data.ContainsKey("meta")) _data["meta"] = new Dictionary<string, object>();
        var meta = _data["meta"];
        
        string? fileVersion = meta.ContainsKey("version") ? Convert.ToString(meta["version"]) : "unknown";

        if (Version != fileVersion)
        {
            Logger($"[LATA] Version mismatch! File: {fileVersion} | Target: {Version}");
            meta["version"] = Version;
            SaveToFile(filePath);
        }
    }

    public void SetValue(string section, string key, object value)
    {
        if (_readOnlySections != null && _readOnlySections.Contains(section))
        {
            throw new UnauthorizedAccessException($"Section [{section}] is read-only!");
        }
        if (!_data.ContainsKey(section)) _data[section] = new Dictionary<string, object>();
        _data[section][key] = value;
    }

    public object? GetValue(string section, string key)
    {
        if (_data.TryGetValue(section, out var secData) && secData.TryGetValue(key, out var value))
        {
            return value;
        }
        return null;
    }

    public void SaveToFile(string filePath)
    {
        using var writer = new StreamWriter(filePath);
        foreach (var sectionEntry in _data)
        {
            writer.WriteLine($"[{sectionEntry.Key}]");
            foreach (var entry in sectionEntry.Value)
            {
                object val = entry.Value;
                string formatted = val switch
                {
                    float f => $"{f}f",
                    string s => $"\"{s}\"",
                    bool b => b.ToString().ToLower(),
                    _ => val.ToString() ?? ""
                };
                writer.WriteLine($"{entry.Key} = {formatted}");
            }
            writer.WriteLine();
        }
    }

    private object Parse(string v)
    {
        if (v.StartsWith("\"") && v.EndsWith("\"")) return v.Substring(1, v.Length - 2);
        if (v == "true" || v == "false") return bool.Parse(v);
        if (v.EndsWith("f") && float.TryParse(v.TrimEnd('f'), out float f)) return f;
        if (int.TryParse(v, out int i)) return i;
        return v;
    }

    public Dictionary<string, Dictionary<string, object>> GetData() => _data;
}