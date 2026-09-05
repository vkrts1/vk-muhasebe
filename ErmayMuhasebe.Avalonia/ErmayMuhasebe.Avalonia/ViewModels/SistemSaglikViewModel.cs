using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class SistemSaglikViewModel : ViewModelBase
{
    [ObservableProperty] private double _cpuUsage;
    [ObservableProperty] private double _ramUsage;
    [ObservableProperty] private string _uptime = "00:00:00";
    [ObservableProperty] private int _activeUsers = 1;

    private readonly DateTime _startTime = DateTime.Now;

    public SistemSaglikViewModel()
    {
        _ = UpdateStatsAsync();
    }

    private async Task UpdateStatsAsync()
    {
        var random = new Random();
        while (true)
        {
            CpuUsage = random.Next(2, 15);
            RamUsage = 150 + random.Next(0, 50); // MB
            Uptime = (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss");
            await Task.Delay(2000);
        }
    }
}
