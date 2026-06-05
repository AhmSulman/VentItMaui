namespace VentItMaui.Models;

public class AppSettings
{
    public string ClaudeApiKey { get; set; } = "";
    public string ClaudeModel { get; set; } = "claude-sonnet-4-6";
    public string UserName { get; set; } = "";
    public bool DarkMode { get; set; } = true;
    public string ActiveMindMapId { get; set; } = "";

    // Google Drive
    public string GoogleClientId { get; set; } = "";
    public string GoogleAccessToken { get; set; } = "";
    public string GoogleRefreshToken { get; set; } = "";
    public long GoogleTokenExpiryMs { get; set; } = 0;
}
