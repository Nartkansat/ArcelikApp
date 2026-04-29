using ArcelikApp.Models;

namespace ArcelikApp.Excel.Mapping.Profiles
{
    /// <summary>
    /// Kurutma Makinesi Excel dosyası kolon mapping profili.
    /// </summary>
    public static class KurutmaMappingProfile
    {
        public static ColumnMappingProfile Get() => new()
        {
            FileTypeName = "Kurutma Makinesi",
            HeaderRow    = 2,
            DataStartRow = 3,

            FieldToColumnHeader = new System.Collections.Generic.Dictionary<string, string>(),

            FieldToColumnIndex = new System.Collections.Generic.Dictionary<string, int>
            {
                // --- Ürün Bilgileri ---
                { nameof(WhiteGoodsProduct.ProductCode),       1  },  // A -> ÜRÜN KODU
                { nameof(WhiteGoodsProduct.ProductName),       2  },  // B -> ÜRÜN TANIM

                // --- Toptan Fiyatlar ---
                { nameof(WhiteGoodsProduct.CashPrice),         3  },  // C -> Peşin Fiyat
                { nameof(WhiteGoodsProduct.WholesalePrice30),  4  },  // D -> 30 Gün
                { nameof(WhiteGoodsProduct.WholesalePrice60),  5  },  // E -> 60 Gün
                { nameof(WhiteGoodsProduct.WholesalePrice90),  6  },  // F -> 90 Gün
                { nameof(WhiteGoodsProduct.WholesalePrice120), 7  },  // G -> 120 Gün

                // --- Tavsiye Edilen Perakende (Kampanya) Fiyatları ---
                { nameof(WhiteGoodsProduct.PromoCashPrice),    8  },  // H -> 2025 NİSAN PEŞİN
                { nameof(WhiteGoodsProduct.PromoInstall1x2),   9  },  // I -> 2025 NİSAN 1+2
                { nameof(WhiteGoodsProduct.PromoInstall1x4),   10 },  // J -> 2025 NİSAN 1+4
                { nameof(WhiteGoodsProduct.PromoInstall1x8),   11 },  // K -> 2025 NİSAN 1+8
            }
        };
    }
}
