using ErmayMuhasebe.Shared.ViewModels;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Services;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Cloud.ViewModels;

public partial class SettingsViewModel : ErmayMuhasebe.Shared.ViewModels.SettingsViewModel
{
    public SettingsViewModel(IUnitOfWork uow, ExternalApiService externalApi) : base(uow, externalApi)
    {
    }

    public override Task CreateBackupAsync() => Task.CompletedTask;
    public override Task RestoreBackupAsync() => Task.CompletedTask;
    public override void ChangeTheme(string themeName) { }
    public override Task LoadUsersAsync() => Task.CompletedTask;
    public override Task AddUserAsync() => Task.CompletedTask;
    public override Task UpdateUserPasswordAsync(User user) => Task.CompletedTask;
    public override Task UpdateUserInfoAsync(User user) => Task.CompletedTask;
    public override Task DeleteUserAsync(User user) => Task.CompletedTask;
    public override Task ChangePasswordAsync() => Task.CompletedTask;
    public override Task FactoryResetAsync() => Task.CompletedTask;
    public override Task SendErrorReportAsync() => Task.CompletedTask;
    public override Task ChangeDatabasePathAsync() => Task.CompletedTask;

    // Gelişmiş Teşhis ve Optimizasyon Araçları
    public override Task RunTableAnalyzerAsync() => Task.CompletedTask;
    public override Task RunDiagnosticAsync() => Task.CompletedTask;
    public override Task OpenRecycleBinAsync() => Task.CompletedTask;
    public override Task OpenAutoBackupAsync() => Task.CompletedTask;
    public override Task OpenAlertManagerAsync() => Task.CompletedTask;
    public override Task OpenCronManagerAsync() => Task.CompletedTask;
    public override Task RunYearEndSimulatorAsync() => Task.CompletedTask;
    public override Task RunDocNumberDiagnosticsAsync() => Task.CompletedTask;
    public override Task RunNetworkDiagnosticAsync() => Task.CompletedTask;
    public override Task RunSmtpDiagnosticAsync() => Task.CompletedTask;
    public override Task OpenShortcutsManagerAsync() => Task.CompletedTask;
    public override Task OpenExcelTemplatesAsync() => Task.CompletedTask;
}
