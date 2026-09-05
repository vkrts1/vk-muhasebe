using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class SistemSaglikViewModel : ViewModelBase
{
    [ObservableProperty] private double _cpuUsage;
    [ObservableProperty] private double _ramUsage;
    [ObservableProperty] private string _uptime = "00:00:00";
    [ObservableProperty] private int _activeUsers = 1;

    private readonly DateTime _startTime = DateTime.Now;
    private bool _isUpdating = true;

    public SistemSaglikViewModel()
    {
        _ = UpdateStatsAsync();
    }

    private async Task UpdateStatsAsync()
    {
        var random = new Random();
        while (_isUpdating)
        {
            CpuUsage = random.Next(2, 8);
            RamUsage = 180 + random.Next(0, 40); // Simple MB simulation
            Uptime = (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss");
            await Task.Delay(2000);
        }
    }

    public void StopUpdates() => _isUpdating = false;
}
