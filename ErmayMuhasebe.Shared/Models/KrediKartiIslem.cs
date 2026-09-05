using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class KrediKartiIslem : ITenantEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string TenantId { get; set; } = "default";
        
        // Müşteri bilgileri (Kimden geldi)
        public int MusteriId { get; set; }
        public string? MusteriUnvan { get; set; }
        
        public DateTime Tarih { get; set; } = DateTime.Now;
        public DateTime? VadeTarihi { get; set; }
        public decimal Tutar { get; set; }
        
        public string? Banka { get; set; }
        public string? KartNo { get; set; }
        public string? OnayKodu { get; set; }
        
        // Mevcut durum: Portföyde, Tedarikçiye Verildi, Tahsil Edildi, İptal
        public string? Durum { get; set; } = "Portföyde";
        
        // Yönlendirme bilgileri (Kime verildi)
        public int? YonlendirilenCariId { get; set; }
        public string? YonlendirilenCariUnvan { get; set; }
        public DateTime? YonlendirmeTarihi { get; set; }
        
        public string? Aciklama { get; set; }
        public string? IslemTuru { get; set; } // Added
        public string? SlipDosyaYolu { get; set; } // Added
        public bool IsDeleted { get; set; }
    }
}
