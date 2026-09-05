using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Data;
using Firebase.Database;
using Firebase.Database.Query;

namespace db_inspector
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErmayMuhasebe");
            
            // 1. Inspect Firebase Cloud Data
            Console.WriteLine("\n==================================================");
            Console.WriteLine("INSPECTING FIREBASE CLOUD DATA");
            Console.WriteLine("==================================================");
            try
            {
                var fbClient = new Firebase.Database.FirebaseClient("https://vkermay-default-rtdb.firebaseio.com", new Firebase.Database.FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult("6c5NPoL9ccRa9LePyjFcdB1q9r73vZG1An6QdUEg")
                });

                string[] years = { "2025", "2026" };
                string[] nodes = { "Cariler", "CariHareketler", "Stoklar", "StokHareketler", "Faturalar", "KasaHareketler", "BankaHareketler", "Cekler" };
                
                foreach (var yr in years)
                {
                    Console.WriteLine($"\n--- Year: {yr} ---");
                    foreach (var node in nodes)
                    {
                        try
                        {
                            var collection = await fbClient.Child($"companies/default/years/{yr}/{node}").OnceAsync<object>();
                            Console.WriteLine($"  {node} in Firebase: {collection.Count} records");
                        }
                        catch (Exception nEx)
                        {
                            Console.WriteLine($"  Error reading {node} from Firebase: {nEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase Init/Connection Error: {ex.Message}");
            }

            string stablePath = Path.Combine(dir, "ErmayV4_Stable.db3");
            string yearPath = Path.Combine(dir, "ermay_2026.db");

            string[] dbPaths = { stablePath, yearPath };
            foreach (var path in dbPaths)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"DB not found: {path}");
                    continue;
                }
                
                Console.WriteLine($"\n==================================================");
                Console.WriteLine($"INSPECTING: {Path.GetFileName(path)}");
                Console.WriteLine($"==================================================");

                try
                {
                    var dbService = new DatabaseService();
                    dbService.UseEncryption = true;
                    await dbService.InitializeAsync(path);
                    var connection = await dbService.GetConnectionAsync();

                    var tables = new[] {
                        "CariKart", "CariHareket", "StokKart", "StokHareket", "Fatura", "KasaHareket", "Cek"
                    };

                    foreach (var tbl in tables)
                    {
                        try
                        {
                            var count = await connection.ExecuteScalarAsync<int>($"SELECT count(*) FROM {tbl}");
                            Console.WriteLine($"Table {tbl}: {count} records");
                        }
                        catch (Exception tEx)
                        {
                            Console.WriteLine($"Table {tbl} error: {tEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
