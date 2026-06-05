using Newtonsoft.Json;
using VentItMaui.Models;

namespace VentItMaui.Services;

public class SettingsService
{
    private AppSettings _settings = null!;
    private readonly string _settingsPath;

    public AppSettings Settings => _settings;

    public SettingsService()
    {
        var appData = FileSystem.AppDataDirectory;
        _settingsPath = Path.Combine(appData, "settings.json");
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new();
            }
            else
            {
                _settings = new();
            }
        }
        catch
        {
            _settings = new();
        }
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
        File.WriteAllText(_settingsPath, json);
    }
}
