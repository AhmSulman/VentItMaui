using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VentItMaui.Models;

public class ChatMessage : INotifyPropertyChanged
{
    private string _content = "";

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = "";
    public string Role { get; set; } = "user";

    public string Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(); }
    }

    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsUser => Role == "user";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
