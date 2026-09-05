namespace ErmayMuhasebe.Models
{
    // Fatura çıktı düzeni kişiselleştirme ayarları.
    // RTDB'de "FaturaTasarimi/1" düğümünde saklanır ve tek PDF motoruna yansıtılır.
    public class FaturaTasarimi
    {
        [SQLite.PrimaryKey]
        public int Id { get; set; } = 1;
        public string TenantId { get; set; } = "default";

        public bool ShowLogo { get; set; } = true;
        public bool ShowBirim { get; set; } = true;
        public bool ShowKdv { get; set; } = true;
        public bool ShowAraToplam { get; set; } = true;
        public bool ShowAciklama { get; set; } = true;

        public string BaslikText { get; set; } = "SATIŞ FATURASI";
        public string AltBilgi { get; set; } = "Mal teslimi ve fatura bedeli ödemesi hakkındadır.";

        // Kağıt boyutu/yönü (FirmaProfili değerleriyle senkron tutulur)
        public string FaturaSize { get; set; } = "A4";
        public string FaturaOrientation { get; set; } = "Dikey";

        // Marka/başlık rengi (hex)
        public string MarkaRengi { get; set; } = "#2563eb";
    }
}
