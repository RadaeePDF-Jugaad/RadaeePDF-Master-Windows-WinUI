using System;
using Windows.UI;

namespace RadaeeWinUI.Models
{
    public class AnnotationData
    {
        public int PageIndex { get; set; }
        public AnnotationType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public Color StrokeColor { get; set; }
        public Color FillColor { get; set; }
        public float StrokeWidth { get; set; }
        public bool IsLocked { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int AnnotIndex { get; set; }
    }
}
