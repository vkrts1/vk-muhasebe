namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class FaturaTasarimViewModel : ErmayMuhasebe.Shared.ViewModels.FaturaTasarimViewModel
{
    public FaturaTasarimViewModel(
        ErmayMuhasebe.Services.DatabaseService db,
        ErmayMuhasebe.Services.PdfService pdfService)
        : base(db, pdfService)
    {
    }
}
