using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Cloud.ViewModels;

public class TeklifDetayViewModel : ErmayMuhasebe.Shared.ViewModels.TeklifDetayViewModel
{
    public TeklifDetayViewModel(IUnitOfWork uow) : base(uow)
    {
    }
}
