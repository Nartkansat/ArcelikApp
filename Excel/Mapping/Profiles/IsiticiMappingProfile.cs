using ArcelikApp.Models;

namespace ArcelikApp.Excel.Mapping.Profiles
{
    /// <summary>
    /// Isıtıcı Aletler (Termosifon, Soba vb.) Excel dosyası kolon mapping profili.
    /// Fotoğraftaki yapıya göre: 
    /// Row 1 = Başlıklar
    /// Row 2+ = Veriler (Bölüm başlıkları SKU boş olduğu için atlanır)
    /// </summary>
    public static class IsiticiMappingProfile
    {
        public static ColumnMappingProfile Get() => new()
        {
            FileTypeName = "Isıtıcı Aletler",
            HeaderRow    = 1,
            DataStartRow = 2,

            FieldToColumnHeader = new System.Collections.Generic.Dictionary<string, string>(),

            FieldToColumnIndex = new System.Collections.Generic.Dictionary<string, int>
            {
                // --- Ürün Bilgileri ---
                { nameof(WhiteGoodsProduct.ProductCode),       3  },  // C -> SKU
                { nameof(WhiteGoodsProduct.ProductName),       4  },  // D -> Model

                // --- Toptan Fiyatlar ---
                { nameof(WhiteGoodsProduct.CashPrice),         6  },  // F -> NAKİT
                { nameof(WhiteGoodsProduct.WholesalePrice30),  7  },  // G -> 30 GÜN VADE
                { nameof(WhiteGoodsProduct.WholesalePrice60),  8  },  // H -> 60 GÜN VADE
                { nameof(WhiteGoodsProduct.WholesalePrice90),  9  },  // I -> 90 GÜN VADE
                { nameof(WhiteGoodsProduct.WholesalePrice120), 10 }   // J -> 120 GÜN VADE
            }
        };
    }
}
