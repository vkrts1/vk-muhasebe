using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class HaftalikSatisHedefi
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int Yil { get; set; }
        public int Hafta { get; set; }
        public decimal HedefTutari { get; set; }
    }
}
