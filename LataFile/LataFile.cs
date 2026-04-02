namespace LataFile
{
    public class LataFile
    {
        private const string Lata = "1.2-snapshot";
        
        private readonly Dictionary<string, Dictionary<string, object>> _data = new();
        private List<string> _readOnlySections = new();

        public void Load(string filePath)
        {
            _data.Clear();
            
            if (!File.Exists(filePath)) 
                throw new FileNotFoundException($"File not found: {filePath}");

            string? section = null;
            foreach (var rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2);
                    if (!_data.ContainsKey(section))
                        _data[section] = new Dictionary<string, object>();
                }
                else if (line.Contains('=') && section != null)
                {
                    var parts = line.Split('=', 2);
                    _data[section][parts[0].Trim()] = Parse(parts[1].Trim());
                }
            }

            if (!_data.ContainsKey("meta"))
                throw new IOException($"Cannot find [meta] section inside .lata - {Path.GetFileName(filePath)}");

            UpdatePermissions();
            if (!_data.TryGetValue("meta", out var meta)) meta = new Dictionary<string, object>();
            
            string fileVersion = meta.TryGetValue("version", out var value) ? value.ToString()! : "unknown";

            if (Lata != fileVersion)
                throw new IOException($"Unsupported Lata version! Latest supported: {Lata}");
        }

        private void UpdatePermissions()
        {
            if (_data.TryGetValue("meta", out var meta) && meta.TryGetValue("readonly", out var ro))
            {
                string raw = ro.ToString() ?? "";
                _readOnlySections = raw.Replace("[", "").Replace("]", "").Replace("\"", "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
            }
        }

        public void SetValue(string section, string key, object value)
        {
            if (_readOnlySections.Contains(section))
                throw new UnauthorizedAccessException($"Cannot modify read-only section: {section}");

            if (!_data.ContainsKey(section))
                _data[section] = new Dictionary<string, object>();

            _data[section][key] = value;
        }

        public object? Get(string section, string key)
        {
            if (_data.TryGetValue(section, out var sectionData))
                return sectionData.GetValueOrDefault(key);
            return null;
        }

        private object Parse(string v)
        {
            if (v.StartsWith("\"") && v.EndsWith("\"")) return v.Substring(1, v.Length - 2);
            if (v == "true" || v == "false") return bool.Parse(v);
            if (v.EndsWith("f") && float.TryParse(v.Replace("f", ""), out float f)) return f;
            if (int.TryParse(v, out int i)) return i;
            return v;
        }

        public void SaveToFile(string filePath)
        {
            using var writer = new StreamWriter(filePath);
            
            if (_data.TryGetValue("meta", out var value))
                WriteSection(writer, "meta", value);
            else
                throw new IOException("Cannot save: Missing [meta] section!");

            foreach (var entry in _data)
            {
                if (entry.Key == "meta") continue;
                WriteSection(writer, entry.Key, entry.Value);
            }
        }

        private void WriteSection(StreamWriter writer, string name, Dictionary<string, object> content)
        {
            writer.WriteLine($"[{name}]");
            foreach (var entry in content)
            {
                writer.WriteLine($"{entry.Key} = {FormatValue(entry.Value)}");
            }
            writer.WriteLine();
        }

        private string FormatValue(object value) => value switch
        {
            float f => $"{f}f",
            string s => $"\"{s}\"",
            bool b => b.ToString().ToLower(),
            _ => value.ToString() ?? ""
        };

        public Dictionary<string, Dictionary<string, object>> GetData() => _data;
    }
}