using MiniExcelLibs.Attributes;
using System;

namespace ErmayMuhasebe.Models
{
    public class FaturaExcelDto
    {
        [ExcelColumnName("Fatura No")]
        public string? FaturaNo { get; set; }
        
        [ExcelColumnName("Tarih")]
        public string? Tarih { get; set; }
        
        [ExcelColumnName("Vade Tarihi")]
        public string? VadeTarihi { get; set; }
        
        [ExcelColumnName("Cari �nvan")]
        public string? CariUnvan { get; set; }
        
        [ExcelColumnName("T�r")]
        public string? Tur { get; set; }
        
        [ExcelColumnName("A�iklama")]
        public string? Aciklama { get; set; }
        
        [ExcelColumnName("Net Tutar")]
        public decimal AraToplam { get; set; }
        
        [ExcelColumnName("KDV")]
        public decimal ToplamKDV { get; set; }
        
        [ExcelColumnName("Genel Toplam")]
        public decimal GenelToplam { get; set; }
        
        [ExcelColumnName("�denen")]
        public decimal Odenen { get; set; }
        
        [ExcelColumnName("Kalan")]
        public decimal Kalan { get; set; }
        
        [ExcelColumnName("�deme Sekli")]
        public string? OdemeSekli { get; set; }
    }
}
