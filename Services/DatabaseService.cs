using Microsoft.Data.Sqlite;
using VentItMaui.Models;
using TaskStatus = VentItMaui.Models.TaskStatus;

namespace VentItMaui.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        var appData = FileSystem.AppDataDirectory;
        var dbPath = Path.Combine(appData, "ventit.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS NotePages (
                Id TEXT PRIMARY KEY, Title TEXT NOT NULL, Content TEXT DEFAULT '',
                Emoji TEXT DEFAULT '📄', ParentId TEXT, CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL, SortOrder INTEGER DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS MindMaps (
                Id TEXT PRIMARY KEY, Title TEXT NOT NULL,
                CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS MindMapNodes (
                Id TEXT PRIMARY KEY, MindMapId TEXT NOT NULL, Text TEXT NOT NULL,
                X REAL NOT NULL, Y REAL NOT NULL, Width REAL DEFAULT 130,
                Height REAL DEFAULT 42, Color TEXT DEFAULT '#7C3AED', ParentNodeId TEXT
            );
            CREATE TABLE IF NOT EXISTS TaskItems (
                Id TEXT PRIMARY KEY, Title TEXT NOT NULL, Description TEXT DEFAULT '',
                Status TEXT DEFAULT 'Todo', Priority TEXT DEFAULT 'Medium',
                DueDate TEXT, CreatedAt TEXT NOT NULL, CompletedAt TEXT
            );
            CREATE TABLE IF NOT EXISTS Routines (
                Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Description TEXT DEFAULT '',
                Emoji TEXT DEFAULT '✅', TimeOfDay TEXT DEFAULT 'Morning',
                DurationMinutes INTEGER DEFAULT 10, Streak INTEGER DEFAULT 0,
                LastCompletedDate TEXT, SortOrder INTEGER DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS ChatMessages (
                Id TEXT PRIMARY KEY, SessionId TEXT NOT NULL, Role TEXT NOT NULL,
                Content TEXT NOT NULL, Timestamp TEXT NOT NULL
            );
        ";
        cmd.ExecuteNonQuery();
    }

    #region NotePages

    public List<NotePage> GetAllPages()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM NotePages ORDER BY SortOrder, CreatedAt";
        var pages = new List<NotePage>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            pages.Add(ReadPage(reader));
        return pages;
    }

    public void SavePage(NotePage page)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO NotePages (Id, Title, Content, Emoji, ParentId, CreatedAt, UpdatedAt, SortOrder)
            VALUES ($id, $title, $content, $emoji, $parentId, $createdAt, $updatedAt, $sortOrder)";
        cmd.Parameters.AddWithValue("$id", page.Id);
        cmd.Parameters.AddWithValue("$title", page.Title);
        cmd.Parameters.AddWithValue("$content", page.Content);
        cmd.Parameters.AddWithValue("$emoji", page.Emoji);
        cmd.Parameters.AddWithValue("$parentId", page.ParentId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$createdAt", page.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("o"));
        cmd.Parameters.AddWithValue("$sortOrder", page.SortOrder);
        cmd.ExecuteNonQuery();
    }

    public void DeletePage(string id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM NotePages WHERE Id = $id OR ParentId = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private static NotePage ReadPage(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        Title = reader.GetString(1),
        Content = reader.GetString(2),
        Emoji = reader.GetString(3),
        ParentId = reader.IsDBNull(4) ? null : reader.GetString(4),
        CreatedAt = DateTime.Parse(reader.GetString(5)),
        UpdatedAt = DateTime.Parse(reader.GetString(6)),
        SortOrder = reader.GetInt32(7)
    };

    #endregion

    #region MindMaps

    public List<MindMap> GetAllMindMaps()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM MindMaps ORDER BY CreatedAt DESC";
        var maps = new List<MindMap>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            maps.Add(new MindMap
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                CreatedAt = DateTime.Parse(reader.GetString(2)),
                UpdatedAt = DateTime.Parse(reader.GetString(3))
            });
        return maps;
    }

    public void SaveMindMap(MindMap map)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO MindMaps (Id, Title, CreatedAt, UpdatedAt)
            VALUES ($id, $title, $createdAt, $updatedAt)";
        cmd.Parameters.AddWithValue("$id", map.Id);
        cmd.Parameters.AddWithValue("$title", map.Title);
        cmd.Parameters.AddWithValue("$createdAt", map.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void DeleteMindMap(string id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM MindMaps WHERE Id = $id; DELETE FROM MindMapNodes WHERE MindMapId = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public List<MindMapNode> GetNodesForMap(string mapId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM MindMapNodes WHERE MindMapId = $mapId";
        cmd.Parameters.AddWithValue("$mapId", mapId);
        var nodes = new List<MindMapNode>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            nodes.Add(ReadMindMapNode(reader));
        return nodes;
    }

    public void SaveNode(MindMapNode node)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO MindMapNodes (Id, MindMapId, Text, X, Y, Width, Height, Color, ParentNodeId)
            VALUES ($id, $mapId, $text, $x, $y, $width, $height, $color, $parentNodeId)";
        cmd.Parameters.AddWithValue("$id", node.Id);
        cmd.Parameters.AddWithValue("$mapId", node.MindMapId);
        cmd.Parameters.AddWithValue("$text", node.Text);
        cmd.Parameters.AddWithValue("$x", node.X);
        cmd.Parameters.AddWithValue("$y", node.Y);
        cmd.Parameters.AddWithValue("$width", node.Width);
        cmd.Parameters.AddWithValue("$height", node.Height);
        cmd.Parameters.AddWithValue("$color", node.Color);
        cmd.Parameters.AddWithValue("$parentNodeId", node.ParentNodeId ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void DeleteNode(string id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM MindMapNodes WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private static MindMapNode ReadMindMapNode(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        MindMapId = reader.GetString(1),
        Text = reader.GetString(2),
        X = reader.GetDouble(3),
        Y = reader.GetDouble(4),
        Width = reader.GetDouble(5),
        Height = reader.GetDouble(6),
        Color = reader.GetString(7),
        ParentNodeId = reader.IsDBNull(8) ? null : reader.GetString(8)
    };

    #endregion

    #region Tasks

    public List<TaskItem> GetAllTasks()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM TaskItems ORDER BY CreatedAt DESC";
        var tasks = new List<TaskItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            tasks.Add(ReadTaskItem(reader));
        return tasks;
    }

    public void SaveTask(TaskItem task)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO TaskItems (Id, Title, Description, Status, Priority, DueDate, CreatedAt, CompletedAt)
            VALUES ($id, $title, $desc, $status, $priority, $dueDate, $createdAt, $completedAt)";
        cmd.Parameters.AddWithValue("$id", task.Id);
        cmd.Parameters.AddWithValue("$title", task.Title);
        cmd.Parameters.AddWithValue("$desc", task.Description);
        cmd.Parameters.AddWithValue("$status", task.Status.ToString());
        cmd.Parameters.AddWithValue("$priority", task.Priority.ToString());
        cmd.Parameters.AddWithValue("$dueDate", task.DueDate?.ToString("o") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$createdAt", task.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$completedAt", task.CompletedAt?.ToString("o") ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void DeleteTask(string id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM TaskItems WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private static TaskItem ReadTaskItem(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        Title = reader.GetString(1),
        Description = reader.GetString(2),
        Status = Enum.Parse<TaskStatus>(reader.GetString(3)),
        Priority = Enum.Parse<TaskPriority>(reader.GetString(4)),
        DueDate = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
        CreatedAt = DateTime.Parse(reader.GetString(6)),
        CompletedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7))
    };

    #endregion

    #region Routines

    public List<Routine> GetAllRoutines()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Routines ORDER BY SortOrder";
        var routines = new List<Routine>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            routines.Add(ReadRoutine(reader));
        return routines;
    }

    public void SaveRoutine(Routine routine)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Routines (Id, Name, Description, Emoji, TimeOfDay, DurationMinutes, Streak, LastCompletedDate, SortOrder)
            VALUES ($id, $name, $desc, $emoji, $timeOfDay, $durationMinutes, $streak, $lastCompletedDate, $sortOrder)";
        cmd.Parameters.AddWithValue("$id", routine.Id);
        cmd.Parameters.AddWithValue("$name", routine.Name);
        cmd.Parameters.AddWithValue("$desc", routine.Description);
        cmd.Parameters.AddWithValue("$emoji", routine.Emoji);
        cmd.Parameters.AddWithValue("$timeOfDay", routine.TimeOfDay);
        cmd.Parameters.AddWithValue("$durationMinutes", routine.DurationMinutes);
        cmd.Parameters.AddWithValue("$streak", routine.Streak);
        cmd.Parameters.AddWithValue("$lastCompletedDate", routine.LastCompletedDate?.ToString("o") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$sortOrder", routine.SortOrder);
        cmd.ExecuteNonQuery();
    }

    public void DeleteRoutine(string id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Routines WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private static Routine ReadRoutine(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        Name = reader.GetString(1),
        Description = reader.GetString(2),
        Emoji = reader.GetString(3),
        TimeOfDay = reader.GetString(4),
        DurationMinutes = reader.GetInt32(5),
        Streak = reader.GetInt32(6),
        LastCompletedDate = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
        SortOrder = reader.GetInt32(8)
    };

    #endregion

    #region Chat

    public List<ChatMessage> GetChatMessages(string sessionId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM ChatMessages WHERE SessionId = $sessionId ORDER BY Timestamp";
        cmd.Parameters.AddWithValue("$sessionId", sessionId);
        var messages = new List<ChatMessage>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            messages.Add(new ChatMessage
            {
                Id = reader.GetString(0),
                SessionId = reader.GetString(1),
                Role = reader.GetString(2),
                Content = reader.GetString(3),
                Timestamp = DateTime.Parse(reader.GetString(4))
            });
        return messages;
    }

    public void SaveChatMessage(ChatMessage message)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO ChatMessages (Id, SessionId, Role, Content, Timestamp)
            VALUES ($id, $sessionId, $role, $content, $timestamp)";
        cmd.Parameters.AddWithValue("$id", message.Id);
        cmd.Parameters.AddWithValue("$sessionId", message.SessionId);
        cmd.Parameters.AddWithValue("$role", message.Role);
        cmd.Parameters.AddWithValue("$content", message.Content);
        cmd.Parameters.AddWithValue("$timestamp", message.Timestamp.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void ClearChatSession(string sessionId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM ChatMessages WHERE SessionId = $sessionId";
        cmd.Parameters.AddWithValue("$sessionId", sessionId);
        cmd.ExecuteNonQuery();
    }

    #endregion
}
