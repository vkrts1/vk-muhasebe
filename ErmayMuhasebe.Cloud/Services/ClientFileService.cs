using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Cloud.Services;

public interface IFileService
{
    Task DownloadFileAsync(string fileName, string contentType, byte[] content);
    Task OpenPdfAsync(byte[] content);
}

public class ClientFileService : IFileService, ErmayMuhasebe.Services.IFileService
{
    private readonly IJSRuntime _js;

    public ClientFileService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task DownloadFileAsync(string fileName, string contentType, byte[] content)
    {
        await _js.InvokeVoidAsync("downloadFile", fileName, contentType, content);
    }

    public async Task OpenPdfAsync(byte[] content)
    {
        await _js.InvokeVoidAsync("openPdf", content);
    }

    // ErmayMuhasebe.Services.IFileService implementation
    public Task SaveAndOpenFileAsync(string fileName, byte[] content, string contentType) => DownloadFileAsync(fileName, contentType, content);
    public Task SaveFileAsync(string fileName, byte[] content) => DownloadFileAsync(fileName, "application/octet-stream", content);
    public Task<ErmayMuhasebe.Services.IFileWrapper?> OpenFilePickerAsync(string title, string[] extensions) => Task.FromResult<ErmayMuhasebe.Services.IFileWrapper?>(null);
    public Task<string?> SaveFilePickerAsync(string title, string defaultFileName, string extension) => Task.FromResult<string?>(null);
    public Task<string?> OpenFolderPickerAsync(string title) => Task.FromResult<string?>(null);
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        return await _js.InvokeAsync<bool>("confirm", message);
    }
}
