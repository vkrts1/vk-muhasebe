using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class CariDosya
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int CariId { get; set; }
        public string? CariUnvan { get; set; }
        public string? DosyaAdi { get; set; } // Kullanıcının verdiği ad (Sözleşme vb)
        public string? DosyaYolu { get; set; } // Fiziksel yol
        public string? DosyaTuru { get; set; } // PDF, JPG vs.
        public string? Boyut { get; set; } // Örn: 1.2 MB
        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
        public string? Aciklama { get; set; }

        // Aliases for Web UI compatibility
        [Ignore] public string Ad { get => DosyaAdi ?? ""; set => DosyaAdi = value; }
        [Ignore] public string Tur { get => DosyaTuru ?? ""; set => DosyaTuru = value; }
        [Ignore] public DateTime Tarih { get => EklenmeTarihi; set => EklenmeTarihi = value; }
    }
}
