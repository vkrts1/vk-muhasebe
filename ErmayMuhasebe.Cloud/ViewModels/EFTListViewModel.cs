using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Cloud.Services;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class EFTListViewModel : ErmayMuhasebe.Shared.ViewModels.EFTListViewModel
{
    private readonly ErmayMuhasebe.Cloud.Services.IFileService _fileService;

    public EFTListViewModel(IUnitOfWork uow, PdfService pdfService, ErmayMuhasebe.Cloud.Services.IFileService fileService) 
        : base(uow, pdfService)
    {
        _fileService = fileService;
    }

    protected override void NotifyFinancialDataChanged()
    {
        // Internal message for desktop not needed in web currently
    }

    protected override async Task HandleFileOpenAsync(byte[] content, string fileName)
    {
        if (fileName.EndsWith(".pdf")) await _fileService.OpenPdfAsync(content);
        else await _fileService.DownloadFileAsync(fileName, "application/octet-stream", content);
    }
}
