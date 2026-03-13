using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RDUILib;
using RadaeeWinUI.Models;
using Windows.Storage;
using Windows.Storage.Streams;

namespace RadaeeWinUI.Services
{
    public class DocumentManager : IDocumentManager
    {
        public async Task<PDFDoc?> OpenDocumentAsync(StorageFile file, string password = "")
        {
            try
            {
                IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                PDFDoc doc = new PDFDoc();
                int ret = doc.Open(stream, password);
                
                if (ret == 0)
                {
                    return doc;
                }
                else if (ret == -2)
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening document: {ex.Message}");
                return null;
            }
        }

        public void CloseDocument(PDFDoc? doc)
        {
            if (doc != null && doc.IsOpened)
            {
                /*if (doc.CanSave)
                    doc.Save();*/
                doc.Close();
            }
        }

        public DocumentInfo? GetDocumentInfo(PDFDoc? doc)
        {
            if (doc == null || !doc.IsOpened)
                return null;

            return new DocumentInfo
            {
                PageCount = doc.PageCount,
                IsEncrypted = false,
                IsOpened = doc.IsOpened
            };
        }

        public Task<Dictionary<string, string>> GetMetadataAsync(PDFDoc? doc)
        {
            var metadata = new Dictionary<string, string>();
            
            if (doc == null || !doc.IsOpened)
                return Task.FromResult(metadata);

            try
            {
                string xmp = doc.XMP;
                if (!string.IsNullOrEmpty(xmp))
                {
                    metadata["XMP"] = xmp;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting metadata: {ex.Message}");
            }

            return Task.FromResult(metadata);
        }
    }
}
