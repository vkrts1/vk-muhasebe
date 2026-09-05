using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class VadeTakipViewModel : ErmayMuhasebe.Shared.ViewModels.VadeTakipViewModel
{
    public VadeTakipViewModel(IUnitOfWork uow) : base(uow)
    {
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
    }
}
