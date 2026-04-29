using System.Collections.Generic;

namespace ArcelikApp.Excel.Mapping
{
    /// <summary>
    /// Bir Excel dosya tipinin hangi kolonlarını hangi DB alanlarına eşleyeceğini tanımlar.
    /// </summary>
    public class ColumnMappingProfile
    {
        /// <summary>
        /// Excel dosya tipinin adı. Örn: "Ankastre", "Soğutucu"
        /// </summary>
        public string FileTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Header (başlık) satırının indeksi. Genellikle 1.
        /// </summary>
        public int HeaderRow { get; set; } = 1;

        /// <summary>
        /// Verinin başladığı satır indeksi. Genellikle 2.
        /// </summary>
        public int DataStartRow { get; set; } = 2;

        /// <summary>
        /// DB model property adı → Excel kolon başlığı eşlemesi (benzersiz başlıklar için).
        /// Örn: { "ProductCode" → "Ürün Kodu" }
        /// </summary>
        public Dictionary<string, string> FieldToColumnHeader { get; set; } = new();

        /// <summary>
        /// DB model property adı → Excel kolon numarası eşlemesi (tekrar eden başlıklar için).
        /// Kolon numaraları 1'den başlar (A=1, B=2, ...).
        /// FieldToColumnIndex, FieldToColumnHeader'a göre önceliklidir.
        /// Örn: { "Installment2Down" → 5 }  // E sütunu
        /// </summary>
        public Dictionary<string, int> FieldToColumnIndex { get; set; } = new();
    }
}
