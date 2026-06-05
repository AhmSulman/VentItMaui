namespace VentItMaui.Models;

public class NotePage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "Untitled";
    public string Content { get; set; } = "";
    public string Emoji { get; set; } = "📄";
    public string? ParentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public int SortOrder { get; set; } = 0;
    public List<NotePage> Children { get; set; } = new();
}
