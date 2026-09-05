using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Threading.Tasks;
using System;
using Avalonia.Threading;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class RaporListViewModel : ErmayMuhasebe.Shared.ViewModels.RaporListViewModel
{
    public RaporListViewModel(IUnitOfWork uow, IPdfService pdfService) : base(uow, pdfService)
    {
    }

    protected override async Task InvokeOnUIThreadAsync(Action action)
    {
        await Dispatcher.UIThread.InvokeAsync(action);
    }

    protected override async Task HandleFileOpenAsync(byte[] content, string fileName)
    {
        try 
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ErmayFiles");
            if (!System.IO.Directory.Exists(tempDir)) System.IO.Directory.CreateDirectory(tempDir);
            
            string safeName = System.IO.Path.GetFileNameWithoutExtension(fileName) + "_" + DateTime.Now.ToString("HHmmss") + System.IO.Path.GetExtension(fileName);
            string path = System.IO.Path.Combine(tempDir, safeName);
            
            await System.IO.File.WriteAllBytesAsync(path, content);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch { }
    }

    protected override async Task HandleFilePrintAsync(byte[] content, string fileName)
    {
        try 
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ErmayPrint");
            if (!System.IO.Directory.Exists(tempDir)) System.IO.Directory.CreateDirectory(tempDir);
            
            string safeName = "Print_" + DateTime.Now.ToString("HHmmss") + "_" + fileName;
            string path = System.IO.Path.Combine(tempDir, safeName);
            
            await System.IO.File.WriteAllBytesAsync(path, content);
            
            try 
            {
               System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) 
               { 
                   UseShellExecute = true,
                   Verb = "print" 
               }); 
            }
            catch 
            {
                // Fallback: just open
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
        }
        catch { }
    }
}
