namespace ArcelikApp.Models
{
    public class OlizCampaign
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string ProductGroup { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public decimal DiscountNetAmount { get; set; }
        public string CampaignStartDate { get; set; } = string.Empty;
        public string CampaignEndDate { get; set; } = string.Empty;
        public string LastTransportDate { get; set; } = string.Empty;
        public string LastBarcodeScanDate { get; set; } = string.Empty;
        public string CampaignCode { get; set; } = string.Empty;
        public string CampaignShortDescription { get; set; } = string.Empty;
        public string GeneralDescription { get; set; } = string.Empty;
        public int UploadedFileId { get; set; }
        public UploadedFile? UploadedFile { get; set; }
    }
}
