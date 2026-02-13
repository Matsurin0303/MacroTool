using System.Text.Json;

namespace MacroTool.WinForms.Core
{
    public sealed class RecentFilesStore
    {
        private readonly int _max;
        private readonly string _filePath;
        private readonly List<string> _items = new();

        public IReadOnlyList<string> Items => _items;

        public RecentFilesStore(string appName = "MacroTool", int maxItems = 10)
        {
            _max = Math.Max(1, maxItems);

            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName);

            Directory.CreateDirectory(dir);

            _filePath = Path.Combine(dir, "recent-files.json");

            Load();
        }

        public void Add(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            string full;
            try { full = Path.GetFullPath(path); }
            catch { return; }

            _items.RemoveAll(p => string.Equals(p, full, StringComparison.OrdinalIgnoreCase));
            _items.Insert(0, full);

            if (_items.Count > _max)
                _items.RemoveRange(_max, _items.Count - _max);

            Save();
        }

        public void Remove(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            string full;
            try { full = Path.GetFullPath(path); }
            catch { return; }

            _items.RemoveAll(p => string.Equals(p, full, StringComparison.OrdinalIgnoreCase));
            Save();
        }

        public void Clear()
        {
            _items.Clear();
            Save();
        }

        private void Load()
        {
            _items.Clear();
            if (!File.Exists(_filePath)) return;

            try
            {
                var json = File.ReadAllText(_filePath);
                var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

                foreach (var p in list.Where(p => !string.IsNullOrWhiteSpace(p)))
                    _items.Add(p);
            }
            catch
            {
                // 壊れていても落とさない（無視）
            }
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // 書けなくても落とさない（無視）
            }
        }
    }
}
