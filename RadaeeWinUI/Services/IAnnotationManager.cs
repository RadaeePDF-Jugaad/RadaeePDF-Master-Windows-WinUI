using System.Collections.Generic;
using System.Threading.Tasks;
using RDUILib;
using RadaeeWinUI.Models;
using Windows.UI;

namespace RadaeeWinUI.Services
{
    public interface IAnnotationManager
    {
        Task<List<AnnotationData>> GetAnnotationsOnPageAsync(PDFPage page, int pageIndex);
        Task<PDFAnnot?> AddTextNoteAsync(PDFPage page, float x, float y, string content);
        Task<PDFAnnot?> AddHighlightAsync(PDFPage page, int charIndex1, int charIndex2, Color color);
        Task<PDFAnnot?> AddUnderlineAsync(PDFPage page, int charIndex1, int charIndex2, Color color);
        Task<PDFAnnot?> AddStrikeoutAsync(PDFPage page, int charIndex1, int charIndex2, Color color);
        Task<PDFAnnot?> AddRectangleAsync(PDFPage page, float x, float y, float width, float height, float strokeWidth, Color strokeColor, Color fillColor);
        Task<PDFAnnot?> AddEllipseAsync(PDFPage page, float x, float y, float width, float height, float strokeWidth, Color strokeColor, Color fillColor);
        Task<PDFAnnot?> AddLineAsync(PDFPage page, float x1, float y1, float x2, float y2, int style1, int style2, float strokeWidth, Color strokeColor, Color fillColor);
        Task<bool> AddInkAsync(PDFPage page, RDInk ink, float strokeWidth, Color strokeColor);
        Task<bool> UpdateAnnotationAsync(PDFAnnot annot, AnnotationData data);
        Task<bool> DeleteAnnotationAsync(PDFAnnot annot);
        Task<PDFAnnot?> GetAnnotationAtAsync(PDFPage page, float x, float y);
        Task<byte[]?> ExportAnnotationAsync(PDFAnnot annot);
        Task<bool> ImportAnnotationAsync(PDFPage page, float x, float y, float width, float height, byte[] data);
    }
}
