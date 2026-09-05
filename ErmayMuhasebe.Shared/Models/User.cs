using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class User : ITenantEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string TenantId { get; set; } = "default";
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? PasswordSalt { get; set; }
        public string? Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Email { get; set; }
        public string? TelegramChatId { get; set; }
        public string? FirebaseAuthUid { get; set; }

        [Ignore] // SQLite-net equivalent of NotMapped
        public string? TempPassword { get; set; }
    }
}
