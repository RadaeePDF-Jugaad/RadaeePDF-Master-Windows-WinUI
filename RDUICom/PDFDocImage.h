#pragma once

#include "RDUILib.PDFDocImage.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct PDFDocImage : PDFDocImageT<PDFDocImage>
    {
        PDFDocImage(int64_t hand)
        {
            m_hand = (PDF_DOC_IMAGE)hand;
        }
        int64_t Handle() { return (int64_t)m_hand; }
        PDF_DOC_IMAGE m_hand;
    };
}

namespace winrt::RDUILib::factory_implementation
{
    struct PDFDocImage : PDFDocImageT<PDFDocImage, implementation::PDFDocImage>
    {
    };
}
