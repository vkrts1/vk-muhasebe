using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Cloud.ViewModels;

public class CariBirlestirmeViewModel : ErmayMuhasebe.Shared.ViewModels.CariBirlestirmeViewModel
{
    public CariBirlestirmeViewModel(IUnitOfWork uow) : base(uow)
    {
    }
}
