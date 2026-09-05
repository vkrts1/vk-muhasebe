namespace ErmayMuhasebe.Models
{
    public class GlobalSearchResult
    {
        public string Title { get; set; } = "";
        public string Type { get; set; } = ""; // Cari, Stok, Fatura, Siparis
        public int Id { get; set; }
        public string Icon { get; set; } = ""; // Avalonia Fluent Icon Name (e.g. "Person", "Box")
    }
}
