using ArcelikApp.Excel.Mapping;
using ArcelikApp.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ArcelikApp.Excel.Processors
{
    public class KeaExcelProcessor
    {
        private readonly CultureInfo _trCulture = new CultureInfo("tr-TR");

        public List<KeaProduct> Process(
            ExcelWorksheet worksheet,
            ColumnMappingProfile profile,
            int uploadedFileId)
        {
            var results = new List<KeaProduct>();
            int rowCount = worksheet.Dimension?.Rows ?? 0;

            for (int row = profile.DataStartRow; row <= rowCount; row++)
            {
                var product = new KeaProduct
                {
                    ExcelFileType  = profile.FileTypeName,
                    UploadedFileId = uploadedFileId,

                    ProductCode    = GetText(worksheet, row, profile, nameof(KeaProduct.ProductCode)),
                    ProductName    = GetText(worksheet, row, profile, nameof(KeaProduct.ProductName)),
                    Description    = GetText(worksheet, row, profile, nameof(KeaProduct.Description)),

                    CashPrice         = GetDecimal(worksheet, row, profile, nameof(KeaProduct.CashPrice)),
                    WholesalePrice30  = GetDecimal(worksheet, row, profile, nameof(KeaProduct.WholesalePrice30)),
                    WholesalePrice60  = GetDecimal(worksheet, row, profile, nameof(KeaProduct.WholesalePrice60)),
                    WholesalePrice90  = GetDecimal(worksheet, row, profile, nameof(KeaProduct.WholesalePrice90)),
                    WholesalePrice120 = GetDecimal(worksheet, row, profile, nameof(KeaProduct.WholesalePrice120)),

                    PromoCashPrice    = GetDecimal(worksheet, row, profile, nameof(KeaProduct.PromoCashPrice)),
                    PromoInstall1x2   = GetDecimal(worksheet, row, profile, nameof(KeaProduct.PromoInstall1x2)),
                    PromoInstall1x4   = GetDecimal(worksheet, row, profile, nameof(KeaProduct.PromoInstall1x4)),
                    PromoInstall1x8   = GetDecimal(worksheet, row, profile, nameof(KeaProduct.PromoInstall1x8)),
                };

                // SKU boşsa atla (bölüm başlıkları vb.)
                if (string.IsNullOrWhiteSpace(product.ProductCode))
                    continue;

                results.Add(product);
            }

            // --- Tekilleştirme Mantığı (Deduplication) ---
            // Kullanıcı: "model adı kısmında tekrarlayabiliyor teke indirmek gerekiyor"
            // Burada ProductName'e göre gruplayıp ilkini alıyoruz.
            var distinctResults = results
                .GroupBy(p => p.ProductName)
                .Select(g => g.First())
                .ToList();

            return distinctResults;
        }

        private string GetText(ExcelWorksheet ws, int row, ColumnMappingProfile profile, string fieldName)
        {
            if (profile.FieldToColumnIndex.TryGetValue(fieldName, out int col))
                return ws.Cells[row, col].Text?.Trim() ?? string.Empty;
            return string.Empty;
        }

        private decimal? GetDecimal(ExcelWorksheet ws, int row, ColumnMappingProfile profile, string fieldName)
        {
            if (!profile.FieldToColumnIndex.TryGetValue(fieldName, out int col))
                return null;

            var cell = ws.Cells[row, col];
            if (cell.Value == null) return null;

            if (cell.Value is double d)  return (decimal)d;
            if (cell.Value is decimal m) return m;
            if (cell.Value is int i)     return (decimal)i;

            string text = (cell.Text ?? string.Empty)
                .ToUpper().Replace("TL", "").Replace("₺", "").Replace(" ", "").Trim();

            if (string.IsNullOrEmpty(text)) return null;

            if (decimal.TryParse(text, NumberStyles.Any, _trCulture, out decimal result))
                return result;

            return null;
        }
    }
}
