using System;
using Microsoft.UI.Input;

namespace RadaeeWinUI.Models
{
    public class PageChangedEventArgs : EventArgs
    {
        public int OldPageIndex { get; set; }
        public int NewPageIndex { get; set; }
    }

    public class PDFPointerEventArgs : EventArgs
    {
        public int PageIndex { get; set; }
        public float ScreenX { get; set; }
        public float ScreenY { get; set; }
        public float PDFX { get; set; }
        public float PDFY { get; set; }
        public PointerPoint PointerPoint { get; set; }
    }
}
