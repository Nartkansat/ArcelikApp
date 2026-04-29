namespace ArcelikApp.Models
{
    public class KeaProduct
    {
        public int Id { get; set; }
        public string ExcelFileType { get; set; } = string.Empty; // "Mutfak", "SupurgeUtu"
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // --- Fiyatlar ---
        public decimal? CashPrice { get; set; }
        public decimal? WholesalePrice30 { get; set; }
        public decimal? WholesalePrice60 { get; set; }
        public decimal? WholesalePrice90 { get; set; }
        public decimal? WholesalePrice120 { get; set; }

        // --- Kampanya Fiyatları ---
        public decimal? PromoCashPrice { get; set; }
        public decimal? PromoInstall1x2 { get; set; }
        public decimal? PromoInstall1x4 { get; set; }
        public decimal? PromoInstall1x8 { get; set; }

        public int UploadedFileId { get; set; }
        public UploadedFile? UploadedFile { get; set; }
    }
}

