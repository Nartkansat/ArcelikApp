namespace ArcelikApp.Models
{
    /// <summary>
    /// Maliyet hesabı sonuçlarını tutan tablo.
    /// WhiteGoodsProducts veya KeaProducts ile OlizCampaigns eşleştirilerek hesaplanır.
    /// </summary>
    public class CostCalculation
    {
        public int Id { get; set; }

        // --- Ürün Bilgisi ---
        public string ProductId { get; set; } = string.Empty;   // WhiteGoodsProducts/KeaProducts Id (referans)
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string SourceTable { get; set; } = string.Empty; // "WhiteGoods" veya "Kea"

        // --- Fiyat Baz Alınan Kolon ---
        /// <summary>Hangi toptan fiyat kolonu baz alındı (örn. "WholesalePrice60")</summary>
        public string PricePPSource { get; set; } = "WholesalePrice60";

        /// <summary>Baz alınan toptan fiyat değeri</summary>
        public decimal PricePP { get; set; }

        // --- Kampanya ---
        /// <summary>OlizCampaigns.DiscountNetAmount — kampanya yoksa 0</summary>
        public decimal PriceConversion { get; set; }

        /// <summary>PricePP - PriceConversion (maliyet fiyatı)</summary>
        public decimal PurchasePrice { get; set; }

        // --- Kart Fiyatı ---
        /// <summary>PurchasePrice üzerine eklenen yüzde (örn. 10 = %10)</summary>
        public decimal CardMarkupPercent { get; set; } = 10;

        /// <summary>PurchasePrice * (1 + CardMarkupPercent/100)</summary>
        public decimal CardPurchasePrice { get; set; }

        // --- Kampanya Bilgisi ---
        public string CampaingDate { get; set; } = string.Empty; // Kampanya dönemi açıklaması

        // --- Kayıt Zamanı ---
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
