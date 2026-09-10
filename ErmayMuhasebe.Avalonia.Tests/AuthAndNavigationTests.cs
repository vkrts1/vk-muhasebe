using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;

namespace ErmayMuhasebe.Avalonia.Tests;

public class AuthAndNavigationTests : HeadlessTestBase
{
    [AvaloniaFact]
    public async Task Admin_Login_With_Valid_Credentials_Succeeds_And_Navigates_To_MainView()
    {
        // 1. Arrange
        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
        Assert.False(mainVm.IsAuthenticated);
        Assert.NotNull(mainVm.LoginViewModel);
        Assert.Same(mainVm.LoginViewModel, mainVm.ActiveAuthView);

        // 2. Act (Kullanıcı son kullanıcı gibi kullanıcı adı ve şifresini yazar)
        mainVm.LoginViewModel.Username = "admin";
        mainVm.LoginViewModel.Password = "123";

        // Giriş butonuna tıklar
        await mainVm.LoginViewModel.LoginCommand.ExecuteAsync(null);

        // 3. Assert (Giriş başarılı olmalı, ana sayfa açılmalı)
        Assert.True(mainVm.IsAuthenticated, "Kullanıcı doğrulanmış olmalı.");
        Assert.Same(mainVm, mainVm.ActiveAuthView);
        Assert.Equal("admin", mainVm.CurrentUserName);
        Assert.NotNull(mainVm.SelectedMenuItem);
        Assert.Equal("Ana Menü", mainVm.SelectedMenuItem.Name);
    }

    [AvaloniaFact]
    public async Task Invalid_Password_Shows_Error_And_Stays_Unauthenticated()
    {
        // 1. Arrange
        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();

        // 2. Act
        mainVm.LoginViewModel!.Username = "admin";
        mainVm.LoginViewModel.Password = "yanlis_sifre_999";

        await mainVm.LoginViewModel.LoginCommand.ExecuteAsync(null);

        // 3. Assert
        Assert.False(mainVm.IsAuthenticated, "Giriş başarısız olmalı.");
        Assert.NotEmpty(mainVm.LoginViewModel.ErrorMessage);
        Assert.Contains("Hatalı", mainVm.LoginViewModel.ErrorMessage);
    }

    [AvaloniaFact]
    public async Task All_14_Left_Menu_Items_Navigate_To_Correct_ViewModels()
    {
        // 1. Arrange & Login
        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
        mainVm.LoginViewModel!.Username = "admin";
        mainVm.LoginViewModel.Password = "123";
        await mainVm.LoginViewModel.LoginCommand.ExecuteAsync(null);
        Assert.True(mainVm.IsAuthenticated);

        // 2. Act & Assert: Sol menüdeki 14 modülün tamamında tek tek gezin
        Assert.Equal(14, mainVm.MenuItems.Count);

        foreach (var item in mainVm.MenuItems)
        {
            if (item.IsHeader) continue;

            mainVm.SelectedMenuItem = item;

            Assert.NotNull(mainVm.CurrentPage);
            Assert.Equal(item.ModelType, mainVm.CurrentPage.GetType());
        }
    }

    [AvaloniaFact]
    public async Task Logout_Resets_Authentication_And_Navigates_To_LoginView()
    {
        // 1. Login
        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
        mainVm.LoginViewModel!.Username = "admin";
        mainVm.LoginViewModel.Password = "123";
        await mainVm.LoginViewModel.LoginCommand.ExecuteAsync(null);
        Assert.True(mainVm.IsAuthenticated);

        // 2. Logout
        mainVm.LogoutCommand.Execute(null);

        // 3. Assert
        Assert.False(mainVm.IsAuthenticated);
        Assert.Same(mainVm.LoginViewModel, mainVm.ActiveAuthView);
    }
}
