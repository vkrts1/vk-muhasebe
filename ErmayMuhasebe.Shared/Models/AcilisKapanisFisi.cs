using SQLite;
using System;
using System.Collections.Generic;

namespace ErmayMuhasebe.Models
{
    public class AcilisKapanisFisi
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string? FisNo { get; set; }
        public string? FisTuru { get; set; } 
        public string? DonemYil { get; set; }
        public string? DonemAy { get; set; }
        public string? Aciklama { get; set; }
        public decimal ToplamBorc { get; set; }
        public decimal ToplamAlacak { get; set; }
        
        public DateTime Tarih { get; set; } = DateTime.Now;
        public DateTime FisTarihi { get => Tarih; set => Tarih = value; } // Alias
    }

    public class AcilisKapanisFisiDetay
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int FisId { get; set; }
        public int HesapId { get; set; } 
        public string? HesapTuru { get; set; } 
        public string? HesapKodu { get; set; } // Missing
        public string? HesapAdi { get; set; } // Missing
        public string? Aciklama { get; set; } // Missing
        public decimal Borc { get; set; }
        public decimal Alacak { get; set; }
    }
}
