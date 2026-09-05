using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Services
{
    public class SecurityRequest
    {
        public string RequestId { get; set; } = "";
        public string Uid { get; set; } = "";
        public string Type { get; set; } = ""; // PASSWORD_CHANGE, USERNAME_CHANGE
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED, EXPIRED
        public string NewValue { get; set; } = ""; // Yeni şifre (hashlenmiş) veya yeni kullanıcı adı
        public string CreatedAt { get; set; } = "";
        public string ExpiresAt { get; set; } = "";
    }

    public class UserSecurityState
    {
        public string LastPasswordChange { get; set; } = "";
        public string ActiveSessionsRevokedAt { get; set; } = "";
    }

    public class SecuritySyncService
    {
        private readonly CloudSyncService _cloudSyncService;
        private IDisposable? _requestSubscription;
        private IDisposable? _sessionSubscription;

        public event Action<string>? OnRequestStatusChanged; // PENDING, APPROVED, REJECTED, EXPIRED
        public event Action? OnSessionRevoked;

        public SecuritySyncService(CloudSyncService cloudSyncService)
        {
            _cloudSyncService = cloudSyncService;
        }

        private FirebaseClient? GetClient()
        {
            return _cloudSyncService.Client;
        }

        /// <summary>
        /// Şifre/Kullanıcı adı değişiklik talebini gerçek zamanlı dinler.
        /// </summary>
        public void ListenToSecurityRequest(string requestId)
        {
            var client = GetClient();
            if (client == null) return;

            _requestSubscription?.Dispose();

            _requestSubscription = client
                .Child("security_requests")
                .Child(requestId)
                .AsObservable<SecurityRequest>()
                .Subscribe(eventArgs =>
                {
                    if (eventArgs.Object != null)
                    {
                        OnRequestStatusChanged?.Invoke(eventArgs.Object.Status);
                    }
                });
        }

        /// <summary>
        /// Kullanıcının oturum iptal durumunu dinler.
        /// </summary>
        public void ListenToSessionStatus(string userId, DateTime sessionStartTime)
        {
            var client = GetClient();
            if (client == null) return;

            _sessionSubscription?.Dispose();

            _sessionSubscription = client
                .Child("users")
                .Child(userId)
                .Child("security")
                .AsObservable<UserSecurityState>()
                .Subscribe(eventArgs =>
                {
                    if (eventArgs.Object != null && !string.IsNullOrEmpty(eventArgs.Object.ActiveSessionsRevokedAt))
                    {
                        if (DateTime.TryParse(eventArgs.Object.ActiveSessionsRevokedAt, out DateTime revokedAt))
                        {
                            if (revokedAt > sessionStartTime)
                            {
                                OnSessionRevoked?.Invoke();
                            }
                        }
                    }
                });
        }

        /// <summary>
        /// Yeni bir güvenlik talebi oluşturur.
        /// </summary>
        public async Task<string> CreateSecurityRequestAsync(string userId, string type, string newValue)
        {
            var client = GetClient();
            if (client == null) throw new Exception("Bulut bağlantısı aktif değil.");

            string requestId = Guid.NewGuid().ToString("N");
            var request = new SecurityRequest
            {
                RequestId = requestId,
                Uid = userId,
                Type = type,
                Status = "PENDING",
                NewValue = newValue,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15).ToString("o")
            };

            await client.Child("security_requests").Child(requestId).PutAsync(request);
            return requestId;
        }

        /// <summary>
        /// Kullanıcının şifre değişiklik zamanını ve oturum iptal zaman damgasını günceller.
        /// </summary>
        public async Task UpdateUserSecurityStateAsync(string userId)
        {
            var client = GetClient();
            if (client == null) return;

            var timestamp = DateTime.UtcNow.ToString("o");
            var state = new UserSecurityState
            {
                LastPasswordChange = timestamp,
                ActiveSessionsRevokedAt = timestamp
            };

            await client.Child("users").Child(userId).Child("security").PutAsync(state);
        }

        /// <summary>
        /// Dinleyicileri kapatır.
        /// </summary>
        public void StopListeners()
        {
            _requestSubscription?.Dispose();
            _requestSubscription = null;
            _sessionSubscription?.Dispose();
            _sessionSubscription = null;
        }
    }
}
