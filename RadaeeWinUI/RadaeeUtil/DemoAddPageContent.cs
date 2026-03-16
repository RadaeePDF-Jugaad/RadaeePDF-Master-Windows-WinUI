using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using RDUILib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace RadaeeWinUI.RadaeeUtil
{
    public sealed class DemoAddPageContent
    {
        /*
         * 
         */
        public bool AddTextPageContent(PDFDoc doc, int pageIndex, String text, String fontName, float fontSize, float pdfx, float pdfy)
        {
            PDFPage page = doc.GetPage(pageIndex);
            if (page == null)
                return false;

            page.ObjsStart();
            PDFDocFont dFont = doc.NewFontCID(fontName, 0x100);
            if (dFont == null)
            {
                //Cannot load font, try to load default.
                dFont = doc.NewFontCID("Arial", 0x100);
                if (dFont == null)
                    return false;
            }

            PDFResFont rFont = page.AddResFont(dFont);

            if (rFont == null)
                return false;

            PDFPageContent content = new PDFPageContent();
            content.TextBegin();
            content.TextSetFont(rFont, fontSize);
            content.TextMove(pdfx, pdfy);
            content.DrawText(text);
            content.TextEnd();
            bool r = page.AddContent(content, true);
            page.Close();

            return r;
        }

        public bool AddImagePageContent(PDFDoc doc, int pageIndex, WriteableBitmap image, bool hasAlpha, bool interpolate, float pdfx, float pdfy, float width, float height)
        {
            PDFDocImage docImage = doc.NewImage0(image, hasAlpha, interpolate);
            if (docImage == null)
                return false;

            return AddImagePageContent(doc, pageIndex, docImage, hasAlpha, interpolate, pdfx, pdfy, width, height);
        }

        public bool AddImagePageContent(PDFDoc doc, int pageIndex, SoftwareBitmap image, bool hasAlpha, bool interpolate, float pdfx, float pdfy, float width, float height)
        {
            PDFDocImage docImage = doc.NewImage1(image, hasAlpha, interpolate);
            if (docImage == null)
                return false;

            return AddImagePageContent(doc, pageIndex, docImage, hasAlpha, interpolate, pdfx, pdfy, width, height);
        }

        private bool AddImagePageContent(PDFDoc doc, int pageIndex, PDFDocImage docImage, bool hasAlpha, bool interpolate, float pdfx, float pdfy, float width, float height)
        {
            PDFPage page = doc.GetPage(pageIndex);
            if (page == null)
                return false;

            page.ObjsStart();

            PDFResImage rImage = page.AddResImage(docImage);

            if (rImage == null)
                return false;

            float pageHeight = doc.GetPageHeight(pageIndex);
            PDFPageContent content = new PDFPageContent();
            RDMatrix mat = new RDMatrix(width, height, pdfx, pageHeight - pdfy - height);
            content.GSSave();

            content.GSSetMatrix(mat);
            content.DrawImage(rImage);

            content.GSRestore();
            bool result = page.AddContent(content, true);
            page.Close();

            return result;
        }
    }
}
