using Avalonia.Controls;
using Avalonia.Input;
using ErmayMuhasebe.Avalonia.ViewModels;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void Password_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.LoginCommand.Execute(null);
            }
        }
    }
}
