using System.IO;
using System.Text.Json;
using RandomToolbox.App.Models;

namespace RandomToolbox.App.Services;

public sealed class StorageService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    public string DataDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RandomToolbox");
    public string DataPath => Path.Combine(DataDirectory, "data.json");

    public AppData Load()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            if (!File.Exists(DataPath))
            {
                var data = new AppData();
                Save(data);
                return data;
            }

            var json = File.ReadAllText(DataPath);
            return JsonSerializer.Deserialize<AppData>(json, _jsonOptions) ?? new AppData();
        }
        catch
        {
            BackupBrokenFile();
            var data = new AppData();
            Save(data);
            return data;
        }
    }

    public void Save(AppData data)
    {
        Directory.CreateDirectory(DataDirectory);
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        File.WriteAllText(DataPath, json);
    }

    private void BackupBrokenFile()
    {
        try
        {
            if (File.Exists(DataPath))
            {
                var backup = Path.Combine(DataDirectory, $"data.broken.{DateTime.Now:yyyyMMddHHmmss}.json");
                File.Copy(DataPath, backup, true);
            }
        }
        catch
        {
            // 配置损坏时不能影响软件启动。
        }
    }
}
