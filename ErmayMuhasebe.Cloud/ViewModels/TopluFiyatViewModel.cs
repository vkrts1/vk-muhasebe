using ErmayMuhasebe.Shared.ViewModels;
using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class TopluFiyatViewModel : ErmayMuhasebe.Shared.ViewModels.TopluFiyatViewModel
{
    public TopluFiyatViewModel(IUnitOfWork uow) : base(uow)
    {
    }
}
