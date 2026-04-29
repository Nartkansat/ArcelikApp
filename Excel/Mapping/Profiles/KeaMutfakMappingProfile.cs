using ArcelikApp.Models;

namespace ArcelikApp.Excel.Mapping.Profiles
{
    /// <summary>
    /// KEA Mutfak Excel dosyası kolon mapping profili.
    /// </summary>
    public static class KeaMutfakMappingProfile
    {
        public static ColumnMappingProfile Get() => new()
        {
            FileTypeName = "Mutfak",
            HeaderRow    = 2,
            DataStartRow = 3,

            FieldToColumnIndex = new System.Collections.Generic.Dictionary<string, int>
            {
                { nameof(KeaProduct.ProductCode),       1  },  // A -> SKU
                { nameof(KeaProduct.ProductName),       2  },  // B -> Model Adı
                { nameof(KeaProduct.Description),       3  },  // C -> Açıklama
                
                { nameof(KeaProduct.CashPrice),         4  },  // D -> Peşin Fiyat
                { nameof(KeaProduct.WholesalePrice30),  5  },  // E -> 30 Gün
                { nameof(KeaProduct.WholesalePrice60),  6  },  // F -> 60 Gün
                { nameof(KeaProduct.WholesalePrice90),  7  },  // G -> 90 Gün
                { nameof(KeaProduct.WholesalePrice120), 8  },  // H -> 120 Gün

                { nameof(KeaProduct.PromoCashPrice),    9  },  // I -> Promo Peşin
                { nameof(KeaProduct.PromoInstall1x2),   10 },  // J -> Promo 1+2
                { nameof(KeaProduct.PromoInstall1x4),   11 },  // K -> Promo 1+4
                { nameof(KeaProduct.PromoInstall1x8),   12 }   // L -> Promo 1+8
            }
        };
    }
}
