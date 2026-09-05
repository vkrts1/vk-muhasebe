using System;
using System.IO;
using System.Linq;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        try
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlcipher());
            SQLitePCL.Batteries_V2.Init();
            
            // Validate SQLCipher is actually loaded and working
            using (var tempConn = new SQLiteConnection(":memory:"))
            {
                var version = tempConn.ExecuteScalar<string>("PRAGMA cipher_version;");
                Console.WriteLine($"[DbKeyTest] SQLCipher active! Version: {version}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLitePCL Init Exception: {ex.Message}");
        }

        string ermayDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErmayMuhasebe");
        string backupDir = Path.Combine(ermayDir, "Backups");
        string corruptedDir = Path.Combine(ermayDir, "Corrupted");
        
        var testKeys = new List<string?> { 
            null, // Unencrypted
            "ERMAY-SECURE-DB-KEY-2025-V2",
            "ERMAY-SECURE-DB-KEY-2025-V1",
            "ERMAY-SECURE-DB-KEY-2025",
            "ERMAY-SECURE-DB-KEY-2024",
            "ERMAY-SECURE-DB-KEY-V2",
            "ERMAY-SECURE-DB-KEY",
            "123"
        };

        var allFiles = new List<string>();
        
        if (Directory.Exists(ermayDir))
        {
            allFiles.AddRange(Directory.GetFiles(ermayDir, "*.db*")
                .Where(f => !f.EndsWith("-wal") && !f.EndsWith("-shm")));
        }

        if (Directory.Exists(backupDir))
        {
            allFiles.AddRange(Directory.GetFiles(backupDir, "*.db*")
                .Where(f => !f.EndsWith("-wal") && !f.EndsWith("-shm")));
        }

        if (Directory.Exists(corruptedDir))
        {
            allFiles.AddRange(Directory.GetFiles(corruptedDir, "*.db*")
                .Where(f => !f.EndsWith("-wal") && !f.EndsWith("-shm")));
        }

        allFiles = allFiles.Distinct()
            .Where(f => File.Exists(f) && new FileInfo(f).Length > 0)
            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
            .ToList();

        Console.WriteLine($"Testing {allFiles.Count} files with {testKeys.Count} different key options...\n");

        foreach (var dbPath in allFiles)
        {
            var fi = new FileInfo(dbPath);
            bool resolved = false;

            // Define compatibility levels to try
            var compatibilityLevels = new List<int?> { null, 3, 2, 1, 4 };

            foreach (var key in testKeys)
            {
                if (resolved) break;
                
                foreach (var compat in compatibilityLevels)
                {
                    SQLiteAsyncConnection? conn = null;
                    try
                    {
                        var opts = new SQLiteConnectionString(dbPath, SQLiteOpenFlags.ReadOnly | SQLiteOpenFlags.FullMutex, key != null, key: key);
                        conn = new SQLiteAsyncConnection(opts);
                        
                        if (key != null && compat.HasValue)
                        {
                            // Apply compatibility pragma immediately before any table read
                            await conn.ExecuteAsync($"PRAGMA cipher_compatibility = {compat.Value};");
                        }
                        
                        // Force read metadata to trigger decryption
                        var count = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM sqlite_master;");
                        
                        int cariCount = 0, faturaCount = 0, stokCount = 0, hareketCount = 0;
                        try { cariCount = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM CariKart;"); } catch {}
                        try { faturaCount = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM Fatura;"); } catch {}
                        try { stokCount = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM StokKart;"); } catch {}
                        try { hareketCount = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM CariHareket;"); } catch {}
                        
                        string keyDisplay = key == null ? "UNENCRYPTED" : $"'{key}'";
                        string compatDisplay = compat.HasValue ? $"Compat V{compat.Value}" : "Default Compat";
                        Console.WriteLine($"OK  [Key: {keyDisplay,-30}] [Compat: {compatDisplay,-15}] {fi.FullName} | Cari:{cariCount} Fatura:{faturaCount} Stok:{stokCount} CariHareket:{hareketCount}");
                        resolved = true;
                        await conn.CloseAsync();
                        break; // Found working key & compat combo
                    }
                    catch (Exception)
                    {
                        if (conn != null) try { await conn.CloseAsync(); } catch { }
                    }
                }
            }

            if (!resolved)
            {
                Console.WriteLine($"ERR [NO WORKING KEY FOUND          ] {fi.FullName} | Size: {fi.Length} bytes | Date: {fi.LastWriteTime}");
            }
        }
        
        Console.WriteLine("\nDiagnostic completed.");
    }
}

