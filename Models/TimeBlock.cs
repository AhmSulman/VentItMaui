namespace VentItMaui.Models;

public class TimeBlock
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Date { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public int StartHour { get; set; } = 8;
    public int StartMinute { get; set; } = 0;
    public int DurationMinutes { get; set; } = 60;
    public string Title { get; set; } = "";
    public string Category { get; set; } = "Work";
    public string Color { get; set; } = "#7C3AED";
    public string Notes { get; set; } = "";
    public bool IsCompleted { get; set; } = false;

    public string StartTime => $"{StartHour:D2}:{StartMinute:D2}";
    public string EndTime
    {
        get
        {
            var total = StartHour * 60 + StartMinute + DurationMinutes;
            return $"{total / 60 % 24:D2}:{total % 60:D2}";
        }
    }
}
