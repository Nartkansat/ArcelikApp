using ArcelikApp.Models;

namespace ArcelikApp.Excel.Mapping.Profiles
{
    /// <summary>
    /// Çamaşır Makinesi Excel dosyası kolon mapping profili.
    /// Genellikle Bulaşık Makinesi ile aynı yapıdadır.
    /// </summary>
    public static class CamasirMakinesiMappingProfile
    {
        public static ColumnMappingProfile Get() => new()
        {
            FileTypeName = "Çamaşır Makinesi",
            HeaderRow    = 2,
            DataStartRow = 3,

            FieldToColumnHeader = new System.Collections.Generic.Dictionary<string, string>(),

            FieldToColumnIndex = new System.Collections.Generic.Dictionary<string, int>
            {
                // --- Ürün Bilgileri ---
                { nameof(WhiteGoodsProduct.ProductCode),       1  },  // A
                { nameof(WhiteGoodsProduct.ProductName),       2  },  // B

                // --- Toptan Fiyatlar ---
                { nameof(WhiteGoodsProduct.CashPrice),         3  },  // C
                { nameof(WhiteGoodsProduct.WholesalePrice30),  4  },  // D
                { nameof(WhiteGoodsProduct.WholesalePrice60),  5  },  // E
                { nameof(WhiteGoodsProduct.WholesalePrice90),  6  },  // F
                { nameof(WhiteGoodsProduct.WholesalePrice120), 7  },  // G

                // --- Tavsiye Edilen Perakende (Kampanya) Fiyatları ---
                { nameof(WhiteGoodsProduct.PromoCashPrice),    8  },  // H
                { nameof(WhiteGoodsProduct.PromoInstall1x2),   9  },  // I
                { nameof(WhiteGoodsProduct.PromoInstall1x4),   10 },  // J
                { nameof(WhiteGoodsProduct.PromoInstall1x8),   11 },  // K
            }
        };
    }
}
