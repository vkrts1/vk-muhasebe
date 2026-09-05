using SQLite;
using System;

namespace ErmayMuhasebe.Models
{
    public class MaliyetMerkeziDef
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string? Kod { get; set; }
        public string? Ad { get; set; }
        
        // Budget Fields
        public decimal PersonelButce { get; set; }
        public decimal GenelButce { get; set; }
        public decimal ToplamButce => PersonelButce + GenelButce;
        
        // Actual Fields
        public decimal PersonelGercek { get; set; }
        public decimal GenelGercek { get; set; }
        public decimal ToplamGercek => PersonelGercek + GenelGercek;
        
        // Diff Fields (Computed)
        public decimal PersonelFark => PersonelButce - PersonelGercek;
        public decimal GenelFark => GenelButce - GenelGercek;
        public decimal ToplamFark => ToplamButce - ToplamGercek;
        
        public double PersonelYuzde => PersonelButce == 0 ? 0 : (double)(PersonelGercek / PersonelButce) * 100;
        public double GenelYuzde => GenelButce == 0 ? 0 : (double)(GenelGercek / GenelButce) * 100;
        public double ToplamYuzde => ToplamButce == 0 ? 0 : (double)(ToplamGercek / ToplamButce) * 100;
    }
}
