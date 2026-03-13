using System.Collections.Generic;
using System.Threading.Tasks;
using RDUILib;
using RadaeeWinUI.Models;
using Windows.Storage;

namespace RadaeeWinUI.Services
{
    public interface IDocumentManager
    {
        Task<PDFDoc?> OpenDocumentAsync(StorageFile file, string password = "");
        void CloseDocument(PDFDoc? doc);
        DocumentInfo? GetDocumentInfo(PDFDoc? doc);
        Task<Dictionary<string, string>> GetMetadataAsync(PDFDoc? doc);
    }
}
