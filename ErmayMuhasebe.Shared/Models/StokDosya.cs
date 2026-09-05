using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class StokDosya
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int StokId { get; set; }
        public string? DosyaAdi { get; set; }
        public string? DosyaYolu { get; set; }
        public string? DosyaTuru { get; set; }
        public string? Boyut { get; set; }
        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
        public string? Aciklama { get; set; }

        // Aliases for Web UI compatibility
        [Ignore] public string Ad { get => DosyaAdi ?? ""; set => DosyaAdi = value; }
        [Ignore] public string Tur { get => DosyaTuru ?? ""; set => DosyaTuru = value; }
        [Ignore] public DateTime Tarih { get => EklenmeTarihi; set => EklenmeTarihi = value; }
    }
}
