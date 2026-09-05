using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class BorcHatirlaticiViewModel : ErmayMuhasebe.Shared.ViewModels.BorcHatirlaticiViewModel
{
    public BorcHatirlaticiViewModel(IUnitOfWork uow) : base(uow)
    {
    }

    public override async Task SendSmsAsync(ErmayMuhasebe.Shared.ViewModels.BorcHatirlatitmaItem item)
    {
        SuccessMessage = "";
        ErrorMessage = "";
        try 
        {
            // Simulate API call
            await Task.Delay(1000);
            SuccessMessage = $"{item.CariUnvan} - {item.Telefon} numarasına SMS başarıyla gönderildi.";
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"SMS Gönderim Hatası: {ex.Message}";
        }
    }

    public override async Task SendEmailAsync(ErmayMuhasebe.Shared.ViewModels.BorcHatirlatitmaItem item)
    {
        SuccessMessage = "";
        ErrorMessage = "";
        try 
        {
            // Simulate API call
            await Task.Delay(1000);
            SuccessMessage = $"{item.CariUnvan} mail adresine hatırlatma gönderildi.";
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Email Gönderim Hatası: {ex.Message}";
        }
    }
}
