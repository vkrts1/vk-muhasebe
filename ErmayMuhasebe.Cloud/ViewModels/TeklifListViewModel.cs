using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class TeklifListViewModel : ErmayMuhasebe.Shared.ViewModels.TeklifListViewModel
{
    public TeklifListViewModel(IUnitOfWork uow, PdfService pdfService, IFileService fileService) 
        : base(uow, pdfService, fileService)
    {
    }
}
