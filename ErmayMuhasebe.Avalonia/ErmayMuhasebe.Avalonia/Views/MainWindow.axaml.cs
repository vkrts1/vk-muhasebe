using Avalonia.Controls;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        try
        {
            var uri = new System.Uri("avares://ErmayMuhasebe.Avalonia/Assets/avalonia-logo.ico");
            if (global::Avalonia.Platform.AssetLoader.Exists(uri))
            {
                using var stream = global::Avalonia.Platform.AssetLoader.Open(uri);
                this.Icon = new global::Avalonia.Controls.WindowIcon(stream);
            }
        }
        catch { }
    }

    protected override void OnPointerPressed(global::Avalonia.Input.PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Session reset on any click
        var app = global::Avalonia.Application.Current as App;
        var session = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<ErmayMuhasebe.Services.SessionService>(app?.Services!);
        session?.ResetActivity();

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed && point.Position.Y <= 50)
        {
            // Begin window drag if in top 50px
            BeginMoveDrag(e);
        }
    }
    protected override async void OnKeyDown(global::Avalonia.Input.KeyEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            // Escape handling
            if (e.Key == global::Avalonia.Input.Key.Escape)
            {
                vm.GoBack();
                e.Handled = true;
                return;
            }

            // Global F11 reset scaling
            if (e.Key == global::Avalonia.Input.Key.F11)
            {
                var app = global::Avalonia.Application.Current as App;
                var ds = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<ErmayMuhasebe.Services.DisplayService>(app?.Services!);
                ds?.SetScaling(1.0);
                e.Handled = true;
                return;
            }

            // Global Dinamik Kısayollar
            var currentApp = global::Avalonia.Application.Current as App;
            var uow = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<ErmayMuhasebe.Repositories.IUnitOfWork>(currentApp?.Services!);
            if (uow != null)
            {
                try
                {
                    var profil = await uow.GetFirmaProfiliAsync();
                    if (profil != null)
                    {
                        string pressedKey = e.Key.ToString();
                        
                        if (pressedKey == profil.ShortcutNewInvoice)
                        {
                            var pdfService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<ErmayMuhasebe.Services.IPdfService>(currentApp?.Services!);
                            if (pdfService != null)
                            {
                                var invoiceVm = new ViewModels.FaturaDetayViewModel(uow, pdfService, null, "Satış");
                                invoiceVm.RequestClose += () => {
                                    vm.NavigateTo(typeof(ViewModels.DashboardViewModel), true);
                                };
                                vm.CurrentPage = invoiceVm;
                                e.Handled = true;
                                return;
                            }
                        }
                        else if (pressedKey == profil.ShortcutNewCari)
                        {
                            vm.NavigateTo(typeof(ViewModels.CariListViewModel));
                            var listVm = vm.CurrentPage as ViewModels.CariListViewModel;
                            listVm?.OpenAddCariDialog();
                            e.Handled = true;
                            return;
                        }
                        else if (pressedKey == profil.ShortcutNewStok)
                        {
                            vm.NavigateTo(typeof(ViewModels.StokListViewModel));
                            var listVm = vm.CurrentPage as ViewModels.StokListViewModel;
                            listVm?.CreateNew();
                            e.Handled = true;
                            return;
                        }
                        else if (pressedKey == profil.ShortcutOpenRaporlar)
                        {
                            vm.NavigateTo(typeof(ViewModels.RaporListViewModel));
                            e.Handled = true;
                            return;
                        }
                        else if (pressedKey == profil.ShortcutOpenFaturalar)
                        {
                            vm.NavigateTo(typeof(ViewModels.FaturaListViewModel));
                            e.Handled = true;
                            return;
                        }
                        else if (pressedKey == profil.ShortcutOpenCariler)
                        {
                            vm.NavigateTo(typeof(ViewModels.CariListViewModel));
                            e.Handled = true;
                            return;
                        }
                    }
                }
                catch { }
            }

            // Function Keys for Faturalar / Other views
            if (vm.CurrentPage is ViewModels.FaturaDetayViewModel fvm)
            {
                switch (e.Key)
                {
                    case global::Avalonia.Input.Key.F2:
                        _ = fvm.OpenCariSecim();
                        e.Handled = true;
                        break;
                    case global::Avalonia.Input.Key.F4:
                        _ = fvm.SaveFaturaAsync();
                        e.Handled = true;
                        break;
                    case global::Avalonia.Input.Key.F7:
                        _ = fvm.OpenStokSecim();
                        e.Handled = true;
                        break;
                }
            }
            else if (vm.CurrentPage is ErmayMuhasebe.Shared.ViewModels.FaturaDetayViewModel sharedFvm)
            {
                 // Handle shared viewmodel if accessed directly
            }
        }
        base.OnKeyDown(e);
    }
}
