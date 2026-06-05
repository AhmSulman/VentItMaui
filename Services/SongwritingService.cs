using Newtonsoft.Json;
using VentItMaui.Models;

namespace VentItMaui.Services;

public class SongwritingService
{
    private readonly string _songsDir;

    public SongwritingService()
    {
        var appData = FileSystem.AppDataDirectory;
        _songsDir = Path.Combine(appData, "songs");
        Directory.CreateDirectory(_songsDir);
    }

    public List<Song> GetAllSongs()
    {
        var files = Directory.GetFiles(_songsDir, "*.json")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .ToList();

        var songs = new List<Song>();
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var song = JsonConvert.DeserializeObject<Song>(json);
                if (song != null)
                    songs.Add(song);
            }
            catch { }
        }
        return songs;
    }

    public Song? GetSongById(string id)
    {
        var filePath = Path.Combine(_songsDir, $"{id}.json");
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Song>(json);
        }
        catch
        {
            return null;
        }
    }

    public void SaveSong(Song song)
    {
        song.UpdatedAt = DateTime.UtcNow;
        var filePath = Path.Combine(_songsDir, $"{song.Id}.json");
        var json = JsonConvert.SerializeObject(song, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public void DeleteSong(Song song)
    {
        var filePath = Path.Combine(_songsDir, $"{song.Id}.json");
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public List<Song> SearchSongs(string query)
    {
        var songs = GetAllSongs();
        var lower = query.ToLower();
        return songs.Where(s =>
            s.Title.ToLower().Contains(lower) ||
            s.Artist.ToLower().Contains(lower) ||
            s.Genre.ToLower().Contains(lower) ||
            s.Mood.ToLower().Contains(lower) ||
            s.Tags.Any(t => t.ToLower().Contains(lower))
        ).ToList();
    }
}
