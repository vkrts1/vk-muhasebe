using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class FaturaTasarimViewModel : ViewModelBase
{
    private readonly DatabaseService? _db;
    private readonly IPdfService? _pdfService;

    public FaturaTasarimViewModel(DatabaseService? db = null, IPdfService? pdfService = null)
    {
        _db = db;
        _pdfService = pdfService;
    }

    [ObservableProperty] private bool _showLogo = true;
    [ObservableProperty] private bool _showBirim = true;
    [ObservableProperty] private bool _showKdv = true;
    [ObservableProperty] private bool _showAraToplam = true;
    [ObservableProperty] private bool _showAciklama = true;
    
    [ObservableProperty] private string _baslikText = "SATIŞ FATURASI";
    [ObservableProperty] private string _altBilgi = "Mal teslimi ve fatura bedeli ödemesi hakkındadır.";
    [ObservableProperty] private string _faturaSize = "A4";
    [ObservableProperty] private string _faturaOrientation = "Dikey";
    [ObservableProperty] private string _markaRengi = "#2563eb";

    public System.Collections.Generic.IReadOnlyList<string> FaturaSizeOptions { get; } =
        new[] { "A4", "A5", "Yazıcı Pulu" };

    public System.Collections.Generic.IReadOnlyList<string> FaturaOrientationOptions { get; } =
        new[] { "Dikey", "Yatay" };

    public override async void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (_db == null) return;
        var t = await _db.GetFaturaTasarimiAsync();
        ShowLogo = t.ShowLogo;
        ShowBirim = t.ShowBirim;
        ShowKdv = t.ShowKdv;
        ShowAraToplam = t.ShowAraToplam;
        ShowAciklama = t.ShowAciklama;
        BaslikText = t.BaslikText;
        AltBilgi = t.AltBilgi;
        FaturaSize = t.FaturaSize;
        FaturaOrientation = t.FaturaOrientation;
        MarkaRengi = t.MarkaRengi;
        ApplyToPdfService();
    }

    private void ApplyToPdfService()
    {
        if (_pdfService == null) return;
        _pdfService.AktifFaturaTasarimi = new FaturaTasarimi
        {
            Id = 1,
            ShowLogo = ShowLogo,
            ShowBirim = ShowBirim,
            ShowKdv = ShowKdv,
            ShowAraToplam = ShowAraToplam,
            ShowAciklama = ShowAciklama,
            BaslikText = BaslikText,
            AltBilgi = AltBilgi,
            FaturaSize = FaturaSize,
            FaturaOrientation = FaturaOrientation,
            MarkaRengi = MarkaRengi
        };
        _pdfService.ShowLogoFatura = ShowLogo;
        _pdfService.FaturaSize = FaturaSize;
        _pdfService.FaturaOrientation = FaturaOrientation;
    }

    [RelayCommand]
    public async Task Kaydet()
    {
        ApplyToPdfService();
        if (_db != null)
        {
            await _db.SaveFaturaTasarimiAsync(new FaturaTasarimi
            {
                Id = 1,
                ShowLogo = ShowLogo,
                ShowBirim = ShowBirim,
                ShowKdv = ShowKdv,
                ShowAraToplam = ShowAraToplam,
                ShowAciklama = ShowAciklama,
                BaslikText = BaslikText,
                AltBilgi = AltBilgi,
                FaturaSize = FaturaSize,
                FaturaOrientation = FaturaOrientation,
                MarkaRengi = MarkaRengi
            });
        }
        SuccessMessage = "Tasarım ayarları kaydedildi.";
    }
}
