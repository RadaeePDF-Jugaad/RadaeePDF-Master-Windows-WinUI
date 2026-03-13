#pragma once

#include "RDUILib.PDFDocTag.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct PDFDocTag : PDFDocTagT<PDFDocTag>
    {
        PDFDocTag(int64_t hand)
        {
            m_hand = (PDF_TAG)hand;
        }
        int64_t Handle() { return (int64_t)m_hand; }
        PDF_TAG m_hand;
    };
}

namespace winrt::RDUILib::factory_implementation
{
    struct PDFDocTag : PDFDocTagT<PDFDocTag, implementation::PDFDocTag>
    {
    };
}
