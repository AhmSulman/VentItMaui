namespace VentItMaui.Models;

public class Song
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Basic Info
    public string Title { get; set; } = "Untitled Song";
    public string Artist { get; set; } = "";
    public string Genre { get; set; } = "Other";

    // Musical Details
    public string Key { get; set; } = "C Major";
    public int BPM { get; set; } = 120;
    public string TimeSignature { get; set; } = "4/4";
    public string Mood { get; set; } = "Neutral";

    // Lyrics Structure
    public string Verse1 { get; set; } = "";
    public string PreChorus { get; set; } = "";
    public string Chorus { get; set; } = "";
    public string Verse2 { get; set; } = "";
    public string Bridge { get; set; } = "";
    public string Outro { get; set; } = "";

    // Musical Elements
    public string ChordProgression { get; set; } = "";
    public string MelodyNotes { get; set; } = "";
    public string Instrumentation { get; set; } = "";

    // Creative Elements
    public string Themes { get; set; } = "";
    public string StoryBehindSong { get; set; } = "";
    public string Inspiration { get; set; } = "";
    public string TargetAudience { get; set; } = "";

    // Development
    public string DemoNotes { get; set; } = "";
    public string NextSteps { get; set; } = "";
    public string Status { get; set; } = "Ideation"; // Ideation, Drafting, Revising, Complete

    // Metrics
    public int CompletionPercentage { get; set; } = 0;
    public List<string> Tags { get; set; } = new();
}
