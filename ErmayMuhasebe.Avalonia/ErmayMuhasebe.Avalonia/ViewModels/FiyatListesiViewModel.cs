using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public record KatalogTema(string Isim, string PrimaryColor, string PreviewImg);

public partial class FiyatListesiViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    private readonly IPdfService _pdfService;
    private readonly IFileService _fileService;

    [ObservableProperty] private ObservableCollection<KatalogTema> _temalar = new();
    [ObservableProperty] private KatalogTema? _seciliTema;
    [ObservableProperty] private string _katalogBasligi = "2026 KIŞ KATALOĞU";
    [ObservableProperty] private bool _showPrices = true;

    public FiyatListesiViewModel(IUnitOfWork uow, IPdfService pdfService, IFileService fileService)
    {
        _uow = uow;
        _pdfService = pdfService;
        _fileService = fileService;

        Temalar = new ObservableCollection<KatalogTema>
        {
            new("Modern Minimal", "#6366F1", "img1"),
            new("Kurumsal Lacivert", "#1E3A8A", "img2"),
            new("Vibrant Turuncu", "#F97316", "img3")
        };
        SeciliTema = Temalar[0];
    }

    [RelayCommand]
    public async Task KatalogOlusturAsync()
    {
        try
        {
            IsLoading = true;
            var stoklar = await _uow.Stoklar.GetAllAsync();
            var list = stoklar.ToList();
            if (!list.Any())
            {
                ErrorMessage = "Katalog oluşturmak için sistemde kayıtlı ürün bulunamadı.";
                return;
            }

            var pdfBytes = await _pdfService.GenerateStokListPdfBytesAsync(list);
            string safeTitle = string.Join("_", (string.IsNullOrWhiteSpace(KatalogBasligi) ? "Katalog" : KatalogBasligi).Split(System.IO.Path.GetInvalidFileNameChars()));
            await _fileService.SaveAndOpenFileAsync($"{safeTitle}.pdf", pdfBytes, "application/pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Katalog PDF oluşturma hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
