using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Cloud.Services;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class KasaDetayViewModel : ErmayMuhasebe.Shared.ViewModels.KasaDetayViewModel
{
    private readonly ErmayMuhasebe.Cloud.Services.IFileService _fileService;
    private readonly NavigationManager _navigationManager;

    public KasaDetayViewModel(IUnitOfWork uow, PdfService pdfService, ErmayMuhasebe.Cloud.Services.IFileService fileService, NavigationManager navigationManager) 
        : base(uow, pdfService)
    {
        _fileService = fileService;
        _navigationManager = navigationManager;
    }

    protected override void NotifyFinancialDataChanged()
    {
        // No specific message needed for now
    }

    protected override async Task HandleFileOpenAsync(byte[] content, string fileName)
    {
        if (fileName.EndsWith(".pdf")) await _fileService.OpenPdfAsync(content);
        else await _fileService.DownloadFileAsync(fileName, "application/octet-stream", content);
    }

    public override void GoBack()
    {
        _navigationManager.NavigateTo("/kasalar");
    }
}
