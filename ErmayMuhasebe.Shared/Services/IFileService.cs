using System.Threading.Tasks;

namespace ErmayMuhasebe.Services
{
    public interface IFileWrapper
    {
        string Name { get; }
        Task<System.IO.Stream> OpenReadAsync();
    }

    public interface IFileService
    {
        Task SaveAndOpenFileAsync(string fileName, byte[] content, string contentType);
        Task SaveFileAsync(string fileName, byte[] content);
        Task<IFileWrapper?> OpenFilePickerAsync(string title, string[] extensions);
        Task<string?> SaveFilePickerAsync(string title, string defaultFileName, string extension);
        Task<string?> OpenFolderPickerAsync(string title);
        Task<bool> ShowConfirmationAsync(string title, string message);
    }
}
