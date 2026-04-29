using ArcelikApp.Excel.Mapping;
using ArcelikApp.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ArcelikApp.Excel.Processors
{
    /// <summary>
    /// ColumnMappingProfile'a göre herhangi bir WhiteGoods Excel dosyasını okur.
    /// - Benzersiz başlıklar: FieldToColumnHeader (başlık adı ile kolon bulur)
    /// - Tekrar eden başlıklar: FieldToColumnIndex (sabit kolon numarası ile okur)
    /// </summary>
    public class WhiteGoodsExcelProcessor
    {
        private readonly CultureInfo _trCulture = new CultureInfo("tr-TR");

        public List<WhiteGoodsProduct> Process(
            ExcelWorksheet worksheet,
            ColumnMappingProfile profile,
            int uploadedFileId)
        {
            // 1. Header satırından kolon başlığı → indeks haritası oluştur
            var colIndex = BuildColumnIndex(worksheet, profile.HeaderRow);

            var results = new List<WhiteGoodsProduct>();
            int rowCount = worksheet.Dimension?.Rows ?? 0;

            for (int row = profile.DataStartRow; row <= rowCount; row++)
            {
                var product = new WhiteGoodsProduct
                {
                    ExcelFileType    = profile.FileTypeName,
                    UploadedFileId   = uploadedFileId,

                    ProductCode      = GetText(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.ProductCode)),
                    ProductName      = GetText(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.ProductName)),
                    Description      = GetText(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.Description)),
                    EnergyClass      = GetText(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.EnergyClass)),

                    CashPrice         = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.CashPrice)),
                    WholesalePrice30  = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.WholesalePrice30)),
                    WholesalePrice60  = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.WholesalePrice60)),
                    WholesalePrice90  = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.WholesalePrice90)),
                    WholesalePrice120 = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.WholesalePrice120)),

                    Installment2Down  = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.Installment2Down)),
                    Installment2Total = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.Installment2Total)),
                    Installment4Down  = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.Installment4Down)),
                    Installment4Total = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.Installment4Total)),
                    Installment8Down  = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.Installment8Down)),
                    Installment8Total = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.Installment8Total)),

                    PromoCashPrice    = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.PromoCashPrice)),
                    PromoInstall1x2   = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.PromoInstall1x2)),
                    PromoInstall1x4   = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.PromoInstall1x4)),
                    PromoInstall1x8   = GetDecimal(worksheet, row, colIndex, profile, nameof(WhiteGoodsProduct.PromoInstall1x8)),
                };

                // ★ FIX: Sadece ProductCode boşsa atla (bölüm başlığı satırlarını da yakalar)
                if (string.IsNullOrWhiteSpace(product.ProductCode))
                    continue;

                results.Add(product);
            }

            return results;
        }

        // ─── Yardımcı: Header satırından Dictionary<başlık, kolon no> üretir ───────────
        // NOT: Tekrar eden başlıklarda SADECE ilk oluşumu alır.
        //      Tekrar edenler için FieldToColumnIndex kullanın.
        private Dictionary<string, int> BuildColumnIndex(ExcelWorksheet ws, int headerRow)
        {
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int colCount = ws.Dimension?.Columns ?? 0;

            for (int col = 1; col <= colCount; col++)
            {
                string header = ws.Cells[headerRow, col].Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(header) && !index.ContainsKey(header))
                    index[header] = col;
            }
            return index;
        }

        // ─── Yardımcı: Kolon numarası çözümle (Index > Header önceliği) ──────────────
        private int? ResolveColumn(
            Dictionary<string, int> colIndex,
            ColumnMappingProfile profile,
            string fieldName)
        {
            // 1. FieldToColumnIndex varsa önce onu kullan (tekrar eden başlıklar için)
            if (profile.FieldToColumnIndex.TryGetValue(fieldName, out int fixedCol))
                return fixedCol;

            // 2. FieldToColumnHeader ile başlık adından bul
            if (profile.FieldToColumnHeader.TryGetValue(fieldName, out var header))
                if (colIndex.TryGetValue(header, out int namedCol))
                    return namedCol;

            return null; // Bu alan bu profilde tanımlı değil
        }

        // ─── Yardımcı: Metinsel değer okur ──────────────────────────────────────────────
        private string GetText(
            ExcelWorksheet ws, int row,
            Dictionary<string, int> colIndex,
            ColumnMappingProfile profile,
            string fieldName)
        {
            int? col = ResolveColumn(colIndex, profile, fieldName);
            if (col == null) return string.Empty;
            return ws.Cells[row, col.Value].Text?.Trim() ?? string.Empty;
        }

        // ─── Yardımcı: Sayısal değer okur, bulunamazsa null döner ───────────────────────
        private decimal? GetDecimal(
            ExcelWorksheet ws, int row,
            Dictionary<string, int> colIndex,
            ColumnMappingProfile profile,
            string fieldName)
        {
            int? col = ResolveColumn(colIndex, profile, fieldName);
            if (col == null) return null;

            var cell = ws.Cells[row, col.Value];
            if (cell.Value == null) return null;

            // Native sayısal tipler
            if (cell.Value is double d)  return (decimal)d;
            if (cell.Value is decimal m) return m;
            if (cell.Value is int i)     return (decimal)i;

            // Metin temizleme: "5.000 TL" → "5000"
            string text = (cell.Text ?? string.Empty)
                .ToUpper()
                .Replace("TL", "")
                .Replace("₺",  "")
                .Replace(" ",  "")
                .Trim();

            if (string.IsNullOrEmpty(text)) return null;

            if (decimal.TryParse(text, NumberStyles.Any, _trCulture, out decimal result))
                return result;

            return null;
        }
    }
}
