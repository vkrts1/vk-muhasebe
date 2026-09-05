using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Services
{
    public interface IExcelService
    {
        Task<byte[]> ExportListToMemoryAsync<T>(IEnumerable<T> data, string sheetName = "Sayfa1");
        Task<byte[]> ExportStyledListToMemoryAsync<T>(IEnumerable<T> data, string title, string sheetName = "Sayfa1");
        Task ExportListToExcelAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Sayfa1");
        Task<List<T>> ReadExcelAsync<T>(Stream stream) where T : class, new();
    }
}
