namespace VentItMaui.Models;

public class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> Messages { get; set; } = [];

    public string Summary
    {
        get
        {
            if (Messages.Count == 0) return "";
            var firstMessage = Messages.FirstOrDefault(m => !m.IsUser);
            if (firstMessage == null) return "";
            return firstMessage.Content.Length > 60
                ? new string(firstMessage.Content.Take(60).ToArray()) + "..."
                : firstMessage.Content;
        }
    }
}
