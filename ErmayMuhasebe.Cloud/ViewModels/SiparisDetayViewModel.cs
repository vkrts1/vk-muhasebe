using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Cloud.ViewModels;

public class SiparisDetayViewModel : ErmayMuhasebe.Shared.ViewModels.SiparisDetayViewModel
{
    public SiparisDetayViewModel(IUnitOfWork uow) : base(uow)
    {
    }
}
