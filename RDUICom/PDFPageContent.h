#pragma once

#include "RDUILib.PDFPageContent.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct PDFPageContent : PDFPageContentT<PDFPageContent>
    {
        PDFPageContent(int64_t hand)
        {
            m_hand = (PDF_IMPORTCTX)hand;
        }
        int64_t Handle() { return (int64_t)m_hand; }
        PDF_IMPORTCTX m_hand;
    };
}

namespace winrt::RDUILib::factory_implementation
{
    struct PDFPageContent : PDFPageContentT<PDFPageContent, implementation::PDFPageContent>
    {
    };
}
