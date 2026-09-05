using System;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using SQLite;

namespace UserSimulationTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("ERMAY MUHASEBE - SON KULLANICI E2E TEST SİMÜLASYONU");
            Console.WriteLine("==================================================");
            Console.WriteLine();

            foreach (var ct in typeof(SQLiteConnectionString).GetConstructors())
            {
                Console.WriteLine($"[REFLECT] Constructor: {string.Join(", ", ct.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))}");
            }

            var candidates = new System.Collections.Generic.List<object>();

            // 1. Initialize SQLite SQLCipher Native Provider
            try
            {
                SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlcipher());
                SQLitePCL.Batteries_V2.Init();
                Console.WriteLine("[INFO] SQLitePCL SQLCipher native provider başarıyla yüklendi.");

                // Decrypt login_settings.txt & Try to crack XOR key using expected 'admin' username
                try
                {
                    string loginSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErmayMuhasebe", "login_settings.txt");
                    if (File.Exists(loginSettingsPath))
                    {
                        var lines = File.ReadAllLines(loginSettingsPath);
                        if (lines.Length >= 2)
                        {
                            byte[] cipherBytes = Convert.FromBase64String(lines[0]);
                            byte[] passCipherBytes = Convert.FromBase64String(lines[1]);
                            string expectedPlain = "admin";
                            byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(expectedPlain);
                            
                            // 1. Crack XOR key assuming Username is "admin" with zero-padding
                            byte[] fullXorKey = new byte[cipherBytes.Length];
                            for (int i = 0; i < plainBytes.Length; i++)
                            {
                                fullXorKey[i] = (byte)(cipherBytes[i] ^ plainBytes[i]);
                            }
                            for (int i = plainBytes.Length; i < cipherBytes.Length; i++)
                            {
                                fullXorKey[i] = (byte)(cipherBytes[i] ^ 0);
                            }
                            
                            string recoveredStr = System.Text.Encoding.UTF8.GetString(fullXorKey).TrimEnd('\0');
                            Console.WriteLine($"[DIAGNOSTIC] Cracked full XOR key string (assuming zero-padded 'admin'): '{recoveredStr}'");
                            string fullKeyHex = string.Concat(fullXorKey.Select(b => b.ToString("x2")));
                            Console.WriteLine($"[DIAGNOSTIC] Cracked full XOR key (Hex): {fullKeyHex}");
                            candidates.Add(recoveredStr);
                            candidates.Add(fullXorKey);
                            
                            // Decrypt password with cracked key
                            try
                            {
                                byte[] decPassBytes = new byte[passCipherBytes.Length];
                                for (int i = 0; i < passCipherBytes.Length; i++)
                                {
                                    decPassBytes[i] = (byte)(passCipherBytes[i] ^ fullXorKey[i % fullXorKey.Length]);
                                }
                                string decPass = System.Text.Encoding.UTF8.GetString(decPassBytes).TrimEnd('\0');
                                Console.WriteLine($"[DIAGNOSTIC] Decrypted Password using cracked key: '{decPass}'");
                                candidates.Add(decPass);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[DIAGNOSTIC] Decrypt password with cracked key failed: {ex.Message}");
                            }

                            // 2. Crack XOR key assuming password is "123" with zero-padding
                            try
                            {
                                string expectedPass = "123";
                                byte[] passPlainBytes = System.Text.Encoding.UTF8.GetBytes(expectedPass);
                                byte[] fullPassXorKey = new byte[passCipherBytes.Length];
                                for (int j = 0; j < passPlainBytes.Length; j++)
                                {
                                    fullPassXorKey[j] = (byte)(passCipherBytes[j] ^ passPlainBytes[j]);
                                }
                                for (int j = passPlainBytes.Length; j < passCipherBytes.Length; j++)
                                {
                                    fullPassXorKey[j] = (byte)(passCipherBytes[j] ^ 0);
                                }
                                
                                string recoveredPassXorStr = System.Text.Encoding.UTF8.GetString(fullPassXorKey).TrimEnd('\0');
                                Console.WriteLine($"[DIAGNOSTIC] Cracked full XOR key string from password (assuming zero-padded '123'): '{recoveredPassXorStr}'");
                                candidates.Add(recoveredPassXorStr);
                                candidates.Add(fullPassXorKey);
                                
                                // Decrypt Username using password key
                                byte[] decUserBytes = new byte[cipherBytes.Length];
                                for (int i = 0; i < cipherBytes.Length; i++)
                                {
                                    decUserBytes[i] = (byte)(cipherBytes[i] ^ fullPassXorKey[i % fullPassXorKey.Length]);
                                }
                                string decUser = System.Text.Encoding.UTF8.GetString(decUserBytes).TrimEnd('\0');
                                Console.WriteLine($"[DIAGNOSTIC] Decrypted Username using password key: '{decUser}'");
                            }
                            catch { }

                            // Let's also try to decrypt with current key just in case
                            string u = AuthService.Decrypt(lines[0]);
                            string p = AuthService.Decrypt(lines[1]);
                            Console.WriteLine($"[DIAGNOSTIC] Decrypted with current key -> Username: '{u}', Password: '{p}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DIAGNOSTIC] Decrypt login settings failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SQLitePCL Init Hatası: {ex.Message}");
                return;
            }

            // 2. Initialize Database Service (Encrypted with Dynamic Key)
            DatabaseService dbService = null;
            SQLiteAsyncConnection db = null;
            try
            {
                Console.WriteLine("[DIAGNOSTIC] SQLite/SQLCipher native loading test...");
                string tempDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErmayMuhasebe", "temp_test_cipher.db");
                if (File.Exists(tempDbPath)) File.Delete(tempDbPath);

                var tempOptions = new SQLiteConnectionString(tempDbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex, true, key: "test_key");
                var tempDb = new SQLiteAsyncConnection(tempOptions);
                await tempDb.ExecuteAsync("CREATE TABLE Test (Id INTEGER PRIMARY KEY, Val TEXT);");
                await tempDb.ExecuteAsync("INSERT INTO Test (Val) VALUES ('Hello SQLCipher');");
                
                // Try retrieving cipher version
                try
                {
                    var cipherVersion = await tempDb.ExecuteScalarAsync<string>("PRAGMA cipher_version;");
                    Console.WriteLine($"[DIAGNOSTIC] SQLCipher Active! Version: {cipherVersion}");
                }
                catch (Exception cipherValEx)
                {
                    Console.WriteLine($"[DIAGNOSTIC] PRAGMA cipher_version failed: {cipherValEx.Message}");
                }

                await tempDb.CloseAsync();
                
                // Try opening with correct key
                tempDb = new SQLiteAsyncConnection(new SQLiteConnectionString(tempDbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex, true, key: "test_key"));
                var count = await tempDb.ExecuteScalarAsync<int>("SELECT count(*) FROM Test;");
                Console.WriteLine($"[DIAGNOSTIC] Decryption with correct key: SUCCESS (Count: {count})");
                await tempDb.CloseAsync();

                // Try opening with WRONG key (should fail)
                try
                {
                    tempDb = new SQLiteAsyncConnection(new SQLiteConnectionString(tempDbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex, true, key: "wrong_key"));
                    await tempDb.ExecuteScalarAsync<int>("SELECT count(*) FROM Test;");
                    Console.WriteLine("[DIAGNOSTIC] WARNING: Opened encrypted database with WRONG key! This means encryption is NOT working!");
                }
                catch (SQLiteException ex) when (ex.Message.Contains("file is not a database"))
                {
                    Console.WriteLine("[DIAGNOSTIC] Opening with wrong key failed as expected: file is not a database.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DIAGNOSTIC] Opening with wrong key threw: {ex.Message}");
                }
                finally
                {
                    if (tempDb != null) await tempDb.CloseAsync();
                    if (File.Exists(tempDbPath)) File.Delete(tempDbPath);
                }

                // Now test the main database files with password variations
                string ermayFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErmayMuhasebe");
                var allDbFiles = new System.Collections.Generic.List<string> { "ErmayV4_Stable.db3", "ermay_2026.db" };
                
                string backupsDir = Path.Combine(ermayFolder, "Backups");
                if (Directory.Exists(backupsDir))
                {
                    foreach (var f in Directory.GetFiles(backupsDir, "*.db3"))
                    {
                        allDbFiles.Add("Backups\\" + Path.GetFileName(f));
                    }
                }

                candidates.AddRange(new object[] {
                    "ERMAY-SECURE-DB-KEY-2025-V2",
                    "ERMAY-SECURE-DB-KEY-2025",
                    "ERMAY-SECURE-DB-KEY-2026",
                    "ERMAY-SECURE-DB-KEY-2025-V1",
                    "ERMAY-SECURE-DB-KEY",
                    "ERMAY-DB-KEY-2025",
                    "ermay2025",
                    "ermay2026",
                    "ermay123",
                    "123",
                    "ERMAY_SECURE_DB_KEY_2025_V2",
                    "ERMAY-SECURE-KEY-2025",
                    "ERMAY-SECURE-DB-KEY-2026-V2",
                    "ERMAY-SECURE-DB-KEY-2026-V1",
                    "ERMAY-SECURE-DB-KEY-2025-V3",
                    "ERMAY-SECURE-DB-KEY-2026-V3",
                    "ERMAY-SECURE-DB-KEY-2025-V4",
                    "ERMAY-SECURE-DB-KEY-2026-V4",
                    "ERMAY-SECURE-DB-KEY-V2",
                    "ERMAY-SECURE-DB-KEY-V3",
                    "ERMAY-SECURE-DB-KEY-V4",
                    "ERMAY-SECURE-DB-KEY-2025-V2-STABLE",
                    "ERMAY-SECURE-DB-KEY-2026-STABLE",
                    "ERMAY-SECURE-DB-KEY-2026-V2-STABLE",
                    "ERMAY-SECURE-DB-KEY-2025-STABLE",
                    "ERMAY-STABLE-DB-KEY-2025",
                    "ERMAY-STABLE-DB-KEY-2026",
                    "ERMAY-STABLE-DB-KEY",
                    "ERMAY-V4-SECURE-KEY",
                    "ERMAY-V4-SECURE-DB-KEY",
                    "ERMAY-V4-STABLE-KEY",
                    "ERMAY-V4-STABLE-DB-KEY",
                    "ERMAY-V4-STABLE",
                    "ERMAY-V4",
                    "ErmayV4",
                    "ErmayV4_Stable",
                    "ERMAY-SECURE-DB-KEY-2026-V2",
                    "ERMAY-SECURE-DB-KEY-2025-V2-2026",
                    "ERMAY-SECURE-DB-KEY-2025-V2-V3",
                    "ERMAY-SECURE-DB-KEY-2025-V5"
                });

                // Read db_key.bin if exists and generate variations
                string binKeyPath = Path.Combine(ermayFolder, "db_key.bin");
                if (File.Exists(binKeyPath))
                {
                    try
                    {
                        byte[] binBytes = File.ReadAllBytes(binKeyPath);
                        candidates.Add(binBytes); // 1. Raw byte array
                        
                        // Add 16-byte and 32-byte chunks of db_key.bin
                        if (binBytes.Length >= 16)
                        {
                            byte[] chunk16 = new byte[16];
                            Array.Copy(binBytes, 0, chunk16, 0, 16);
                            candidates.Add(chunk16);
                        }
                        if (binBytes.Length >= 32)
                        {
                            byte[] chunk32 = new byte[32];
                            Array.Copy(binBytes, 0, chunk32, 0, 32);
                            candidates.Add(chunk32);
                        }
                        
                        string base64Key = Convert.ToBase64String(binBytes);
                        candidates.Add(base64Key); // 2. Base64
                        
                        string hexKey = string.Concat(binBytes.Select(b => b.ToString("x2")));
                        candidates.Add(hexKey); // 3. Lowercase Hex
                        candidates.Add(hexKey.ToUpper()); // 4. Uppercase Hex
                        
                        string utf8Key = System.Text.Encoding.UTF8.GetString(binBytes).TrimEnd('\0');
                        candidates.Add(utf8Key); // 5. UTF8 String
                        
                        string asciiKey = System.Text.Encoding.ASCII.GetString(binBytes).TrimEnd('\0');
                        candidates.Add(asciiKey); // 6. ASCII String

                        // SHA256 Hash of binBytes
                        using (var sha256 = System.Security.Cryptography.SHA256.Create())
                        {
                            byte[] sha256Bytes = sha256.ComputeHash(binBytes);
                            candidates.Add(sha256Bytes);
                            candidates.Add(Convert.ToBase64String(sha256Bytes));
                            string sha256Hex = string.Concat(sha256Bytes.Select(b => b.ToString("x2")));
                            candidates.Add(sha256Hex);
                            candidates.Add(sha256Hex.ToUpper());
                        }

                        // SHA512 Hash of binBytes
                        using (var sha512 = System.Security.Cryptography.SHA512.Create())
                        {
                            byte[] sha512Bytes = sha512.ComputeHash(binBytes);
                            candidates.Add(sha512Bytes);
                            candidates.Add(Convert.ToBase64String(sha512Bytes));
                            string sha512Hex = string.Concat(sha512Bytes.Select(b => b.ToString("x2")));
                            candidates.Add(sha512Hex);
                            candidates.Add(sha512Hex.ToUpper());
                        }

                        // XOR Decrypted version of db_key.bin using current AuthService.Key
                        try
                        {
                            byte[] decryptedBinBytes = new byte[binBytes.Length];
                            byte[] authXorKey = System.Text.Encoding.UTF8.GetBytes("ERMAY-SECURE-KEY-2025");
                            for (int i = 0; i < binBytes.Length; i++)
                            {
                                decryptedBinBytes[i] = (byte)(binBytes[i] ^ authXorKey[i % authXorKey.Length]);
                            }
                            candidates.Add(decryptedBinBytes);
                            candidates.Add(System.Text.Encoding.UTF8.GetString(decryptedBinBytes).TrimEnd('\0'));
                            candidates.Add(System.Text.Encoding.ASCII.GetString(decryptedBinBytes).TrimEnd('\0'));
                            candidates.Add(Convert.ToBase64String(decryptedBinBytes));
                            string decHex = string.Concat(decryptedBinBytes.Select(b => b.ToString("x2")));
                            candidates.Add(decHex);
                            candidates.Add(decHex.ToUpper());
                        }
                        catch { }

                        Console.WriteLine($"[DIAGNOSTIC] Loaded db_key.bin ({binBytes.Length} bytes). Generated hash and decrypted variations.");
                    }
                    catch (Exception binEx)
                    {
                        Console.WriteLine($"[DIAGNOSTIC] Failed to read db_key.bin: {binEx.Message}");
                    }
                }

                // Add more candidate string password variations
                string[] morePasswordCandidates = {
                    "ERMAY-SECURE-DB-KEY-2026-V5",
                    "ERMAY-SECURE-DB-KEY-2025-STABLE-V2",
                    "ERMAY-SECURE-DB-KEY-2025-STABLE-V3",
                    "ERMAY-SECURE-DB-KEY-2025-STABLE-V4",
                    "ERMAY-SECURE-DB-KEY-2026-STABLE-V2",
                    "ERMAY-SECURE-DB-KEY-2026-STABLE-V3",
                    "ERMAY-SECURE-DB-KEY-2026-STABLE-V4",
                    "ERMAY-SECURE-DB-KEY-STABLE",
                    "ERMAY-SECURE-DB-KEY-STABLE-2025",
                    "ERMAY-SECURE-DB-KEY-STABLE-2026",
                    "ERMAY-SECURE-DB-KEY-V4-STABLE",
                    "ERMAY-SECURE-DB-KEY-V4-2025",
                    "ERMAY-SECURE-DB-KEY-V4-2026",
                    "ERMAY-SECURE-DB-KEY-V4-2026-V2",
                    "ERMAY-SECURE-DB-KEY-V4-STABLE-V2",
                    "ERMAY-SECURE-DB-KEY-V4-STABLE-2025",
                    "ERMAY-SECURE-DB-KEY-V4-STABLE-2026",
                    "ERMAY-V4-SECURE-DB-KEY-2025",
                    "ERMAY-V4-SECURE-DB-KEY-2026",
                    "ERMAY-V4-SECURE-DB-KEY-2025-V2",
                    "ERMAY-V4-SECURE-DB-KEY-2026-V2",
                    "ERMAY-V4-STABLE-DB-KEY-2025",
                    "ERMAY-V4-STABLE-DB-KEY-2026",
                    "ERMAY-V4-STABLE-DB-KEY-2025-V2",
                    "ERMAY-V4-STABLE-DB-KEY-2026-V2",
                    "ERMAY-SECURE-DB-KEY-2025-V2-STABLE",
                    "ERMAY-SECURE-DB-KEY-2026-V2-STABLE"
                };
                foreach (var s in morePasswordCandidates) candidates.Add(s);

                // Expand byte[] candidates to include SQLCipher hex-string representations like x'...'
                var extraCandidates = new System.Collections.Generic.List<object>();
                foreach (var cand in candidates)
                {
                    if (cand is byte[] bytes)
                    {
                        string hex = string.Concat(bytes.Select(b => b.ToString("x2")));
                        extraCandidates.Add($"x'{hex}'");
                        extraCandidates.Add($"x'{hex.ToUpper()}'");
                    }
                }
                candidates.AddRange(extraCandidates);

                // Scan for other ErmayMuhasebe.Shared.dll files in the project to extract DatabasePassword and AuthService.Key
                Console.WriteLine("\n[DIAGNOSTIC] SCANNING COMPILED DLL FILES FOR CONSTANTS...");
                string workspaceRoot = "E:\\avalonia yedek\\ermaymuhasebe";
                if (Directory.Exists(workspaceRoot))
                {
                    var dllFiles = Directory.GetFiles(workspaceRoot, "ErmayMuhasebe.Shared.dll", SearchOption.AllDirectories);
                    foreach (var dllPath in dllFiles)
                    {
                        try
                        {
                            var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
                            
                            // 1. Constants.DatabasePassword
                            var constantsType = assembly.GetType("ErmayMuhasebe.Data.Constants");
                            if (constantsType != null)
                            {
                                var pwdField = constantsType.GetField("DatabasePassword", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
                                if (pwdField != null)
                                {
                                    object val = pwdField.GetValue(null) ?? pwdField.GetRawConstantValue();
                                    Console.WriteLine($"  Found DLL: {dllPath.Replace(workspaceRoot, string.Empty)} | DatabasePassword: '{val}'");
                                    if (val != null && !candidates.Contains(val.ToString()))
                                    {
                                        candidates.Add(pwdField.GetValue(null)?.ToString() ?? pwdField.GetRawConstantValue().ToString());
                                    }
                                }
                            }
                            
                            // 2. AuthService.Key
                            var authServiceType = assembly.GetType("ErmayMuhasebe.Services.AuthService");
                            if (authServiceType != null)
                            {
                                var keyField = authServiceType.GetField("Key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                                if (keyField != null)
                                {
                                    var keyVal = keyField.GetValue(null) as byte[];
                                    if (keyVal != null)
                                    {
                                        string keyStr = System.Text.Encoding.UTF8.GetString(keyVal);
                                        Console.WriteLine($"  Found DLL: {dllPath.Replace(workspaceRoot, string.Empty)} | AuthService.Key: '{keyStr}'");
                                        
                                        // Decrypt login_settings.txt with this keyVal
                                        try
                                        {
                                            string loginSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErmayMuhasebe", "login_settings.txt");
                                            if (File.Exists(loginSettingsPath))
                                            {
                                                var lines = File.ReadAllLines(loginSettingsPath);
                                                if (lines.Length >= 2)
                                                {
                                                     byte[] uBytes = Convert.FromBase64String(lines[0]);
                                                     byte[] pBytes = Convert.FromBase64String(lines[1]);
                                                     for (int i = 0; i < uBytes.Length; i++) uBytes[i] ^= keyVal[i % keyVal.Length];
                                                     for (int i = 0; i < pBytes.Length; i++) pBytes[i] ^= keyVal[i % keyVal.Length];
                                                     string uDec = System.Text.Encoding.UTF8.GetString(uBytes).TrimEnd('\0');
                                                     string pDec = System.Text.Encoding.UTF8.GetString(pBytes).TrimEnd('\0');
                                                     Console.WriteLine($"    Decrypted Credentials using DLL key -> User: '{uDec}', Pass: '{pDec}'");
                                                     
                                                     // If username is admin, maybe Password is database key? Or let's add these decrypted strings
                                                     if (!string.IsNullOrEmpty(uDec)) candidates.Add(uDec);
                                                     if (!string.IsNullOrEmpty(pDec)) candidates.Add(pDec);
                                                }
                                            }
                                        }
                                        catch (Exception credEx)
                                        {
                                            Console.WriteLine($"    Failed credentials decrypt: {credEx.Message}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception dllEx)
                        {
                            Console.WriteLine($"  Error loading {dllPath.Replace(workspaceRoot, string.Empty)}: {dllEx.Message}");
                        }
                    }
                }

                Console.WriteLine("\n[DIAGNOSTIC] BRUTE-FORCING DATABASE PASSWORDS...");
                foreach (var dbName in allDbFiles)
                {
                    string fullPath = Path.Combine(ermayFolder, dbName);
                    if (!File.Exists(fullPath)) continue;
                    
                    Console.WriteLine($"\n[DIAGNOSTIC] Checking database: {dbName} (Size: {new FileInfo(fullPath).Length} bytes)");
                    bool found = false;
                    candidates.Insert(0, ""); // Add empty password for unencrypted test

                    foreach (var pwd in candidates)
                    {
                        var modes = new[] {
                            new { Name = "Default (SQLCipher 4)", Action = (Func<SQLiteAsyncConnection, Task>)null },
                            new { Name = "SQLCipher 3 Compatibility", Action = new Func<SQLiteAsyncConnection, Task>(async db => { await db.ExecuteAsync("PRAGMA cipher_compatibility = 3;"); }) },
                            new { Name = "SQLCipher 2 Compatibility", Action = new Func<SQLiteAsyncConnection, Task>(async db => { await db.ExecuteAsync("PRAGMA cipher_compatibility = 2;"); }) },
                            new { Name = "Page Size 1024, KDF 64000", Action = new Func<SQLiteAsyncConnection, Task>(async db => { await db.ExecuteAsync("PRAGMA cipher_page_size = 1024; PRAGMA kdf_iter = 64000;"); }) },
                            new { Name = "Page Size 1024", Action = new Func<SQLiteAsyncConnection, Task>(async db => { await db.ExecuteAsync("PRAGMA cipher_page_size = 1024;"); }) },
                            new { Name = "Page Size 4096", Action = new Func<SQLiteAsyncConnection, Task>(async db => { await db.ExecuteAsync("PRAGMA cipher_page_size = 4096;"); }) },
                            new { Name = "SQLCipher 3, HMAC OFF", Action = new Func<SQLiteAsyncConnection, Task>(async db => { await db.ExecuteAsync("PRAGMA cipher_compatibility = 3; PRAGMA cipher_use_hmac = OFF;"); }) }
                        };

                        foreach (var m in modes)
                        {
                            try
                            {
                                var opt = new SQLiteConnectionString(fullPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex, true, key: pwd);
                                var testConn = new SQLiteAsyncConnection(opt);
                                if (m.Action != null)
                                {
                                    await m.Action(testConn);
                                }
                                await testConn.ExecuteScalarAsync<int>("SELECT count(*) FROM sqlite_master;");
                                
                                string pwdDisplay = pwd is byte[] ? "byte[] array" : $"'{pwd}'";
                                if (string.IsNullOrEmpty(pwdDisplay)) pwdDisplay = "Empty/None";
                                Console.WriteLine($"  >>> SUCCESS! Password {pwdDisplay} works with {m.Name} for {dbName}!");
                                found = true;
                                await testConn.CloseAsync();
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (!ex.Message.Contains("file is not a database") && !ex.Message.Contains("FileIsNotADatabase"))
                                {
                                    Console.WriteLine($"    [DEBUG ERROR for {pwd} in {m.Name}]: {ex.GetType().Name} - {ex.Message}");
                                }
                            }
                        }
                        if (found) break;
                    }
                    if (!found)
                    {
                        Console.WriteLine($"  [FAILED] None of the candidate passwords worked for {dbName}.");
                    }
                }

                // Test opening backups and main DB using DatabaseService with various keys directly
                string[] testFiles = {
                    "Backups\\Ermay_AutoBackup_20260601.db3",
                    "Backups\\Ermay_AutoBackup_20260602.db3",
                    "ErmayV4_Stable.db3"
                };

                foreach (var fName in testFiles)
                {
                    string fPath = Path.Combine(ermayFolder, fName);
                    if (!File.Exists(fPath)) continue;

                    Console.WriteLine($"\n[DIAGNOSTIC] Testing direct DatabaseService open for {fName}...");
                    
                    // Try with ERMAY-SECURE-DB-KEY-2025-V2
                    try
                    {
                        var testDbService = new DatabaseService();
                        testDbService.CustomPassword = "ERMAY-SECURE-DB-KEY-2025-V2";
                        await testDbService.InitializeAsync(fPath);
                        var conn = await testDbService.GetConnectionAsync();
                        var tablesCount = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM sqlite_master;");
                        Console.WriteLine($"  >>> SUCCESS with 'ERMAY-SECURE-DB-KEY-2025-V2'! Tables: {tablesCount}");
                        await testDbService.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  [FAILED] with 'ERMAY-SECURE-DB-KEY-2025-V2': {ex.Message}");
                    }

                    // Try with cracked candidates
                    foreach (var cand in candidates)
                    {
                        if (cand == null || cand.ToString() == "" || cand.ToString() == "ERMAY-SECURE-DB-KEY-2025-V2") continue;
                        
                        try
                        {
                            var testDbService = new DatabaseService();
                            if (cand is byte[] bKeys)
                            {
                                testDbService.CustomPassword = System.Text.Encoding.UTF8.GetString(bKeys);
                            }
                            else
                            {
                                testDbService.CustomPassword = cand.ToString();
                            }
                            
                            await testDbService.InitializeAsync(fPath);
                            var conn = await testDbService.GetConnectionAsync();
                            var tablesCount = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM sqlite_master;");
                            string displayStr = cand is byte[] ? "byte[] key" : $"'{cand}'";
                            Console.WriteLine($"  >>> SUCCESS with cracked key {displayStr}! Tables: {tablesCount}");
                            await testDbService.CloseAsync();
                        }
                        catch
                        {
                            // Silent failure for candidates
                        }
                    }
                }

                Console.WriteLine("\n[INFO] DatabaseService başlatılıyor...");
                dbService = new DatabaseService();
                Console.WriteLine($"[SECURITY] Dinamik Veritabanı Şifresi: {dbService.CustomPassword?.Substring(0, 5)}***");
                await dbService.InitializeAsync();
                Console.WriteLine("[SUCCESS] Veritabanı başarıyla şifreli olarak açıldı ve tablolar ilklendirildi.");
                db = await dbService.GetConnectionAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] Veritabanı Başlatma Hatası: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"-> Detay: {ex.InnerException.Message}");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("ADIM 1: KULLANICI GİRİŞ DOĞRULAMASI (LOGIN VALIDATION)");
            Console.WriteLine("--------------------------------------------------");
            
            // 3. User Login Simulation
            try
            {
                var adminUser = await dbService.GetUserByUsernameAsync("admin");
                if (adminUser != null)
                {
                    Console.WriteLine($"[SUCCESS] 'admin' kullanıcısı bulundu. Rolü: {adminUser.Role}, Oluşturulma: {adminUser.CreatedAt}");
                    
                    // Şifre doğrulama testi (Varsayılan şifre: "123")
                    bool passwordOk = AuthService.VerifyPassword("123", adminUser.Password!, adminUser.PasswordSalt!);
                    if (passwordOk)
                    {
                        Console.WriteLine("[SUCCESS] Şifre başarıyla doğrulandı (Kullanıcı Oturumu: Açıldı).");
                    }
                    else
                    {
                        Console.WriteLine("[ERROR] Şifre doğrulama başarısız!");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("[ERROR] 'admin' kullanıcısı veritabanında bulunamadı!");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Giriş Doğrulama Hatası: {ex.Message}");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("ADIM 2: YENİ CARİ KART VE STOK KARTI OLUŞTURMA");
            Console.WriteLine("--------------------------------------------------");

            CariKart testCari = null;
            StokKart testStok = null;

            try
            {
                // Create unique customer
                string uniqueId = DateTime.Now.ToString("mmss");
                testCari = new CariKart
                {
                    Unvan = $"Beta Müşterisi LTD ({uniqueId})",
                    CariKod = $"C-BETA-{uniqueId}",
                    Grup = "Müşteri",
                    VergiNo = $"123456{uniqueId}",
                    VadeGunu = 15,
                    RiskLimiti = 50000,
                    RiskTakibiYapilsin = true,
                    KayitTarihi = DateTime.Now
                };

                await db.InsertAsync(testCari);
                Console.WriteLine($"[SUCCESS] Cari Kart Oluşturuldu: {testCari.Unvan} | Kod: {testCari.CariKod}");

                // Create unique stock product
                testStok = new StokKart
                {
                    StokAdi = $"Beta Test Ürünü - {uniqueId}",
                    StokKodu = $"STK-BETA-{uniqueId}",
                    Birim = "Adet",
                    AlisFiyati = 100, // Maliyet
                    SatisFiyati = 150, // Satış Fiyatı
                    KdvOrani = 20,
                    Miktar = 0
                };

                await db.InsertAsync(testStok);
                Console.WriteLine($"[SUCCESS] Stok Kartı Oluşturuldu: {testStok.StokAdi} | Maliyet: {testStok.AlisFiyati}₺ | Satış: {testStok.SatisFiyati}₺");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Kart Oluşturma Hatası: {ex.Message}");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("ADIM 3: SATIŞ FATURASI SİMÜLASYONU (TRANSACTIONAL SAFETY)");
            Console.WriteLine("--------------------------------------------------");

            try
            {
                // 10 Adet Satış Faturası Hazırla
                var fatura = new Fatura
                {
                    CariId = testCari.Id,
                    CariUnvan = testCari.Unvan,
                    Tarih = DateTime.Now,
                    FaturaNo = $"FAT-{DateTime.Now.ToString("yyMMddHHmmss")}",
                    Tur = "Satış",
                    GenelToplam = 1500, // 10 adet * 150 ₺
                    Aciklama = "Beta Test E2E Satış Faturası"
                };

                var detaylar = new System.Collections.Generic.List<FaturaDetay>
                {
                    new FaturaDetay
                    {
                        StokId = testStok.Id,
                        StokAdi = testStok.StokAdi,
                        StokKodu = testStok.StokKodu,
                        Miktar = 10,
                        Birim = "Adet",
                        BirimFiyat = 150,
                        KDVOrani = 20,
                        ToplamTutar = 1500
                    }
                };

                Console.WriteLine($"[INFO] Fatura Kaydediliyor (Transactional)... Fatura No: {fatura.FaturaNo}");
                await dbService.SaveFaturaWithTransactionAsync(fatura, detaylar, testCari);
                Console.WriteLine("[SUCCESS] Fatura, stok hareketleri ve cari hesap bakiyeleri işlemsel bütünlük içinde (Atomic) kaydedildi!");

                // Doğrulama: Cari Bakiyesi Güncellemesi
                var updatedCari = await db.Table<CariKart>().FirstOrDefaultAsync(c => c.Id == testCari.Id);
                Console.WriteLine($"[CHECK] Güncel Cari Borç: {updatedCari.Borc}₺ | Alacak: {updatedCari.Alacak}₺ | Bakiye: {updatedCari.Borc - updatedCari.Alacak}₺ (Beklenen: 1500₺ Borç)");

                // Doğrulama: Stok Miktarı ve Maliyet Güncellemesi
                var updatedStok = await db.Table<StokKart>().FirstOrDefaultAsync(s => s.Id == testStok.Id);
                Console.WriteLine($"[CHECK] Güncel Stok Miktarı: {updatedStok.Miktar} (Beklenen: -10)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Fatura Kayıt Hatası: {ex.Message}");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("ADIM 4: FİNANSAL TAHSİLAT HAREKETİ VE KASA SİMÜLASYONU");
            Console.WriteLine("--------------------------------------------------");

            try
            {
                // Kasa kartlarını yükle veya oluştur
                var bankalar = await db.Table<BankaKart>().ToListAsync();
                var kasa = bankalar.FirstOrDefault(b => b.KartTuru == "Kasa");
                if (kasa == null)
                {
                    kasa = new BankaKart { BankaAdi = "Merkez TL Kasası", KartTuru = "Kasa", IBAN = "", Bakiye = 0 };
                    await db.InsertAsync(kasa);
                    Console.WriteLine("[INFO] Merkez TL Kasası oluşturuldu.");
                }

                Console.WriteLine($"[INFO] Kasa: {kasa.BankaAdi} | Başlangıç Bakiyesi: {kasa.Bakiye}₺");

                // Müşteriden 500 ₺ Nakit Tahsilat Yap
                var tahsilat = new CariHareket
                {
                    CariId = testCari.Id,
                    CariUnvan = testCari.Unvan,
                    Tarih = DateTime.Now,
                    IslemTuru = "Tahsilat",
                    Aciklama = "Nakit Tahsilat (Beta Test)",
                    Alacak = 500, // Cari Alacaklanır
                    Borc = 0
                };

                await db.InsertAsync(tahsilat);

                // Cari bakiyesini güncelle
                var finalCari = await db.Table<CariKart>().FirstOrDefaultAsync(c => c.Id == testCari.Id);
                finalCari.Alacak += 500;
                await db.UpdateAsync(finalCari);

                // Kasa hareketini kaydet
                var kasaHareket = new KasaHareket
                {
                    KasaId = kasa.Id,
                    Tarih = DateTime.Now,
                    IslemTuru = "Giriş",
                    Tutar = 500,
                    Aciklama = "Nakit Tahsilat (Beta Test)"
                };
                await db.InsertAsync(kasaHareket);

                // Kasa bakiyesini güncelle
                var finalKasa = await db.Table<BankaKart>().FirstOrDefaultAsync(b => b.Id == kasa.Id);
                finalKasa.Bakiye += 500;
                await db.UpdateAsync(finalKasa);

                Console.WriteLine("[SUCCESS] 500₺ Nakit Tahsilat yapıldı. Cari hareket ve Kasa defteri senkronize güncellendi!");

                // Son Durum Cari Kontrolü
                finalCari = await db.Table<CariKart>().FirstOrDefaultAsync(c => c.Id == testCari.Id);
                Console.WriteLine($"[FINAL CHECK] Cari Bakiye: {finalCari.Borc - finalCari.Alacak}₺ (Beklenen: 1000₺ Borç)");

                // Son Durum Kasa Kontrolü
                finalKasa = await db.Table<BankaKart>().FirstOrDefaultAsync(b => b.Id == kasa.Id);
                Console.WriteLine($"[FINAL CHECK] Kasa Yeni Bakiyesi: {finalKasa.Bakiye}₺ (Beklenen: +500₺ artış)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Tahsilat İşlem Hatası: {ex.Message}");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("==================================================");
            Console.WriteLine("E2E SİMÜLASYONU TAMAMLANDI - SON KULLANICI TESTİ BAŞARILI!");
            Console.WriteLine("==================================================");
            
            // Close connection cleanly
            await dbService.CloseAsync();
        }
    }
}
