using Newtonsoft.Json;
using VentItMaui.Models;

namespace VentItMaui.Services;

public class JournalService
{
    private readonly string _journalDir;

    public JournalService()
    {
        var appData = FileSystem.AppDataDirectory;
        _journalDir = Path.Combine(appData, "journal");
        Directory.CreateDirectory(_journalDir);
    }

    public List<JournalEntry> GetAllEntries()
    {
        var files = Directory.GetFiles(_journalDir, "*.json")
            .OrderByDescending(File.GetLastWriteTime)
            .ToList();

        var entries = new List<JournalEntry>();
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var entry = JsonConvert.DeserializeObject<JournalEntry>(json);
                if (entry != null)
                    entries.Add(entry);
            }
            catch { }
        }
        return entries;
    }

    public JournalEntry? GetEntryForToday()
    {
        return GetAllEntries().FirstOrDefault(e => e.CreatedAt.Date == DateTime.Today);
    }

    public void SaveEntry(JournalEntry entry)
    {
        var filePath = Path.Combine(_journalDir, $"{entry.Id}.json");
        var json = JsonConvert.SerializeObject(entry, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public void DeleteEntry(string id)
    {
        var filePath = Path.Combine(_journalDir, $"{id}.json");
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
