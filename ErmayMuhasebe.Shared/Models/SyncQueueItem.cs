using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class SyncQueueItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string EntityType { get; set; } = ""; // Cari, Stok, Fatura, etc.
        public string Operation { get; set; } = "Put"; // Put, Delete
        public string EntityId { get; set; } = "";
        public string JsonData { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int RetryCount { get; set; } = 0;
        public bool IsSynced { get; set; } = false;
        public string? Error { get; set; }
    }
}
