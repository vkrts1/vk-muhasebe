using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class YillikSatisHedefi
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int Yil { get; set; }
        public decimal HedefTutari { get; set; }
        public string? Aciklama { get; set; } // Örn: "2026 Büyüme Hedefi"
    }
}
