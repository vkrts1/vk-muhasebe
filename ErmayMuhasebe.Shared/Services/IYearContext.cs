using System;

namespace ErmayMuhasebe.Services
{
    public interface IYearContext
    {
        int CurrentYear { get; set; }
    }

    public class YearContext : IYearContext
    {
        public int CurrentYear { get; set; } = DateTime.Now.Year;
    }
}
