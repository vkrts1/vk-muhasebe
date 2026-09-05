using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class SmsGecmisi
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int CariId { get; set; } // Missing
        public string? Unvan { get; set; } // Missing
        public string? Telefon { get; set; } // Missing
        public string? Mesaj { get; set; } // Missing
        public DateTime Tarih { get; set; } = DateTime.Now;
        public bool Basarili { get; set; }
        public string? HataMesaji { get; set; } // Missing
        public string? Durum { get; set; }
    }
}
