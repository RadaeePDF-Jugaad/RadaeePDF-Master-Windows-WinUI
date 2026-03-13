#pragma once

#include "RDUILib.PDFSign.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct PDFSign : PDFSignT<PDFSign>
    {
        PDFSign(int64_t hand)
        {
            m_hand = (PDF_SIGN)hand;
        }
        int64_t Handle() { return (int64_t)m_hand; }
        PDF_SIGN m_hand;
    };
}

namespace winrt::RDUILib::factory_implementation
{
    struct PDFSign : PDFSignT<PDFSign, implementation::PDFSign>
    {
    };
}
