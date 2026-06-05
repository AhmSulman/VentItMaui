namespace VentItMaui.Services;

public class ClaudeService
{
    private const string SystemPrompt = @"You are VentBot, a compassionate AI wellness companion and life coach for VentIt.
Your role is to help users:
- Clear their mind through active listening
- Plan and organize their goals and routines
- Journal and reflect on their experiences
- Build healthy habits and routines
- Gain clarity on decisions and problems

Be warm, empathetic, non-judgmental, and action-oriented. Ask follow-up questions to understand context deeply.
Keep responses concise but thoughtful (2-3 paragraphs max for chat).";

    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-sonnet-4-6";
    private readonly SettingsService _settingsService;
    private HttpClient? _httpClient;

    public string AuthMode { get; private set; } = "none";
    public bool IsAuthenticated => !string.IsNullOrEmpty(_settingsService.Settings.ClaudeApiKey);

    public ClaudeService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        DetectAuthMode();
    }

    private void DetectAuthMode()
    {
        if (!string.IsNullOrEmpty(_settingsService.Settings.ClaudeApiKey))
        {
            AuthMode = "api-key";
        }
    }

    public async Task<string> SendMessageAsync(List<(string role, string content)> messages)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Claude API key not configured. Please set it in Settings.");

        var client = _httpClient ?? new HttpClient();
        _httpClient = client;

        var payload = new
        {
            model = Model,
            max_tokens = 2048,
            system = SystemPrompt,
            messages = messages.Select(m => new { role = m.role, content = m.content }).ToList()
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = content
        };
        request.Headers.Add("x-api-key", _settingsService.Settings.ClaudeApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await client.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Claude API error: {responseText}");

        var jsonDoc = System.Text.Json.JsonDocument.Parse(responseText);
        var root = jsonDoc.RootElement;
        var firstContent = root.GetProperty("content")[0];
        return firstContent.GetProperty("text").GetString() ?? "";
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(List<(string role, string content)> messages)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Claude API key not configured. Please set it in Settings.");

        var client = _httpClient ?? new HttpClient();
        _httpClient = client;

        var payload = new
        {
            model = Model,
            max_tokens = 2048,
            system = SystemPrompt,
            stream = true,
            messages = messages.Select(m => new { role = m.role, content = m.content }).ToList()
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = content
        };
        request.Headers.Add("x-api-key", _settingsService.Settings.ClaudeApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        HttpResponseMessage? response = null;
        StreamReader? reader = null;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Claude API error: {errorText}");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                    continue;

                var data = line["data: ".Length..].Trim();
                if (data == "[DONE]")
                    break;

                var jsonDoc = System.Text.Json.JsonDocument.Parse(data);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("text", out var text))
                {
                    yield return text.GetString() ?? "";
                }
            }
        }
        finally
        {
            reader?.Dispose();
            response?.Dispose();
        }
    }
}
