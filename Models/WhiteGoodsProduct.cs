namespace ArcelikApp.Models
{
    public class WhiteGoodsProduct
    {
        public int Id { get; set; }

        // --- Ürün Kimliği ---
        public string ExcelFileType      { get; set; } = string.Empty; // "Ankastre", "Sogutucu" vb.
        public string ProductCode        { get; set; } = string.Empty;
        public string ProductName        { get; set; } = string.Empty;
        public string Description        { get; set; } = string.Empty;
        public string EnergyClass        { get; set; } = string.Empty;

        // --- Peşin / Toptan Fiyatlar ---
        public decimal? CashPrice         { get; set; }  // Peşin / KDV Dahil Peşin Toptan
        public decimal? WholesalePrice30  { get; set; }  // 30 Günlük Toptan
        public decimal? WholesalePrice60  { get; set; }  // 60 Günlük Toptan
        public decimal? WholesalePrice90  { get; set; }  // 90 Günlük Toptan
        public decimal? WholesalePrice120 { get; set; }  // 120 Günlük Toptan

        // --- Taksit Fiyatları ---
        public decimal? Installment2Down  { get; set; }  // 2 Taksit Peşinat
        public decimal? Installment2Total { get; set; }  // 2 Taksit Toplam
        public decimal? Installment4Down  { get; set; }  // 4 Taksit Peşinat
        public decimal? Installment4Total { get; set; }  // 4 Taksit Toplam
        public decimal? Installment8Down  { get; set; }  // 8 Taksit Peşinat
        public decimal? Installment8Total { get; set; }  // 8 Taksit Toplam

        // --- Kampanya / Promo Fiyatları ---
        public decimal? PromoCashPrice    { get; set; }  // 2026 Nisan Peşin vb.
        public decimal? PromoInstall1x2   { get; set; }  // 2026 Nisan 1+2
        public decimal? PromoInstall1x4   { get; set; }  // 2026 Nisan 1+4
        public decimal? PromoInstall1x8   { get; set; }  // 2026 Nisan 1+8

        // --- İlişki ---
        public int UploadedFileId { get; set; }
        public UploadedFile? UploadedFile { get; set; }
    }
}
