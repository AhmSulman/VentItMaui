using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VentItMaui.Models;

public enum JournalMood { Terrible = 1, Bad = 2, Neutral = 3, Good = 4, Great = 5 }

public class JournalEntry : INotifyPropertyChanged
{
    private JournalMood _mood = JournalMood.Neutral;
    private string _title = "";

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; } = DateTime.Today;

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public string Content { get; set; } = "";

    public JournalMood Mood
    {
        get => _mood;
        set
        {
            _mood = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MoodEmoji));
        }
    }

    public List<string> Tags { get; set; } = new();
    public string GratitudeList { get; set; } = "";
    public string WinOfTheDay { get; set; } = "";
    public string IntentionForTomorrow { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string MoodEmoji => Mood switch
    {
        JournalMood.Terrible => "😢",
        JournalMood.Bad => "😟",
        JournalMood.Neutral => "😐",
        JournalMood.Good => "😊",
        JournalMood.Great => "🤩",
        _ => "😐"
    };

    public string TagsDisplay => Tags.Count > 0 ? string.Join(", ", Tags) : "";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
