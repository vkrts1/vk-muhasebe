using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Cloud.Services;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class BankaDetayViewModel : ErmayMuhasebe.Shared.ViewModels.BankaDetayViewModel
{
    private readonly ErmayMuhasebe.Cloud.Services.IFileService _fileService;
    private readonly NavigationManager _navigationManager;

    public BankaDetayViewModel(IUnitOfWork uow, PdfService pdfService, ErmayMuhasebe.Cloud.Services.IFileService fileService, NavigationManager navigationManager) 
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
        _navigationManager.NavigateTo("/bankalar");
    }

    public override async Task SelectCariForTransactionAsync()
    {
        await Task.CompletedTask; // Implement selection logic if needed
    }

    public override async Task UploadDekontForTransactionAsync()
    {
         await Task.CompletedTask; // Implement upload logic if needed
    }
    
    public override async Task TransferTransactionAsync()
    {
        await Task.CompletedTask; // Implement transfer logic if needed
    }
}
