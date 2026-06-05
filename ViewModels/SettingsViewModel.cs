using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VentItMaui.Services;

namespace VentItMaui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly GoogleDriveService _googleDriveService;

    [ObservableProperty]
    private string apiKey = "";

    [ObservableProperty]
    private string userName = "";

    [ObservableProperty]
    private bool isAuthenticated = false;

    public SettingsViewModel()
    {
        _settingsService = ServiceProvider.GetService<SettingsService>()!;
        _googleDriveService = ServiceProvider.GetService<GoogleDriveService>()!;

        LoadSettings();
    }

    private void LoadSettings()
    {
        ApiKey = _settingsService.Settings.ClaudeApiKey;
        UserName = _settingsService.Settings.UserName;
        IsAuthenticated = !string.IsNullOrEmpty(ApiKey);
    }

    [RelayCommand]
    public void SaveSettings()
    {
        _settingsService.Settings.ClaudeApiKey = ApiKey;
        _settingsService.Settings.UserName = UserName;
        _settingsService.Save();
        IsAuthenticated = !string.IsNullOrEmpty(ApiKey);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current?.MainPage?.DisplayAlert("Success", "Settings saved!", "OK");
        });
    }

    [RelayCommand]
    public void ClearApiKey()
    {
        ApiKey = "";
        _settingsService.Settings.ClaudeApiKey = "";
        _settingsService.Save();
        IsAuthenticated = false;
    }
}

internal static class ServiceProvider
{
    public static T? GetService<T>() where T : class
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService(typeof(T)) as T;
    }
}
