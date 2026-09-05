using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Cloud.ViewModels;

public class BarkodTasarimViewModel : ErmayMuhasebe.Shared.ViewModels.BarkodTasarimViewModel
{
    public BarkodTasarimViewModel(IUnitOfWork uow) : base(uow)
    {
    }
}
