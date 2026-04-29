using ArcelikApp.Models;

namespace ArcelikApp.Excel.Mapping.Profiles
{
    /// <summary>
    /// TV (Televizyon) Excel dosyası kolon mapping profili.
    /// Fotoğraftaki yapıya göre: 1. satır başlık, 2. satır veri başlangıcı.
    /// </summary>
    public static class TvMappingProfile
    {
        public static ColumnMappingProfile Get() => new()
        {
            FileTypeName = "Televizyon",
            HeaderRow    = 1,
            DataStartRow = 2,

            FieldToColumnHeader = new System.Collections.Generic.Dictionary<string, string>(),

            FieldToColumnIndex = new System.Collections.Generic.Dictionary<string, int>
            {
                // --- Ürün Bilgileri ---
                { nameof(WhiteGoodsProduct.ProductCode),       1  },  // A -> Arçelik SKU
                { nameof(WhiteGoodsProduct.ProductName),       2  },  // B -> Arçelik Model

                // --- Toptan Fiyatlar ---
                { nameof(WhiteGoodsProduct.CashPrice),         3  },  // C -> Peşin
                { nameof(WhiteGoodsProduct.WholesalePrice30),  4  },  // D -> 30 gün
                { nameof(WhiteGoodsProduct.WholesalePrice60),  5  },  // E -> 60 gün
                { nameof(WhiteGoodsProduct.WholesalePrice90),  6  },  // F -> 90 gün

                // --- Kampanya / Taksit Fiyatları (1+3 Taksit) ---
                { nameof(WhiteGoodsProduct.Installment4Total), 10 },  // J -> 1+3 Toplam
                { nameof(WhiteGoodsProduct.Installment4Down),  11 }   // K -> Peşinat
            }
        };
    }
}
