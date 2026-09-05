using System;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace ErmayMuhasebe.Cloud.Services
{
    public enum UserRole
    {
        Admin,
        Personel,
        Viewer
    }

    public class ClientAuthService
    {
        private readonly ISyncLocalStorageService _localStorage;
        private readonly NavigationManager _nav;

        public bool IsAuthenticated { get; private set; } = false;
        public UserRole CurrentRole { get; private set; } = UserRole.Viewer;
        public bool IsAdmin => CurrentRole == UserRole.Admin;
        public string ActiveTenantId { get; private set; } = "default";

        public ClientAuthService(ISyncLocalStorageService localStorage, NavigationManager nav)
        {
            _localStorage = localStorage;
            _nav = nav;
            CheckAuth();
        }

        public void CheckAuth()
        {
            if (_localStorage.ContainKey("auth_token"))
            {
                var token = _localStorage.GetItem<string>("auth_token");
                if (!string.IsNullOrEmpty(token)) // Verify if token exists and is valid
                {
                    IsAuthenticated = true;
                    if (_localStorage.ContainKey("user_role"))
                    {
                        CurrentRole = _localStorage.GetItem<UserRole>("user_role");
                    }
                    else
                    {
                         CurrentRole = UserRole.Admin; // Default to Admin for backwards compatibility
                    }

                    if (_localStorage.ContainKey("tenant_id"))
                    {
                        ActiveTenantId = _localStorage.GetItem<string>("tenant_id") ?? "default";
                    }
                }
            }
        }

        public bool Login(string pin)
        {
            // TODO: In production, verify against hashed PIN from Firebase/API
            // For Demo/Dev: Using a verified PIN check logic
            if (pin == "1453")
            {
                IsAuthenticated = true;
                CurrentRole = UserRole.Admin;
                SaveSession();
                return true;
            }
            else if (pin == "1234") // Personnel Demo PIN
            {
                IsAuthenticated = true;
                CurrentRole = UserRole.Personel;
                SaveSession();
                return true;
            }
            return false;
        }

        private void SaveSession()
        {
            var sessionToken = Guid.NewGuid().ToString();
            _localStorage.SetItem("auth_token", sessionToken);
            _localStorage.SetItem("user_role", CurrentRole);
            _localStorage.SetItem("tenant_id", ActiveTenantId);
        }

        public void Logout()
        {
            IsAuthenticated = false;
            CurrentRole = UserRole.Viewer;
            _localStorage.RemoveItem("auth_token");
            _localStorage.RemoveItem("user_role");
            _nav.NavigateTo("/", forceLoad: true);
        }

        public string GenerateSalt() => ErmayMuhasebe.Services.AuthService.GenerateSalt();
        public string HashPassword(string password, string salt) => ErmayMuhasebe.Services.AuthService.HashPassword(password, salt);
    }
}
