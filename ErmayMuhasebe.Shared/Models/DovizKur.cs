using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class DovizKur
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string? Kod { get; set; }
        public string? Isim { get; set; }
        public decimal Alis { get; set; }
        public decimal Satis { get; set; }
        public decimal EfektifAlis { get; set; } // Missing
        public decimal EfektifSatis { get; set; } // Missing
        public DateTime Tarih { get; set; }
    }
}
