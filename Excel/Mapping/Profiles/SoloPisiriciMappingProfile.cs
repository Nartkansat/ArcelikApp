using ArcelikApp.Models;

namespace ArcelikApp.Excel.Mapping.Profiles
{
    /// <summary>
    /// Solo Pişiriciler (Mikrodalgalar vb.) Excel dosyası kolon mapping profili.
    /// </summary>
    public static class SoloPisiriciMappingProfile
    {
        public static ColumnMappingProfile Get() => new()
        {
            FileTypeName = "Solo Pişirici",
            HeaderRow    = 2,
            DataStartRow = 3,

            FieldToColumnHeader = new System.Collections.Generic.Dictionary<string, string>(),

            FieldToColumnIndex = new System.Collections.Generic.Dictionary<string, int>
            {
                // --- Ürün Bilgileri ---
                { nameof(WhiteGoodsProduct.ProductCode),       1  },  // A
                { nameof(WhiteGoodsProduct.ProductName),       2  },  // B
                { nameof(WhiteGoodsProduct.Description),       3  },  // C

                // --- Taksitler (1+2, 1+4, 1+8) ---
                { nameof(WhiteGoodsProduct.Installment2Down),  5  },  // E
                { nameof(WhiteGoodsProduct.Installment2Total), 7  },  // G
                { nameof(WhiteGoodsProduct.Installment4Down),  8  },  // H
                { nameof(WhiteGoodsProduct.Installment4Total), 10 },  // J
                { nameof(WhiteGoodsProduct.Installment8Down),  11 },  // K
                { nameof(WhiteGoodsProduct.Installment8Total), 13 },  // M

                // --- Toptan Fiyatlar ---
                { nameof(WhiteGoodsProduct.CashPrice),         14 },  // N -> KDV Dahil Peşin Toptan
                { nameof(WhiteGoodsProduct.WholesalePrice30),  15 },  // O
                { nameof(WhiteGoodsProduct.WholesalePrice60),  16 },  // P
                { nameof(WhiteGoodsProduct.WholesalePrice90),  17 },  // Q
                { nameof(WhiteGoodsProduct.WholesalePrice120), 18 }   // R
            }
        };
    }
}
