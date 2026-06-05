namespace VentItMaui.Models;

public class Routine
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Emoji { get; set; } = "✅";
    public string TimeOfDay { get; set; } = "Morning";
    public int DurationMinutes { get; set; } = 10;
    public int Streak { get; set; } = 0;
    public DateTime? LastCompletedDate { get; set; }
    public int SortOrder { get; set; } = 0;

    public bool IsCompletedToday => LastCompletedDate?.Date == DateTime.Today;
}

public class RoutineCompletion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RoutineId { get; set; } = "";
    public DateTime CompletedDate { get; set; } = DateTime.Now;
}
