using ArcelikApp.Models;

namespace ArcelikApp.Excel.Mapping.Profiles
{
    /// <summary>
    /// Havalandırma Excel dosyası kolon mapping profili.
    /// </summary>
    public static class HavalandirmaMappingProfile
    {
        public static ColumnMappingProfile Get() => new()
        {
            FileTypeName = "Havalandırma",
            HeaderRow    = 1,
            DataStartRow = 3,

            FieldToColumnIndex = new System.Collections.Generic.Dictionary<string, int>
            {
                { nameof(WhiteGoodsProduct.ProductCode),       3  },  // C -> SKU
                { nameof(WhiteGoodsProduct.ProductName),       4  },  // D -> Model
                { nameof(WhiteGoodsProduct.Description),       2  },  // B -> Ürün Tipi (Vantilatör vb.)
                
                { nameof(WhiteGoodsProduct.CashPrice),         5  },  // E -> NAKİT
                { nameof(WhiteGoodsProduct.WholesalePrice30),  6  },  // F -> 30 GÜN VADE
                { nameof(WhiteGoodsProduct.WholesalePrice60),  7  },  // G -> 60 GÜN VADE
                { nameof(WhiteGoodsProduct.WholesalePrice90),  8  },  // H -> 90 GÜN VADE
                { nameof(WhiteGoodsProduct.WholesalePrice120), 9  }   // I -> 120 GÜN VADE
            }
        };
    }
}
