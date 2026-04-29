using ArcelikApp.Models;

namespace ArcelikApp.Excel.Mapping.Profiles
{
    /// <summary>
    /// Soğutucu (Buzdolabı) Excel dosyası kolon mapping profili.
    ///
    /// Gerçek Excel yapısı (fotoğraftan tespit edildi):
    ///  Row 1 = Merged kategori başlıkları (ARÇELİK, TOPTAN FİYATLAR, TAVSİYE EDİLEN...)
    ///  Row 2 = Gerçek kolon başlıkları
    ///  Row 3 = "BUZDOLABI" bölüm başlığı → ProductCode boş olduğu için otomatik atlanır
    ///  Row 4+ = Veri satırları
    ///
    ///  Sütunlar:
    ///   1  = ÜRÜN KODU
    ///   2  = ÜRÜN TANIMI
    ///   3  = Peşin Fiyat
    ///   4  = 30 Gün
    ///   5  = 60 Gün
    ///   6  = 90 Gün
    ///   7  = 120 Gün
    ///   8  = 2026 NİSAN PEŞİN
    ///   9  = 2026 NİSAN 1+2
    ///  10  = 2026 NİSAN 1+4
    ///  11  = 2026 NİSAN 1+8
    /// </summary>
    public static class SogutucuMappingProfile
    {
        public static ColumnMappingProfile Get() => new()
        {
            FileTypeName = "Soğutucu",
            HeaderRow    = 2,       // Gerçek başlıklar 2. satırda
            DataStartRow = 3,       // "BUZDOLABI" gibi bölüm başlıkları ProductCode boş → atlanır

            // Türkçe ı/İ farkı OrdinalIgnoreCase'de sorun çıkarır — tüm alanlar index ile
            FieldToColumnHeader = new System.Collections.Generic.Dictionary<string, string>(),

            FieldToColumnIndex = new System.Collections.Generic.Dictionary<string, int>
            {
                // --- Ürün Bilgileri ---
                { nameof(WhiteGoodsProduct.ProductCode),       1  },  // A  → ÜRÜN KODU
                { nameof(WhiteGoodsProduct.ProductName),       2  },  // B  → ÜRÜN TANIMI

                // --- Toptan Fiyatlar ---
                { nameof(WhiteGoodsProduct.CashPrice),         3  },  // C  → Peşin Fiyat
                { nameof(WhiteGoodsProduct.WholesalePrice30),  4  },  // D  → 30 Gün
                { nameof(WhiteGoodsProduct.WholesalePrice60),  5  },  // E  → 60 Gün
                { nameof(WhiteGoodsProduct.WholesalePrice90),  6  },  // F  → 90 Gün
                { nameof(WhiteGoodsProduct.WholesalePrice120), 7  },  // G  → 120 Gün

                // --- Tavsiye Edilen Perakende (Kampanya) Fiyatları ---
                { nameof(WhiteGoodsProduct.PromoCashPrice),    8  },  // H  → 2026 NİSAN PEŞİN
                { nameof(WhiteGoodsProduct.PromoInstall1x2),   9  },  // I  → 2026 NİSAN 1+2
                { nameof(WhiteGoodsProduct.PromoInstall1x4),   10 },  // J  → 2026 NİSAN 1+4
                { nameof(WhiteGoodsProduct.PromoInstall1x8),   11 },  // K  → 2026 NİSAN 1+8
            }
        };
    }
}
