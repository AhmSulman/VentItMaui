using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using VentItMaui.Models;

namespace VentItMaui.Services;

public class GoogleDriveService
{
    private const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string DriveApiBase = "https://www.googleapis.com/drive/v3";

#if WINDOWS
    private const string RedirectUri = "http://localhost:8080/auth";
#else
    private const string RedirectUri = "ventit://auth";
#endif

    private readonly SettingsService _settingsService;
    private readonly HttpClient _httpClient = new();
    private string ClientId => _settingsService.Settings.GoogleClientId;

    public event EventHandler<SyncEventArgs>? SyncProgress;

    public GoogleDriveService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<bool> AuthenticateAsync()
    {
        try
        {
#if WINDOWS
            return await AuthenticateWindowsAsync();
#else
            return await AuthenticateAndroidAsync();
#endif
        }
        catch (Exception ex)
        {
            OnSyncProgress($"Auth error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> AuthenticateWindowsAsync()
    {
        try
        {
            // Check if Client ID is configured
            if (string.IsNullOrEmpty(ClientId))
            {
                OnSyncProgress("ERROR: Google Client ID not configured. Go to Settings and enter your Client ID from Google Cloud Console.");
                return false;
            }

            var state = Guid.NewGuid().ToString();
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            // Simplified OAuth URL (without PKCE which can cause issues)
            var authUrl = $"{AuthEndpoint}?" +
                $"client_id={Uri.EscapeDataString(ClientId)}&" +
                $"redirect_uri={Uri.EscapeDataString(RedirectUri)}&" +
                $"response_type=code&" +
                $"scope={Uri.EscapeDataString("https://www.googleapis.com/auth/drive.appdata")}&" +
                $"access_type=offline&" +
                $"prompt=consent";

            OnSyncProgress($"Opening Google sign-in browser...");
            Debug.WriteLine($"Auth URL: {authUrl}");

            // Open browser
            try
            {
                await Launcher.Default.OpenAsync(new Uri(authUrl));
            }
            catch (Exception ex)
            {
                OnSyncProgress($"Failed to open browser: {ex.Message}");
                return false;
            }

            // Wait for OAuth callback on localhost
            var code = await WaitForOAuthCallbackAsync();
            if (string.IsNullOrEmpty(code))
            {
                OnSyncProgress("OAuth cancelled or timed out (no auth code received)");
                return false;
            }

            var exchanged = await ExchangeCodeForTokenSimpleAsync(code);
            if (exchanged)
            {
                OnSyncProgress("Successfully authenticated with Google Drive");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            OnSyncProgress($"Auth error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> AuthenticateAndroidAsync()
    {
        try
        {
            var state = Guid.NewGuid().ToString();
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            var authUrl = $"{AuthEndpoint}?" +
                $"client_id={Uri.EscapeDataString(ClientId)}&" +
                $"redirect_uri={Uri.EscapeDataString(RedirectUri)}&" +
                $"response_type=code&" +
                $"scope={Uri.EscapeDataString("https://www.googleapis.com/auth/drive.appdata")}&" +
                $"state={state}&" +
                $"code_challenge={codeChallenge}&" +
                $"code_challenge_method=S256&" +
                $"access_type=offline&" +
                $"prompt=consent";

            var result = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(authUrl),
                new Uri(RedirectUri)
            );

            if (result == null || !result.Properties.TryGetValue("code", out var code))
                return false;

            var exchanged = await ExchangeCodeForTokenAsync(code, codeVerifier);
            if (exchanged)
            {
                OnSyncProgress("Successfully authenticated with Google Drive");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            OnSyncProgress($"Auth error: {ex.Message}");
            return false;
        }
    }

    private async Task<string> WaitForOAuthCallbackAsync()
    {
        try
        {
            using var listener = new System.Net.HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();

            // Timeout after 2 minutes
            var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(2));
            var context = await listener.GetContextAsync().ConfigureAwait(false);

            var query = context.Request.Url?.Query ?? "";
            var code = ExtractCodeFromQuery(query);

            // Send response to browser
            using var response = context.Response;
            response.StatusCode = 200;
            response.ContentType = "text/html";
            var html = @"
                <html>
                <body style='font-family: Arial; text-align: center; padding: 50px;'>
                    <h1 style='color: #7C3AED;'>✓ Authentication Successful</h1>
                    <p>You can close this window and return to VentIt.</p>
                </body>
                </html>";
            using var writer = new StreamWriter(response.OutputStream);
            await writer.WriteAsync(html);
            writer.Close();

            listener.Stop();
            return code;
        }
        catch
        {
            return "";
        }
    }

    private static string ExtractCodeFromQuery(string query)
    {
        var parts = query.Split('&');
        foreach (var part in parts)
        {
            if (part.StartsWith("code="))
                return Uri.UnescapeDataString(part["code=".Length..]);
        }
        return "";
    }

    private async Task<bool> ExchangeCodeForTokenSimpleAsync(string code)
    {
        try
        {
            OnSyncProgress("Exchanging authorization code for token...");

            // Simple token exchange without PKCE
            var request = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", RedirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });

            var response = await _httpClient.PostAsync(TokenEndpoint, request);
            var json = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"Token response status: {response.StatusCode}");
            Debug.WriteLine($"Token response: {json}");

            if (!response.IsSuccessStatusCode)
            {
                OnSyncProgress($"Token exchange failed: {json}");
                return false;
            }

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var accessToken = root.GetProperty("access_token").GetString() ?? "";
            var refreshToken = root.TryGetProperty("refresh_token", out var rt)
                ? rt.GetString() ?? ""
                : "";
            var expiresIn = root.GetProperty("expires_in").GetInt32();

            _settingsService.Settings.GoogleAccessToken = accessToken;
            _settingsService.Settings.GoogleRefreshToken = refreshToken;
            _settingsService.Settings.GoogleTokenExpiryMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (expiresIn * 1000L);
            _settingsService.Save();

            OnSyncProgress("Token obtained successfully!");
            return true;
        }
        catch (Exception ex)
        {
            OnSyncProgress($"Token exchange error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExchangeCodeForTokenAsync(string code, string codeVerifier)
    {
        try
        {
            var request = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", RedirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code_verifier", codeVerifier)
            });

            var response = await _httpClient.PostAsync(TokenEndpoint, request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return false;

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var accessToken = root.GetProperty("access_token").GetString() ?? "";
            var refreshToken = root.TryGetProperty("refresh_token", out var rt)
                ? rt.GetString() ?? ""
                : "";
            var expiresIn = root.GetProperty("expires_in").GetInt32();

            _settingsService.Settings.GoogleAccessToken = accessToken;
            _settingsService.Settings.GoogleRefreshToken = refreshToken;
            _settingsService.Settings.GoogleTokenExpiryMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (expiresIn * 1000L);
            _settingsService.Save();

            return true;
        }
        catch (Exception ex)
        {
            OnSyncProgress($"Token exchange error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> BackupAsync()
    {
        try
        {
            OnSyncProgress("Starting backup...");

            if (string.IsNullOrEmpty(_settingsService.Settings.GoogleAccessToken))
            {
                OnSyncProgress("Not authenticated. Please sign in to Google Drive first.");
                return false;
            }

            // Ensure token is fresh
            if (!await RefreshTokenIfNeededAsync())
            {
                OnSyncProgress("Failed to refresh Google authentication.");
                return false;
            }

            // Create a zip of app data
            var appDataDir = FileSystem.AppDataDirectory;
            var zipPath = Path.Combine(FileSystem.CacheDirectory, "ventit_backup.zip");

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            OnSyncProgress("Zipping data...");
            ZipFile.CreateFromDirectory(appDataDir, zipPath);

            OnSyncProgress("Uploading to Google Drive...");
            await UploadFileAsync(zipPath, "ventit_backup.zip");

            File.Delete(zipPath);
            OnSyncProgress("Backup completed successfully!");
            return true;
        }
        catch (Exception ex)
        {
            OnSyncProgress($"Backup error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RestoreAsync()
    {
        try
        {
            OnSyncProgress("Starting restore...");

            if (string.IsNullOrEmpty(_settingsService.Settings.GoogleAccessToken))
            {
                OnSyncProgress("Not authenticated. Please sign in to Google Drive first.");
                return false;
            }

            // Ensure token is fresh
            if (!await RefreshTokenIfNeededAsync())
            {
                OnSyncProgress("Failed to refresh Google authentication.");
                return false;
            }

            OnSyncProgress("Downloading from Google Drive...");
            var fileId = await FindBackupFileAsync();
            if (string.IsNullOrEmpty(fileId))
            {
                OnSyncProgress("No backup found on Google Drive.");
                return false;
            }

            var zipPath = Path.Combine(FileSystem.CacheDirectory, "ventit_restore.zip");
            await DownloadFileAsync(fileId, zipPath);

            OnSyncProgress("Extracting backup...");
            var appDataDir = FileSystem.AppDataDirectory;

            // Create a temp extract dir first to validate
            var tempExtractDir = Path.Combine(FileSystem.CacheDirectory, "ventit_extract_temp");
            if (Directory.Exists(tempExtractDir))
                Directory.Delete(tempExtractDir, true);

            ZipFile.ExtractToDirectory(zipPath, tempExtractDir);

            // Merge/overwrite: copy each file from temp to appdata
            foreach (var file in Directory.GetFiles(tempExtractDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(tempExtractDir, file);
                var destPath = Path.Combine(appDataDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? appDataDir);
                File.Copy(file, destPath, overwrite: true);
            }

            Directory.Delete(tempExtractDir, true);
            File.Delete(zipPath);

            OnSyncProgress("Restore completed successfully!");
            return true;
        }
        catch (Exception ex)
        {
            OnSyncProgress($"Restore error: {ex.Message}");
            return false;
        }
    }

    private async Task UploadFileAsync(string filePath, string fileName)
    {
        var url = $"{DriveApiBase}/files?uploadType=multipart";

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");

        var metadata = new { name = fileName, parents = new[] { "appDataFolder" } };
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
        var metadataContent = new StringContent(metadataJson, Encoding.UTF8, "application/json");

        content.Add(metadataContent, "metadata");
        content.Add(fileContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            _settingsService.Settings.GoogleAccessToken
        );

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string?> FindBackupFileAsync()
    {
        var url = $"{DriveApiBase}/files?" +
            $"q={Uri.EscapeDataString("name='ventit_backup.zip' and trashed=false")}&" +
            $"spaces=appDataFolder&" +
            $"fields=files(id)";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            _settingsService.Settings.GoogleAccessToken
        );

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("files", out var files) && files.GetArrayLength() > 0)
        {
            return files[0].GetProperty("id").GetString();
        }

        return null;
    }

    private async Task DownloadFileAsync(string fileId, string savePath)
    {
        var url = $"{DriveApiBase}/files/{fileId}?alt=media";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            _settingsService.Settings.GoogleAccessToken
        );

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(savePath, data);
    }

    private async Task<bool> RefreshTokenIfNeededAsync()
    {
        if (string.IsNullOrEmpty(_settingsService.Settings.GoogleRefreshToken))
            return false;

        var expiryTime = new DateTimeOffset(
            DateTime.UnixEpoch.AddMilliseconds(_settingsService.Settings.GoogleTokenExpiryMs)
        );

        if (DateTimeOffset.UtcNow < expiryTime - TimeSpan.FromMinutes(5))
            return true; // Token still valid

        try
        {
            var request = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("refresh_token", _settingsService.Settings.GoogleRefreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token")
            });

            var response = await _httpClient.PostAsync(TokenEndpoint, request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return false;

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var accessToken = root.GetProperty("access_token").GetString() ?? "";
            var expiresIn = root.GetProperty("expires_in").GetInt32();

            _settingsService.Settings.GoogleAccessToken = accessToken;
            _settingsService.Settings.GoogleTokenExpiryMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (expiresIn * 1000L);
            _settingsService.Save();

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private void OnSyncProgress(string message)
    {
        SyncProgress?.Invoke(this, new SyncEventArgs { Message = message });
    }
}

public class SyncEventArgs : EventArgs
{
    public string Message { get; set; } = "";
}
