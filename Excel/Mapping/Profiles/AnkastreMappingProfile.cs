using ArcelikApp.Models;

namespace ArcelikApp.Excel.Mapping.Profiles
{
    /// <summary>
    /// Ankastre Excel dosyası kolon mapping profili.
    ///
    /// Gerçek Excel yapısı (hata mesajından tespit edildi):
    ///  1  = (boş header) → Ürün Kodu
    ///  2  = (boş header) → Ürün Tanımı
    ///  3  = (boş header) → Açıklama
    ///  4  = (boş header) → Enerji Sınıfı
    ///  5  = (boş header) → (bilinmiyor — varsa ek alan)
    ///  6  = PEŞİNAT        (2 Taksit)
    ///  7  = 2 TAKSİT
    ///  8  = TOPLAM FİYAT   (2 Taksit)
    ///  9  = PEŞİNAT        (4 Taksit)
    ///  10 = 4 TAKSİT
    ///  11 = TOPLAM FİYAT   (4 Taksit)
    ///  12 = PEŞİNAT        (8 Taksit)
    ///  13 = 8 TAKSİT
    ///  14 = TOPLAM FİYAT   (8 Taksit)
    ///  15 = KDV DAHİL PEŞİN TOPTAN FİYAT
    ///  16 = KDV DAHİL 30 GÜNLÜK TOPTAN FİYAT
    ///  17 = KDV DAHİL 60 GÜNLÜK TOPTAN FİYAT
    ///  18 = KDV DAHİL 90 GÜNLÜK TOPTAN FİYAT
    ///  19 = KDV DAHİL 120 GÜNLÜK TOPTAN FİYAT
    ///
    /// ★ Sütun 1-5 header satırında boş olduğu için TÜM alanlar FieldToColumnIndex
    ///   ile sabit kolon numarasıyla okunuyor.
    /// </summary>
    public static class AnkastreMappingProfile
    {
        public static ColumnMappingProfile Get() => new()
        {
            FileTypeName = "Ankastre",
            HeaderRow    = 1,
            DataStartRow = 2, // Bölüm başlığı satırları (ANKASTRE FIRINLAR vb.) ProductCode boş olduğu için otomatik atlanır

            // Başlıklar boş/merged olduğundan header eşlemesi kullanılmıyor
            FieldToColumnHeader = new System.Collections.Generic.Dictionary<string, string>(),

            // TÜM alanlar sabit kolon numarası ile okunuyor
            FieldToColumnIndex = new System.Collections.Generic.Dictionary<string, int>
            {
                // --- Ürün Bilgileri (sütun 1-5, header'sız) ---
                { nameof(WhiteGoodsProduct.ProductCode),       1  },  // A
                { nameof(WhiteGoodsProduct.ProductName),       2  },  // B
                { nameof(WhiteGoodsProduct.Description),       3  },  // C
                { nameof(WhiteGoodsProduct.EnergyClass),       4  },  // D
                // Sütun 5 şu an kullanılmıyor — gerekirse ekleyin

                // --- 2 Taksit ---
                { nameof(WhiteGoodsProduct.Installment2Down),  6  },  // F  → PEŞİNAT  (2 Taksit)
                { nameof(WhiteGoodsProduct.Installment2Total), 8  },  // H  → TOPLAM FİYAT (2 Taksit)

                // --- 4 Taksit ---
                { nameof(WhiteGoodsProduct.Installment4Down),  9  },  // I  → PEŞİNAT  (4 Taksit)
                { nameof(WhiteGoodsProduct.Installment4Total), 11 },  // K  → TOPLAM FİYAT (4 Taksit)

                // --- 8 Taksit ---
                { nameof(WhiteGoodsProduct.Installment8Down),  12 },  // L  → PEŞİNAT  (8 Taksit)
                { nameof(WhiteGoodsProduct.Installment8Total), 14 },  // N  → TOPLAM FİYAT (8 Taksit)

                // --- Toptan Fiyatlar ---
                { nameof(WhiteGoodsProduct.CashPrice),         15 },  // O  → KDV DAHİL PEŞİN TOPTAN FİYAT
                { nameof(WhiteGoodsProduct.WholesalePrice30),  16 },  // P  → KDV DAHİL 30 GÜNLÜK
                { nameof(WhiteGoodsProduct.WholesalePrice60),  17 },  // Q  → KDV DAHİL 60 GÜNLÜK
                { nameof(WhiteGoodsProduct.WholesalePrice90),  18 },  // R  → KDV DAHİL 90 GÜNLÜK
                { nameof(WhiteGoodsProduct.WholesalePrice120), 19 },  // S  → KDV DAHİL 120 GÜNLÜK
            }
        };
    }
}
