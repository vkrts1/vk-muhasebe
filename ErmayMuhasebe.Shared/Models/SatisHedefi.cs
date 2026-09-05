using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class SatisHedefi
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int Yil { get; set; }
        public int Ay { get; set; }
        public decimal HedefTutari { get; set; }
        [Ignore]
        public decimal HedefTutar { get => HedefTutari; set => HedefTutari = value; } // Alias
    }
}
