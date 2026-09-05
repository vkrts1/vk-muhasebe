using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class TurkiyeSehirler
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string? SehirAdi { get; set; }
        public int PlakaKodu { get; set; }
    }
}
