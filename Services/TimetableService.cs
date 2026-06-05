using Newtonsoft.Json;
using VentItMaui.Models;

namespace VentItMaui.Services;

public class TimetableService
{
    private readonly string _timetablePath;
    private List<TimeBlock> _blocks = new();

    public TimetableService()
    {
        var appData = FileSystem.AppDataDirectory;
        _timetablePath = Path.Combine(appData, "timetable.json");
        LoadBlocks();
    }

    private void LoadBlocks()
    {
        if (File.Exists(_timetablePath))
        {
            try
            {
                var json = File.ReadAllText(_timetablePath);
                _blocks = JsonConvert.DeserializeObject<List<TimeBlock>>(json) ?? new();
            }
            catch
            {
                _blocks = new();
            }
        }
    }

    private void SaveBlocks()
    {
        var json = JsonConvert.SerializeObject(_blocks, Formatting.Indented);
        File.WriteAllText(_timetablePath, json);
    }

    public List<TimeBlock> GetBlocksForDate(DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        return _blocks.Where(b => b.Date == dateStr)
            .OrderBy(b => b.StartHour * 60 + b.StartMinute)
            .ToList();
    }

    public void SaveBlock(TimeBlock block)
    {
        var existing = _blocks.FirstOrDefault(b => b.Id == block.Id);
        if (existing != null)
        {
            _blocks.Remove(existing);
        }
        _blocks.Add(block);
        SaveBlocks();
    }

    public void DeleteBlock(string id)
    {
        _blocks.RemoveAll(b => b.Id == id);
        SaveBlocks();
    }
}
