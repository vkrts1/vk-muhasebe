using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public record DocumentItem(string Name, string Type, long Size, DateTime CreatedAt, string Path, int DbId = 0);

public partial class BelgeArsivViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<DocumentItem> _documents = new();
    [ObservableProperty] private string _searchQuery = "";
    [ObservableProperty] private string _storageInfo = "";
    [ObservableProperty] private string _category = "Genel";

    private string _storagePath = "";

    public Func<Task<string?>>? FilePicker { get; set; }
    public Func<string, Task<string?>>? SaveFilePicker { get; set; }

    public BelgeArsivViewModel()
    {
        UpdatePath();
        _ = LoadDocumentsAsync();
    }

    public void SetCategory(string category)
    {
        Category = category;
        UpdatePath();
        _ = LoadDocumentsAsync();
    }

    private void UpdatePath()
    {
        _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErmayDMS");
        if (Category != "Genel")
            _storagePath = Path.Combine(_storagePath, Category);
            
        if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
    }

    private async Task LoadDocumentsAsync()
    {
        try 
        {
            var uow = ((App)global::Avalonia.Application.Current!).Services?.GetService<ErmayMuhasebe.Repositories.IUnitOfWork>();
            if (uow == null) return;

            var dbList = await uow.BelgeArsiv.GetByKategoriAsync(Category);

            // Import any local files in _storagePath that are not yet registered in the SQLite database
            if (Directory.Exists(_storagePath))
            {
                var localFiles = Directory.GetFiles(_storagePath);
                bool importedAny = false;
                foreach (var file in localFiles)
                {
                    var fi = new FileInfo(file);
                    // Check if a database record exists with the same filename (Ad)
                    var existsInDb = dbList.Any(x => x.Ad == fi.Name);
                    if (!existsInDb)
                    {
                        try
                        {
                            byte[] fileBytes = await File.ReadAllBytesAsync(file);
                            string base64Content = Convert.ToBase64String(fileBytes);
                            string sizeStr = fi.Length > 1024 * 1024 
                                ? $"{(fi.Length / (1024.0 * 1024.0)):N2} MB" 
                                : $"{(fi.Length / 1024.0):N0} KB";

                            var newBelge = new BelgeArsiv
                            {
                                Ad = fi.Name,
                                Kategori = Category,
                                Tur = fi.Extension.Replace(".", "").ToLower(),
                                Boyut = sizeStr,
                                Tarih = fi.CreationTime,
                                Veri = base64Content,
                                Yol = file,
                                IsDeleted = false
                            };
                            await uow.BelgeArsiv.SaveAsync(newBelge);
                            importedAny = true;
                        }
                        catch {}
                    }
                }
                
                if (importedAny)
                {
                    dbList = await uow.BelgeArsiv.GetByKategoriAsync(Category);
                }
            }

            var list = await Task.Run(() => {
                return dbList.Select(b => {
                    string localPath = b.Yol ?? "";
                    
                    // If local path is missing or doesn't exist, reconstruct/decode from base64
                    if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
                    {
                        localPath = Path.Combine(_storagePath, b.Ad ?? "belge");
                        if (!string.IsNullOrEmpty(b.Veri) && !File.Exists(localPath))
                        {
                            try
                            {
                                var bytes = Convert.FromBase64String(b.Veri);
                                File.WriteAllBytes(localPath, bytes);
                            }
                            catch {}
                        }
                    }

                    // Parse size string (e.g. "1.24 MB" or "40 KB")
                    long size = 0;
                    if (!string.IsNullOrEmpty(b.Boyut))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(b.Boyut, @"[\d\.,]+");
                        if (match.Success && double.TryParse(match.Value.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedVal))
                        {
                            if (b.Boyut.Contains("MB")) size = (long)(parsedVal * 1024 * 1024);
                            else size = (long)(parsedVal * 1024);
                        }
                    }

                    return new DocumentItem(b.Ad ?? "Belge", (b.Tur ?? "").ToUpper(), size, b.Tarih, localPath, b.Id);
                }).OrderByDescending(x => x.CreatedAt).ToList();
            });

            Documents = new ObservableCollection<DocumentItem>(list);
            
            long totalSize = list.Sum(x => x.Size);
            StorageInfo = $"Toplam: {list.Count} Belge | Alan: {(totalSize / 1024.0 / 1024.0):N2} MB";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Arşiv Yükleme Hatası: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task YeniBelgeEkleAsync()
    {
        if (FilePicker == null) return;

        var sourcePath = await FilePicker();
        if (string.IsNullOrEmpty(sourcePath)) return;

        try
        {
            var uow = ((App)global::Avalonia.Application.Current!).Services?.GetService<ErmayMuhasebe.Repositories.IUnitOfWork>();
            if (uow == null) return;

            var fi = new FileInfo(sourcePath);
            var destPath = Path.Combine(_storagePath, fi.Name);
            
            // If file exists, rename
            if (File.Exists(destPath))
            {
                string name = Path.GetFileNameWithoutExtension(fi.Name);
                string ext = fi.Extension;
                destPath = Path.Combine(_storagePath, $"{name}_{DateTime.Now.Ticks}{ext}");
            }

            File.Copy(sourcePath, destPath);

            // Read file bytes and convert to Base64
            byte[] fileBytes = await File.ReadAllBytesAsync(destPath);
            string base64Content = Convert.ToBase64String(fileBytes);
            string sizeStr = fi.Length > 1024 * 1024 
                ? $"{(fi.Length / (1024.0 * 1024.0)):N2} MB" 
                : $"{(fi.Length / 1024.0):N0} KB";

            var newBelge = new BelgeArsiv
            {
                Ad = Path.GetFileName(destPath),
                Kategori = Category,
                Tur = fi.Extension.Replace(".", "").ToLower(),
                Boyut = sizeStr,
                Tarih = DateTime.Now,
                Veri = base64Content,
                Yol = destPath,
                IsDeleted = false
            };

            await uow.BelgeArsiv.SaveAsync(newBelge);

            await LoadDocumentsAsync();
        }
        catch (Exception ex)
        {
             System.Diagnostics.Debug.WriteLine($"Belge Ekleme Hatası: {ex.Message}");
        }
    }

    [RelayCommand]
    public void BelgeAc(DocumentItem item)
    {
        try {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = item.Path,
                UseShellExecute = true
            });
        } catch { }
    }

    [RelayCommand]
    public async Task DosyaIndirAsync(DocumentItem item)
    {
        if (SaveFilePicker == null || item == null) return;

        var destPath = await SaveFilePicker(item.Name);
        if (string.IsNullOrEmpty(destPath)) return;

        try
        {
            File.Copy(item.Path, destPath, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"İndirme Hatası: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task BelgeSilAsync(DocumentItem item)
    {
        if (item == null) return;

        var uow = ((App)global::Avalonia.Application.Current!).Services?.GetService<ErmayMuhasebe.Repositories.IUnitOfWork>();
        var fileService = ((App)global::Avalonia.Application.Current!).Services?.GetService<ErmayMuhasebe.Services.IFileService>();
        if (fileService != null)
        {
            bool confirm = await fileService.ShowConfirmationAsync(
                "Belgeyi Sil", 
                $"'{item.Name}' isimli belge kalıcı olarak silinecektir. Devam etmek istiyor musunuz?"
            );
            if (!confirm) return;
        }

        try
        {
            if (uow != null && item.DbId != 0)
            {
                await uow.BelgeArsiv.DeleteAsync(item.DbId);
            }

            if (File.Exists(item.Path))
            {
                await Task.Run(() => File.Delete(item.Path));
            }
            await LoadDocumentsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Belge Silme Hatası: {ex.Message}");
        }
    }
}
