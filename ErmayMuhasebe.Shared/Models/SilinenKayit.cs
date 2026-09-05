using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class SilinenKayit
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int KayitId { get; set; } // Missing
        public string? TableName { get; set; }
        public string? Aciklama { get; set; } // Missing
        public DateTime SilinmeTarihi { get; set; } // Missing
        public string? KayitIcerik { get; set; } // Missing (JSON data)
        public string? TabloAdi { get; set; }
        public string? SilenKullanici { get; set; }
    }
}
