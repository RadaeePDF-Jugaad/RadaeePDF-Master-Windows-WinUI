#pragma once

#include "RDUILib.PDFDocFont.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct PDFDocFont : PDFDocFontT<PDFDocFont>
    {
        PDFDocFont(int64_t doc, int64_t hand)
        {
            m_doc = (PDF_DOC)doc;
            m_hand = (PDF_DOC_FONT)hand;
        }
        float Ascent()
        {
            return PDF_Document_getFontAscent(m_doc, m_hand);
        }
        float Descent()
        {
            return PDF_Document_getFontDescent(m_doc, m_hand);
        }
        int64_t Handle() { return (int64_t)m_hand; }
        PDF_DOC m_doc;
        PDF_DOC_FONT m_hand;
    };
}

namespace winrt::RDUILib::factory_implementation
{
    struct PDFDocFont : PDFDocFontT<PDFDocFont, implementation::PDFDocFont>
    {
    };
}
