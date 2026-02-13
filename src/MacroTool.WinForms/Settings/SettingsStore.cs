using System.Text.Json;

namespace MacroTool.WinForms.Settings;

public sealed class SettingsStore
{
    private readonly string _path;

    public SettingsStore(string path)
    {
        _path = path;
    }

    public AppSettings Load()
    {
        if (!File.Exists(_path))
            return new AppSettings();

        var json = File.ReadAllText(_path);
        var obj = JsonSerializer.Deserialize<AppSettings>(json);
        return obj ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, json);
    }

    public static string DefaultPath()
    {
        // LocalAppData\MacroTool\settings.json
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MacroTool");
        return Path.Combine(dir, "settings.json");
    }
}
