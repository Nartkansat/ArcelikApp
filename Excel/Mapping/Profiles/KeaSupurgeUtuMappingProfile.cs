using ArcelikApp.Models;

namespace ArcelikApp.Excel.Mapping.Profiles
{
    /// <summary>
    /// KEA Süpürge ve Ütü Excel dosyası kolon mapping profili.
    /// </summary>
    public static class KeaSupurgeUtuMappingProfile
    {
        public static ColumnMappingProfile Get() => new()
        {
            FileTypeName = "Süpürge ve Ütü",
            HeaderRow    = 2,
            DataStartRow = 3,

            FieldToColumnIndex = new System.Collections.Generic.Dictionary<string, int>
            {
                { nameof(KeaProduct.ProductCode),       1  },  // A -> SKU
                { nameof(KeaProduct.ProductName),       2  },  // B -> Model Adı
                // NOT: Bu dosyada açıklama sütunu yok, direkt fiyatlara geçiyor.
                
                { nameof(KeaProduct.CashPrice),         3  },  // C -> Peşin Fiyat
                { nameof(KeaProduct.WholesalePrice30),  4  },  // D -> 30 Gün
                { nameof(KeaProduct.WholesalePrice60),  5  },  // E -> 60 Gün
                { nameof(KeaProduct.WholesalePrice90),  6  },  // F -> 90 Gün
                { nameof(KeaProduct.WholesalePrice120), 7  },  // G -> 120 Gün

                { nameof(KeaProduct.PromoCashPrice),    8  },  // H -> Promo Peşin
                { nameof(KeaProduct.PromoInstall1x2),   9  },  // I -> Promo 1+2
                { nameof(KeaProduct.PromoInstall1x4),   10 },  // J -> Promo 1+4
                { nameof(KeaProduct.PromoInstall1x8),   11 }   // K -> Promo 1+8
            }
        };
    }
}
