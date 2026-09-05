using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Cloud.ViewModels;

public class StokGrupDuzenleViewModel : ErmayMuhasebe.Shared.ViewModels.StokGrupDuzenleViewModel
{
    public StokGrupDuzenleViewModel(IUnitOfWork uow) : base(uow)
    {
    }
}
