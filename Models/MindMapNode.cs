namespace VentItMaui.Models;

public class MindMap
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "New Mind Map";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class MindMapNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MindMapId { get; set; } = "";
    public string Text { get; set; } = "New Node";
    public double X { get; set; } = 100;
    public double Y { get; set; } = 100;
    public double Width { get; set; } = 130;
    public double Height { get; set; } = 42;
    public string Color { get; set; } = "#7C3AED";
    public string? ParentNodeId { get; set; }
}
