using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class MusteriTakipKlasor : ITenantEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string TenantId { get; set; } = "default";
        public int CariId { get; set; }
        public string CariUnvan { get; set; } = string.Empty;
        public string? CariKod { get; set; }
        public string? Telefon { get; set; }
        public string? Yetkili { get; set; }
        public string? Etiket { get; set; } // Örn: "Sıcak Müşteri", "Teklif Verildi", "Önemli"
        public string Renk { get; set; } = "#3B82F6"; // Klasör renk teması
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
        public DateTime SonIslemTarihi { get; set; } = DateTime.Now;
        public string? Aciklama { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class MusteriTakipDetay : ITenantEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string TenantId { get; set; } = "default";
        public int KlasorId { get; set; }
        public int CariId { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string? Icerik { get; set; }
        public string Tip { get; set; } = "Not"; // "Not", "Gorusme", "Fiyat", "Gorsel"
        public decimal? FiyatBilgisi { get; set; }
        public string ParaBirimi { get; set; } = "₺";
        public string? DosyaYolu { get; set; }
        public string? GorselBase64 { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; }
    }
}
