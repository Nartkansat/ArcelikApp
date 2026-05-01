using ArcelikApp.Models;

namespace ArcelikApp.Excel.Mapping.Profiles
{
    /// <summary>
    /// Klima Excel dosyası kolon mapping profili.
    /// </summary>
    public static class KlimaMappingProfile
    {
        public static ColumnMappingProfile Get() => new()
        {
            FileTypeName = "Klima",
            HeaderRow    = 1,
            DataStartRow = 2,

            FieldToColumnHeader = new System.Collections.Generic.Dictionary<string, string>(),

            FieldToColumnIndex = new System.Collections.Generic.Dictionary<string, int>
            {
                // --- Ürün Bilgileri ---
                { nameof(WhiteGoodsProduct.ProductCode),       1  },  // A -> İç Ünite SKU
                { nameof(WhiteGoodsProduct.Description),       2  },  // B -> Dış Ünite SKU
                { nameof(WhiteGoodsProduct.ProductName),       3  },  // C -> Klimalar

                // --- Toptan Fiyatlar (Red line sonrası) ---
                { nameof(WhiteGoodsProduct.WholesalePrice60),   4  },  // D -> F+30 Gün (Y060)
                { nameof(WhiteGoodsProduct.WholesalePrice90),   5  },  // E -> F+60 Gün (Y90)
                { nameof(WhiteGoodsProduct.WholesalePrice120),  6  },  // F -> F+90 Gün (Y120)
            }
        };
    }
}
