using Newtonsoft.Json;
using VentItMaui.Models;

namespace VentItMaui.Services;

public class ChatSessionService
{
    private readonly string _sessionsDir;

    public ChatSessionService()
    {
        var appData = FileSystem.AppDataDirectory;
        _sessionsDir = Path.Combine(appData, "sessions");
        Directory.CreateDirectory(_sessionsDir);
    }

    public List<ChatSession> GetAllSessions()
    {
        var files = Directory.GetFiles(_sessionsDir, "*.json")
            .OrderByDescending(File.GetLastWriteTime)
            .ToList();

        var sessions = new List<ChatSession>();
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var session = JsonConvert.DeserializeObject<ChatSession>(json);
                if (session != null)
                    sessions.Add(session);
            }
            catch { }
        }
        return sessions;
    }

    public ChatSession? LoadSession(string id)
    {
        var filePath = Path.Combine(_sessionsDir, $"{id}.json");
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<ChatSession>(json);
        }
        catch
        {
            return null;
        }
    }

    public void SaveSession(ChatSession session)
    {
        session.UpdatedAt = DateTime.UtcNow;
        if (string.IsNullOrEmpty(session.Name) && session.Messages.Count > 0)
        {
            var firstUserMessage = session.Messages.FirstOrDefault(m => m.Role == "user");
            if (firstUserMessage != null)
            {
                session.Name = firstUserMessage.Content.Length > 40
                    ? firstUserMessage.Content[..40] + "..."
                    : firstUserMessage.Content;
            }
        }

        var filePath = Path.Combine(_sessionsDir, $"{session.Id}.json");
        var json = JsonConvert.SerializeObject(session, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public void DeleteSession(string id)
    {
        var filePath = Path.Combine(_sessionsDir, $"{id}.json");
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public ChatSession CreateNewSession(string? name = null)
    {
        return new ChatSession { Name = name ?? "" };
    }
}
