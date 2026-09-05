using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class EftIslem : ITenantEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string TenantId { get; set; } = "default";
        
        public int MusteriId { get; set; }
        public string? MusteriUnvan { get; set; }
        
        public DateTime Tarih { get; set; } = DateTime.Now;
        public decimal Tutar { get; set; }
        
        public string? Banka { get; set; }
        public int BankaId { get; set; }
        public string? HesapNo { get; set; }
        public string? DekontNo { get; set; }
        public string EvrakNo { get; set; } = "";
        
        public string? Durum { get; set; } = "Portföyde";
        
        public int? YonlendirilenCariId { get; set; }
        public string? YonlendirilenCariUnvan { get; set; }
        public DateTime? YonlendirmeTarihi { get; set; }
        
        public string? Aciklama { get; set; }
        public string? IslemTuru { get; set; } // Added
        public string? DekontPath { get; set; } 
        public bool IsDeleted { get; set; }
    }
}
