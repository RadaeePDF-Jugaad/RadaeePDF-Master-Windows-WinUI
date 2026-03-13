using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RDUILib;
using RadaeeWinUI.Models;
using Windows.UI;

namespace RadaeeWinUI.Services
{
    public class AnnotationManager : IAnnotationManager
    {
        public async Task<List<AnnotationData>> GetAnnotationsOnPageAsync(PDFPage page, int pageIndex)
        {
            return await Task.Run(() =>
            {
                var annotations = new List<AnnotationData>();
                try
                {
                    // Note: Annotation enumeration would require proper API access
                    // For now, return empty list until we can properly access annotation count
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting annotations: {ex.Message}");
                }
                return annotations;
            });
        }

        public async Task<PDFAnnot?> AddTextNoteAsync(PDFPage page, float x, float y, string content)
        {
            return await Task.Run(() =>
            {
                try
                {
                    bool success = page.AddAnnotTextNote(x, y);
                    if (success)
                    {
                        // Get the newly added annotation at the position
                        PDFAnnot? annot = page.GetAnnot(x, y);
                        if (annot != null && !string.IsNullOrEmpty(content))
                        {
                            annot.PopupText = content;
                        }
                        return annot;
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding text note: {ex.Message}");
                    return null;
                }
            });
        }

        public async Task<PDFAnnot?> AddHighlightAsync(PDFPage page, int charIndex1, int charIndex2, Color color)
        {
            return await Task.Run((Func<PDFAnnot?>)(() =>
            {
                try
                {
                    uint colorValue = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
                    page.AddAnnotMarkup(charIndex1, charIndex2, colorValue, 0);
                    // Note: Cannot retrieve markup annotation without proper API access
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding highlight: {ex.Message}");
                    return null;
                }
            }));
        }

        public async Task<PDFAnnot?> AddUnderlineAsync(PDFPage page, int charIndex1, int charIndex2, Color color)
        {
            return await Task.Run((Func<PDFAnnot?>)(() =>
            {
                try
                {
                    uint colorValue = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
                    page.AddAnnotMarkup(charIndex1, charIndex2, colorValue, 1);
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding underline: {ex.Message}");
                    return null;
                }
            }));
        }

        public async Task<PDFAnnot?> AddStrikeoutAsync(PDFPage page, int charIndex1, int charIndex2, Color color)
        {
            return await Task.Run((Func<PDFAnnot?>)(() =>
            {
                try
                {
                    uint colorValue = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
                    page.AddAnnotMarkup(charIndex1, charIndex2, colorValue, 2);
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding strikeout: {ex.Message}");
                    return null;
                }
            }));
        }

        public async Task<PDFAnnot?> AddRectangleAsync(PDFPage page, float x, float y, float width, float height, float strokeWidth, Color strokeColor, Color fillColor)
        {
            return await Task.Run(() =>
            {
                try
                {
                    RDRect rect = new RDRect();
                    rect.left = x;
                    rect.bottom = y;
                    rect.right = x + width;
                    rect.top = y + height;

                    uint strokeColorValue = (uint)((strokeColor.A << 24) | (strokeColor.R << 16) | (strokeColor.G << 8) | strokeColor.B);
                    uint fillColorValue = (uint)((fillColor.A << 24) | (fillColor.R << 16) | (fillColor.G << 8) | fillColor.B);

                    bool success = page.AddAnnotRect(rect, strokeWidth, strokeColorValue, fillColorValue);
                    if (success)
                    {
                        // Get annotation at center of rectangle
                        float centerX = x + width / 2;
                        float centerY = y + height / 2;
                        return page.GetAnnot(centerX, centerY);
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding rectangle: {ex.Message}");
                    return null;
                }
            });
        }

        public async Task<PDFAnnot?> AddEllipseAsync(PDFPage page, float x, float y, float width, float height, float strokeWidth, Color strokeColor, Color fillColor)
        {
            return await Task.Run(() =>
            {
                try
                {
                    RDRect rect = new RDRect();
                    rect.left = x;
                    rect.bottom = y;
                    rect.right = x + width;
                    rect.top = y + height;

                    uint strokeColorValue = (uint)((strokeColor.A << 24) | (strokeColor.R << 16) | (strokeColor.G << 8) | strokeColor.B);
                    uint fillColorValue = (uint)((fillColor.A << 24) | (fillColor.R << 16) | (fillColor.G << 8) | fillColor.B);

                    bool success = page.AddAnnotEllipse(rect, strokeWidth, strokeColorValue, fillColorValue);
                    if (success)
                    {
                        float centerX = x + width / 2;
                        float centerY = y + height / 2;
                        return page.GetAnnot(centerX, centerY);
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding ellipse: {ex.Message}");
                    return null;
                }
            });
        }

        public async Task<PDFAnnot?> AddLineAsync(PDFPage page, float x1, float y1, float x2, float y2, int style1, int style2, float strokeWidth, Color strokeColor, Color fillColor)
        {
            return await Task.Run(() =>
            {
                try
                {
                    uint strokeColorValue = (uint)((strokeColor.A << 24) | (strokeColor.R << 16) | (strokeColor.G << 8) | strokeColor.B);
                    uint fillColorValue = (uint)((fillColor.A << 24) | (fillColor.R << 16) | (fillColor.G << 8) | fillColor.B);

                    bool success = page.AddAnnotLine(x1, y1, x2, y2, style1, style2, strokeWidth, strokeColorValue, fillColorValue);
                    if (success)
                    {
                        float centerX = (x1 + x2) / 2;
                        float centerY = (y1 + y2) / 2;
                        return page.GetAnnot(centerX, centerY);
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding line: {ex.Message}");
                    return null;
                }
            });
        }

        public async Task<bool> AddInkAsync(PDFPage page, RDInk ink, float strokeWidth, Color strokeColor)
        {
            return await Task.Run(() =>
            {
                try
                {
                    uint colorValue = (uint)((strokeColor.A << 24) | (strokeColor.R << 16) | (strokeColor.G << 8) | strokeColor.B);
                    return page.AddAnnotInk(ink);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding ink: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> UpdateAnnotationAsync(PDFAnnot annot, AnnotationData data)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(data.Content))
                    {
                        annot.PopupText = data.Content;
                    }

                    if (data.Width > 0 && data.Height > 0)
                    {
                        RDRect rect = new RDRect();
                        rect.left = data.X;
                        rect.bottom = data.Y;
                        rect.right = data.X + data.Width;
                        rect.top = data.Y + data.Height;
                        annot.Rect = rect;
                    }

                    if (data.StrokeWidth > 0)
                    {
                        annot.StrokeWidth = data.StrokeWidth;
                    }

                    uint strokeColorValue = (uint)((data.StrokeColor.A << 24) | (data.StrokeColor.R << 16) | (data.StrokeColor.G << 8) | data.StrokeColor.B);
                    uint fillColorValue = (uint)((data.FillColor.A << 24) | (data.FillColor.R << 16) | (data.FillColor.G << 8) | data.FillColor.B);
                    
                    annot.StrokeColor = (int)strokeColorValue;
                    annot.FillColor = (int)fillColorValue;
                    annot.Locked = data.IsLocked;

                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating annotation: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> DeleteAnnotationAsync(PDFAnnot annot)
        {
            return await Task.Run(() =>
            {
                try
                {
                    annot.RemoveFromPage();
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting annotation: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<PDFAnnot?> GetAnnotationAtAsync(PDFPage page, float x, float y)
        {
            return await Task.Run(() =>
            {
                try
                {
                    return page.GetAnnot(x, y);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting annotation at position: {ex.Message}");
                    return null;
                }
            });
        }

        public async Task<byte[]?> ExportAnnotationAsync(PDFAnnot annot)
        {
            return await Task.Run(() =>
            {
                try
                {
                    return annot.Export();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error exporting annotation: {ex.Message}");
                    return null;
                }
            });
        }

        public async Task<bool> ImportAnnotationAsync(PDFPage page, float x, float y, float width, float height, byte[] data)
        {
            return await Task.Run(() =>
            {
                try
                {
                    RDRect rect = new RDRect();
                    rect.left = x;
                    rect.bottom = y;
                    rect.right = x + width;
                    rect.top = y + height;

                    page.ImportAnnot(rect, data, data.Length);
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error importing annotation: {ex.Message}");
                    return false;
                }
            });
        }

        private AnnotationType MapAnnotationType(int type)
        {
            return type switch
            {
                0 => AnnotationType.TextNote,
                8 => AnnotationType.Highlight,
                9 => AnnotationType.Underline,
                10 => AnnotationType.Strikeout,
                11 => AnnotationType.Squiggly,
                4 => AnnotationType.Rectangle,
                3 => AnnotationType.Ellipse,
                2 => AnnotationType.Line,
                15 => AnnotationType.Ink,
                _ => AnnotationType.None
            };
        }
    }
}
