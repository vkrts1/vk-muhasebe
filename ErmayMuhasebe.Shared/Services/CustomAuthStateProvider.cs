using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ErmayMuhasebe.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal AdminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.Name, "Admin"),
        new Claim(ClaimTypes.Role, "Admin")
    }, "Permanent"));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(AdminUser));
    }

    public void Login(string username) { NotifyAuthenticationStateChanged(GetAuthenticationStateAsync()); }
    public void Logout() { /* No logout allowed */ }
}
