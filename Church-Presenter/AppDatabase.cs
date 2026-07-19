using Microsoft.Data.Sqlite;
using System.IO;

namespace Church_Presenter;

/// <summary>Owns the portable offline SQLite database placed beside the executable.</summary>
public sealed class AppDatabase
{
    private readonly string _connectionString;
    public AppDatabase()
    {
        // Development builds keep the database in the project root. Published builds use the app folder.
        var folder = FindProjectRoot() ?? AppContext.BaseDirectory;
        _connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(folder, "church-presenter.db"), ForeignKeys = true }.ToString();
    }
    private static string? FindProjectRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
                if (directory.EnumerateFiles("*.csproj").Any()) return directory.FullName;
        return null;
    }
    private SqliteConnection Open() { var c = new SqliteConnection(_connectionString); c.Open(); return c; }
    public void Initialize()
    {
        using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS BibleBooks (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL COLLATE NOCASE UNIQUE, Testament TEXT NOT NULL DEFAULT 'Old', SortOrder INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS BibleVerses (Id INTEGER PRIMARY KEY AUTOINCREMENT, BookId INTEGER NOT NULL REFERENCES BibleBooks(Id) ON DELETE CASCADE, ChapterNumber INTEGER NOT NULL, VerseNumber INTEGER NOT NULL, VerseText TEXT NOT NULL, UNIQUE(BookId, ChapterNumber, VerseNumber));
            CREATE VIRTUAL TABLE IF NOT EXISTS BibleSearch USING fts5(VerseText, content='BibleVerses', content_rowid='Id');
            CREATE TRIGGER IF NOT EXISTS bible_ai AFTER INSERT ON BibleVerses BEGIN INSERT INTO BibleSearch(rowid, VerseText) VALUES (new.Id, new.VerseText); END;
            CREATE TRIGGER IF NOT EXISTS bible_ad AFTER DELETE ON BibleVerses BEGIN INSERT INTO BibleSearch(BibleSearch, rowid, VerseText) VALUES('delete', old.Id, old.VerseText); END;
            CREATE TRIGGER IF NOT EXISTS bible_au AFTER UPDATE ON BibleVerses BEGIN INSERT INTO BibleSearch(BibleSearch, rowid, VerseText) VALUES('delete', old.Id, old.VerseText); INSERT INTO BibleSearch(rowid, VerseText) VALUES (new.Id, new.VerseText); END;
            CREATE TABLE IF NOT EXISTS Settings (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Planners (Id INTEGER PRIMARY KEY AUTOINCREMENT, ServiceDate TEXT NOT NULL, ServiceName TEXT NOT NULL, ThemeName TEXT, UNIQUE(ServiceDate, ServiceName));
            CREATE TABLE IF NOT EXISTS PlannerComponents (Id INTEGER PRIMARY KEY AUTOINCREMENT, PlannerId INTEGER NOT NULL REFERENCES Planners(Id) ON DELETE CASCADE, Position INTEGER NOT NULL, Type TEXT NOT NULL, Title TEXT, Content TEXT);
            CREATE TABLE IF NOT EXISTS Songs (Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL COLLATE NOCASE UNIQUE, Lyrics TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS MediaAssets (Id INTEGER PRIMARY KEY AUTOINCREMENT, Type TEXT NOT NULL, Title TEXT NOT NULL, FilePath TEXT NOT NULL UNIQUE);
            """; cmd.ExecuteNonQuery(); EnsureColumn(c, "PlannerComponents", "PresentationMode", "TEXT NOT NULL DEFAULT 'Static'"); SetIfMissing("BackgroundColor", "#101828"); SetIfMissing("FontColor", "#FFFFFF"); SetIfMissing("FontSize", "48");
    }
    private static void EnsureColumn(SqliteConnection connection, string table, string column, string definition)
    {
        using var check = connection.CreateCommand();
        check.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name=$column";
        check.Parameters.AddWithValue("$column", column);
        if (Convert.ToInt32(check.ExecuteScalar()) == 0)
        {
            using var alter = connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
            alter.ExecuteNonQuery();
        }
    }
    public int GetOrCreatePlanner(DateTime date, string serviceName)
    {
        using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "INSERT INTO Planners(ServiceDate,ServiceName) VALUES($date,$name) ON CONFLICT(ServiceDate,ServiceName) DO NOTHING; SELECT Id FROM Planners WHERE ServiceDate=$date AND ServiceName=$name;"; cmd.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd")); cmd.Parameters.AddWithValue("$name", serviceName.Trim()); return Convert.ToInt32(cmd.ExecuteScalar());
    }
    public IReadOnlyList<Song> GetSongs() { using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT Id,Title,Lyrics FROM Songs ORDER BY Title";using var r=cmd.ExecuteReader();var result=new List<Song>();while(r.Read())result.Add(new Song{Id=r.GetInt32(0),Title=r.GetString(1),Lyrics=r.GetString(2)});return result; }
    public void SaveSong(string title,string lyrics) { using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO Songs(Title,Lyrics) VALUES($title,$lyrics) ON CONFLICT(Title) DO UPDATE SET Lyrics=excluded.Lyrics";cmd.Parameters.AddWithValue("$title",title);cmd.Parameters.AddWithValue("$lyrics",lyrics);cmd.ExecuteNonQuery(); }
    public IReadOnlyList<MediaAsset> GetMediaAssets() { using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT Id,Type,Title,FilePath FROM MediaAssets ORDER BY Type,Title";using var r=cmd.ExecuteReader();var result=new List<MediaAsset>();while(r.Read())result.Add(new MediaAsset{Id=r.GetInt32(0),Type=r.GetString(1),Title=r.GetString(2),FilePath=r.GetString(3)});return result; }
    public void SaveMediaAsset(string type,string title,string filePath) { using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO MediaAssets(Type,Title,FilePath) VALUES($type,$title,$path) ON CONFLICT(FilePath) DO UPDATE SET Type=excluded.Type,Title=excluded.Title";cmd.Parameters.AddWithValue("$type",type);cmd.Parameters.AddWithValue("$title",title);cmd.Parameters.AddWithValue("$path",filePath);cmd.ExecuteNonQuery(); }
    public IReadOnlyList<PlannerComponent> GetPlannerComponents(int plannerId)
    {
        using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT Id,Position,Type,COALESCE(Title,''),COALESCE(Content,''),COALESCE(PresentationMode,'Static') FROM PlannerComponents WHERE PlannerId=$id ORDER BY Position";cmd.Parameters.AddWithValue("$id",plannerId);using var r=cmd.ExecuteReader();var result=new List<PlannerComponent>();while(r.Read())result.Add(new PlannerComponent{Id=r.GetInt32(0),Position=r.GetInt32(1),Type=r.GetString(2),Title=r.GetString(3),Content=r.GetString(4),PresentationMode=r.GetString(5)});return result;
    }
    public void AddPlannerComponent(int plannerId,string type,string title,string content)
    {
        using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO PlannerComponents(PlannerId,Position,Type,Title,Content) VALUES($planner,(SELECT COALESCE(MAX(Position),-1)+1 FROM PlannerComponents WHERE PlannerId=$planner),$type,$title,$content)";cmd.Parameters.AddWithValue("$planner",plannerId);cmd.Parameters.AddWithValue("$type",type);cmd.Parameters.AddWithValue("$title",title);cmd.Parameters.AddWithValue("$content",content);cmd.ExecuteNonQuery();
    }
    public void UpdatePlannerComponent(int id,string title,string content) { using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE PlannerComponents SET Title=$title,Content=$content WHERE Id=$id";cmd.Parameters.AddWithValue("$id",id);cmd.Parameters.AddWithValue("$title",title);cmd.Parameters.AddWithValue("$content",content);cmd.ExecuteNonQuery(); }
    public void SetPlannerPresentationMode(int id,string mode) { using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE PlannerComponents SET PresentationMode=$mode WHERE Id=$id";cmd.Parameters.AddWithValue("$id",id);cmd.Parameters.AddWithValue("$mode",mode);cmd.ExecuteNonQuery(); }
    public void DeletePlannerComponent(int id) { using var c=Open();using var read=c.CreateCommand();read.CommandText="SELECT PlannerId,Position FROM PlannerComponents WHERE Id=$id";read.Parameters.AddWithValue("$id",id);using var r=read.ExecuteReader();if(!r.Read())return;var plannerId=r.GetInt32(0);var position=r.GetInt32(1);r.Close();using var tx=c.BeginTransaction();using var del=c.CreateCommand();del.Transaction=tx;del.CommandText="DELETE FROM PlannerComponents WHERE Id=$id";del.Parameters.AddWithValue("$id",id);del.ExecuteNonQuery();using var fix=c.CreateCommand();fix.Transaction=tx;fix.CommandText="UPDATE PlannerComponents SET Position=Position-1 WHERE PlannerId=$planner AND Position>$position";fix.Parameters.AddWithValue("$planner",plannerId);fix.Parameters.AddWithValue("$position",position);fix.ExecuteNonQuery();tx.Commit(); }
    public void MovePlannerComponent(int plannerId,int id,int direction)
    {
        var items=GetPlannerComponents(plannerId);var index=items.ToList().FindIndex(x=>x.Id==id);var destination=index+direction;if(index<0||destination<0||destination>=items.Count)return;using var c=Open();using var tx=c.BeginTransaction();using var a=c.CreateCommand();a.Transaction=tx;a.CommandText="UPDATE PlannerComponents SET Position=$p WHERE Id=$id";a.Parameters.AddWithValue("$p",items[destination].Position);a.Parameters.AddWithValue("$id",items[index].Id);a.ExecuteNonQuery();using var b=c.CreateCommand();b.Transaction=tx;b.CommandText="UPDATE PlannerComponents SET Position=$p WHERE Id=$id";b.Parameters.AddWithValue("$p",items[index].Position);b.Parameters.AddWithValue("$id",items[destination].Id);b.ExecuteNonQuery();tx.Commit();
    }
    public IReadOnlyList<BibleBook> GetBooks(string? testament = null) { using var c=Open(); using var cmd=c.CreateCommand(); cmd.CommandText="SELECT Id,SortOrder,Name,Testament FROM BibleBooks WHERE $testament IS NULL OR Testament=$testament ORDER BY SortOrder,Name"; cmd.Parameters.AddWithValue("$testament", (object?)testament ?? DBNull.Value); using var r=cmd.ExecuteReader(); var x=new List<BibleBook>(); while(r.Read()) x.Add(new BibleBook{Id=r.GetInt32(0),SortOrder=r.GetInt32(1),Name=r.GetString(2),Testament=r.GetString(3)}); return x; }
    public IReadOnlyList<int> GetChapters(int bookId) { using var c=Open(); using var cmd=c.CreateCommand(); cmd.CommandText="SELECT DISTINCT ChapterNumber FROM BibleVerses WHERE BookId=$id ORDER BY ChapterNumber";cmd.Parameters.AddWithValue("$id",bookId);using var r=cmd.ExecuteReader();var x=new List<int>();while(r.Read())x.Add(r.GetInt32(0));return x; }
    public IReadOnlyList<BibleVerse> GetVerses(int bookId,int chapter) { using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT b.Name,v.BookId,v.ChapterNumber,v.VerseNumber,v.VerseText FROM BibleVerses v JOIN BibleBooks b ON b.Id=v.BookId WHERE v.BookId=$id AND v.ChapterNumber=$chapter ORDER BY v.VerseNumber";cmd.Parameters.AddWithValue("$id",bookId);cmd.Parameters.AddWithValue("$chapter",chapter);return Read(cmd); }
    public IReadOnlyList<BibleVerse> Search(string query) { if(string.IsNullOrWhiteSpace(query))return [];using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT b.Name,v.BookId,v.ChapterNumber,v.VerseNumber,v.VerseText FROM BibleSearch s JOIN BibleVerses v ON v.Id=s.rowid JOIN BibleBooks b ON b.Id=v.BookId WHERE BibleSearch MATCH $q LIMIT 100";cmd.Parameters.AddWithValue("$q",query.Trim().Replace("'"," "));return Read(cmd); }
    private static IReadOnlyList<BibleVerse> Read(SqliteCommand cmd) { using var r=cmd.ExecuteReader();var x=new List<BibleVerse>();while(r.Read()){var book=r.GetString(0);var ch=r.GetInt32(2);var verse=r.GetInt32(3);x.Add(new BibleVerse{BookId=r.GetInt32(1),Chapter=ch,Verse=verse,Text=r.GetString(4),Reference=$"{book} {ch}:{verse}"});}return x; }
    public void ImportBibleCsv(string path) { using var c=Open();using var tx=c.BeginTransaction();var order=0;foreach(var line in File.ReadLines(path).Skip(1)){var p=Parse(line);if(p.Count<5||!int.TryParse(p[2],out var ch)||!int.TryParse(p[3],out var verse))continue;using var b=c.CreateCommand();b.Transaction=tx;b.CommandText="INSERT INTO BibleBooks(Name,Testament,SortOrder) VALUES($n,$t,$o) ON CONFLICT(Name) DO NOTHING; SELECT Id FROM BibleBooks WHERE Name=$n;";b.Parameters.AddWithValue("$n",p[0]);b.Parameters.AddWithValue("$t",string.IsNullOrWhiteSpace(p[1])?"Old":p[1]);b.Parameters.AddWithValue("$o",++order);var id=Convert.ToInt32(b.ExecuteScalar());using var v=c.CreateCommand();v.Transaction=tx;v.CommandText="INSERT INTO BibleVerses(BookId,ChapterNumber,VerseNumber,VerseText) VALUES($b,$c,$v,$t) ON CONFLICT(BookId,ChapterNumber,VerseNumber) DO UPDATE SET VerseText=excluded.VerseText";v.Parameters.AddWithValue("$b",id);v.Parameters.AddWithValue("$c",ch);v.Parameters.AddWithValue("$v",verse);v.Parameters.AddWithValue("$t",p[4]);v.ExecuteNonQuery();}tx.Commit(); }
    public void SaveBibleVerse(string testament, string bookName, int chapter, int verse, string text)
    {
        using var c=Open();using var tx=c.BeginTransaction();using var book=c.CreateCommand();book.Transaction=tx;book.CommandText="INSERT INTO BibleBooks(Name,Testament,SortOrder) VALUES($name,$testament,(SELECT COALESCE(MAX(SortOrder),0)+1 FROM BibleBooks)) ON CONFLICT(Name) DO UPDATE SET Testament=excluded.Testament; SELECT Id FROM BibleBooks WHERE Name=$name;";book.Parameters.AddWithValue("$name",bookName.Trim());book.Parameters.AddWithValue("$testament",testament);var bookId=Convert.ToInt32(book.ExecuteScalar());using var entry=c.CreateCommand();entry.Transaction=tx;entry.CommandText="INSERT INTO BibleVerses(BookId,ChapterNumber,VerseNumber,VerseText) VALUES($book,$chapter,$verse,$text) ON CONFLICT(BookId,ChapterNumber,VerseNumber) DO UPDATE SET VerseText=excluded.VerseText";entry.Parameters.AddWithValue("$book",bookId);entry.Parameters.AddWithValue("$chapter",chapter);entry.Parameters.AddWithValue("$verse",verse);entry.Parameters.AddWithValue("$text",text);entry.ExecuteNonQuery();tx.Commit();
    }
    public string Get(string key,string fallback){using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT Value FROM Settings WHERE Key=$k";cmd.Parameters.AddWithValue("$k",key);return cmd.ExecuteScalar() as string??fallback;} public void Set(string key,string value){using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO Settings(Key,Value) VALUES($k,$v) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value";cmd.Parameters.AddWithValue("$k",key);cmd.Parameters.AddWithValue("$v",value);cmd.ExecuteNonQuery();} private void SetIfMissing(string key,string value){using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO Settings(Key,Value) VALUES($k,$v) ON CONFLICT(Key) DO NOTHING";cmd.Parameters.AddWithValue("$k",key);cmd.Parameters.AddWithValue("$v",value);cmd.ExecuteNonQuery();}
    private static List<string> Parse(string s){var r=new List<string>();var b=new System.Text.StringBuilder();var q=false;foreach(var x in s){if(x=='\"')q=!q;else if(x==','&&!q){r.Add(b.ToString().Trim());b.Clear();}else b.Append(x);}r.Add(b.ToString().Trim());return r;}
}
