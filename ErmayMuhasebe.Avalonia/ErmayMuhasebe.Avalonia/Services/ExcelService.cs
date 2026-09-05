using ClosedXML.Excel;
using ErmayMuhasebe.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Avalonia.Services
{
    public class ExcelService : IExcelService
    {
        public async Task<byte[]> ExportListToMemoryAsync<T>(IEnumerable<T> data, string sheetName = "Sayfa1")
        {
            return await Task.Run(() =>
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(sheetName);
                    worksheet.Cell(1, 1).InsertTable(data);
                    worksheet.Columns().AdjustToContents();

                    using (var ms = new MemoryStream())
                    {
                        workbook.SaveAs(ms);
                        return ms.ToArray();
                    }
                }
            });
        }

        public async Task<byte[]> ExportStyledListToMemoryAsync<T>(IEnumerable<T> data, string title, string sheetName = "Sayfa1")
        {
            return await Task.Run(() =>
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(sheetName);
                    
                    worksheet.Cell(1, 1).Value = title;
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1e3a8a");
                    
                    var table = worksheet.Cell(3, 1).InsertTable(data);
                    table.Theme = XLTableTheme.TableStyleMedium2;
                    
                    worksheet.Columns().AdjustToContents();

                    using (var ms = new MemoryStream())
                    {
                        workbook.SaveAs(ms);
                        return ms.ToArray();
                    }
                }
            });
        }

        public async Task ExportListToExcelAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Sayfa1")
        {
            var bytes = await ExportListToMemoryAsync(data, sheetName);
            await File.WriteAllBytesAsync(filePath, bytes);
        }

        public async Task<List<T>> ReadExcelAsync<T>(Stream stream) where T : class, new()
        {
            return await Task.Run(() =>
            {
                var list = new List<T>();
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RowsUsed().ToList();
                    if (rows.Count <= 1) return list;

                    var headerRow = rows[0];
                    var dataRows = rows.Skip(1);

                    var properties = typeof(T).GetProperties();
                    var headerMap = new Dictionary<int, System.Reflection.PropertyInfo>();

                    // Map headers to properties with smart matching (spaces, Turkish chars, case insensitive, aliases)
                    foreach (var cell in headerRow.CellsUsed())
                    {
                        var rawHeader = cell.GetString()?.Trim() ?? "";
                        if (string.IsNullOrWhiteSpace(rawHeader)) continue;

                        var normHeader = NormalizeKey(rawHeader);
                        var colNum = cell.Address.ColumnNumber;

                        // 1. Direct property name match
                        var prop = Array.Find(properties, p => p.CanWrite && (
                            string.Equals(p.Name, rawHeader, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(NormalizeKey(p.Name), normHeader, StringComparison.OrdinalIgnoreCase)
                        ));

                        // 2. Alias mapping for stock / cari fields
                        if (prop == null)
                        {
                            prop = FindPropertyByAlias(properties, normHeader);
                        }

                        if (prop != null && prop.CanWrite)
                        {
                            headerMap[colNum] = prop;
                        }
                    }

                    foreach (var row in dataRows)
                    {
                        var item = new T();
                        bool hasData = false;
                        foreach (var entry in headerMap)
                        {
                            var cell = row.Cell(entry.Key);
                            if (cell != null && !cell.IsEmpty())
                            {
                                try 
                                {
                                    var val = ParseCellValue(cell, entry.Value.PropertyType);
                                    if (val != null)
                                    {
                                        entry.Value.SetValue(item, val);
                                        hasData = true;
                                    }
                                }
                                catch { }
                            }
                        }
                        if (hasData) list.Add(item);
                    }
                }
                return list;
            });
        }

        private static string NormalizeKey(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            return text.Trim().ToLowerInvariant()
                .Replace("ı", "i")
                .Replace("ğ", "g")
                .Replace("ü", "u")
                .Replace("ş", "s")
                .Replace("ö", "o")
                .Replace("ç", "c")
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace(".", "")
                .Replace(":", "")
                .Replace("%", "");
        }

        private static System.Reflection.PropertyInfo? FindPropertyByAlias(System.Reflection.PropertyInfo[] properties, string normHeader)
        {
            // Stock aliases
            if (normHeader is "stokkodu" or "stokkod" or "kod" or "urunkodu" or "urunno" or "skodu" or "malzemekodu")
                return Array.Find(properties, p => p.Name.Equals("StokKodu", StringComparison.OrdinalIgnoreCase));

            if (normHeader is "stokadi" or "stokad" or "urunadi" or "urunad" or "ad" or "adi" or "malzemeadi" or "urun")
                return Array.Find(properties, p => p.Name.Equals("StokAdi", StringComparison.OrdinalIgnoreCase));

            if (normHeader is "birim" or "birimi" or "unit")
                return Array.Find(properties, p => p.Name.Equals("Birim", StringComparison.OrdinalIgnoreCase));

            if (normHeader is "kategori" or "grup" or "grubu" or "stokgrubu" or "category" or "stokkategori")
                return Array.Find(properties, p => p.Name.Equals("Kategori", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("Grup", StringComparison.OrdinalIgnoreCase));

            if (normHeader is "alisfiyati" or "alisfiyat" or "alis" or "alisfiy" or "fiyat1")
                return Array.Find(properties, p => p.Name.Equals("AlisFiyati", StringComparison.OrdinalIgnoreCase));

            if (normHeader is "satisfiyati" or "satisfiyat" or "satis" or "satisfiy" or "fiyat" or "fiyat2")
                return Array.Find(properties, p => p.Name.Equals("SatisFiyati", StringComparison.OrdinalIgnoreCase));

            if (normHeader is "kdv" or "kdvorani" or "vergi" or "tax" or "vat")
                return Array.Find(properties, p => p.Name.Equals("KDV", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("KdvOrani", StringComparison.OrdinalIgnoreCase));

            if (normHeader is "miktar" or "acilisbakiyesi" or "acilisbakiye" or "adet" or "stokmiktari" or "bakiye" or "stok")
                return Array.Find(properties, p => p.Name.Equals("Miktar", StringComparison.OrdinalIgnoreCase));

            if (normHeader is "minseviye" or "kritikseviye" or "minimumseviye" or "minmiktar" or "kritikstok")
                return Array.Find(properties, p => p.Name.Equals("MinSeviye", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("KritikSeviye", StringComparison.OrdinalIgnoreCase));

            if (normHeader is "aciklama" or "not" or "aciklamalar" or "description")
                return Array.Find(properties, p => p.Name.Equals("Aciklama", StringComparison.OrdinalIgnoreCase));

            if (normHeader is "barkod" or "barcode" or "barkodno")
                return Array.Find(properties, p => p.Name.Equals("Barkod", StringComparison.OrdinalIgnoreCase));

            return null;
        }

        private static object? ParseCellValue(IXLCell cell, Type targetType)
        {
            var propType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            var raw = cell.GetString()?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                if (cell.DataType == XLDataType.Number) raw = cell.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
                else if (cell.DataType == XLDataType.Boolean) raw = cell.GetBoolean().ToString();
                else if (cell.DataType == XLDataType.DateTime) raw = cell.GetDateTime().ToString("o");
                else return propType == typeof(string) ? "" : (propType.IsValueType ? Activator.CreateInstance(propType) : null);
            }

            if (propType == typeof(string)) return raw;

            if (propType == typeof(decimal))
            {
                if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dec)) return dec;
                if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("tr-TR"), out dec)) return dec;
                return 0m;
            }

            if (propType == typeof(double))
            {
                if (double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dbl)) return dbl;
                if (double.TryParse(raw, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("tr-TR"), out dbl)) return dbl;
                return 0.0;
            }

            if (propType == typeof(int))
            {
                if (int.TryParse(raw, out var num)) return num;
                if (double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dbl) ||
                    double.TryParse(raw, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("tr-TR"), out dbl))
                {
                    return (int)dbl;
                }
                return 0;
            }

            if (propType == typeof(long))
            {
                if (long.TryParse(raw, out var num)) return num;
                if (double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dbl)) return (long)dbl;
                return 0L;
            }

            if (propType == typeof(DateTime))
            {
                if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
                if (DateTime.TryParse(raw, new System.Globalization.CultureInfo("tr-TR"), System.Globalization.DateTimeStyles.None, out var dt)) return dt;
                if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt)) return dt;
                return DateTime.Now;
            }

            if (propType == typeof(bool))
            {
                if (bool.TryParse(raw, out var b)) return b;
                if (raw == "1" || raw.Equals("evet", StringComparison.OrdinalIgnoreCase) || raw.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }

            return Convert.ChangeType(raw, propType);
        }
    }
}
