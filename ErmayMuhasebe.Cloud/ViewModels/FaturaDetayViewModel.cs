using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class FaturaDetayViewModel : ErmayMuhasebe.Shared.ViewModels.FaturaDetayViewModel
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;

    public FaturaDetayViewModel(IUnitOfWork uow, PdfService pdfService, NavigationManager navigationManager, IJSRuntime jsRuntime) 
        : base(uow, pdfService)
    {
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
    }

    protected override void NotifyFinancialDataChanged()
    {
        // No specific message needed for web
    }

    protected override void OnRequestClose()
    {
        _navigationManager.NavigateTo("/faturalar");
    }

    protected override async Task HandleFileOpenAsync(byte[] content, string fileName)
    {
        var base64 = System.Convert.ToBase64String(content);
        await _jsRuntime.InvokeVoidAsync("open", $"data:application/pdf;base64,{base64}", "_blank");
    }
}
