using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class SiparisListViewModel : ErmayMuhasebe.Shared.ViewModels.SiparisListViewModel
{
    public SiparisListViewModel(IUnitOfWork uow, PdfService pdfService, IFileService fileService) 
        : base(uow, pdfService, fileService)
    {
    }
}
