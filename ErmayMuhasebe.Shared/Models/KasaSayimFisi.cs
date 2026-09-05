using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class KasaSayimFisi
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string? FisNo { get; set; } // Missing
        public DateTime SayimTarihi { get; set; } // Missing
        public string? KasaAdi { get; set; } // Missing
        public string? SayimYapan { get; set; } // Missing
        public decimal SistemBakiye { get; set; } // Missing
        public decimal FizikiBakiye { get; set; } // Missing
        public bool Onaylandi { get; set; } // Missing
        public string? Aciklama { get; set; }
    }
}
