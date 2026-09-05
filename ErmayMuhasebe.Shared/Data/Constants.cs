namespace ErmayMuhasebe.Data;

public static class Constants
{
    public const string DatabaseFilename = "ErmayV4_Stable.db3";
    public const string DatabasePassword = "ERMAY-SECURE-DB-KEY-2025-V2"; 


    public const SQLite.SQLiteOpenFlags Flags =
        SQLite.SQLiteOpenFlags.ReadWrite |
        SQLite.SQLiteOpenFlags.Create;

    private static string? _databasePath;
    public static string DatabasePath
    {
        get
        {
            if (_databasePath != null) return _databasePath;
            
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErmayMuhasebe");
            if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
            
            return Path.Combine(appData, DatabaseFilename);
        }
        set => _databasePath = value;
    }
}
