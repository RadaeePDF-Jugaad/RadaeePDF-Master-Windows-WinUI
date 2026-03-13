#pragma once

#include "RDUILib.PDFOutline.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct PDFOutline : PDFOutlineT<PDFOutline>
    {
        PDFOutline(int64_t doc, int64_t hand)
        {
            m_doc = (PDF_DOC)doc;
            m_hand = (PDF_OUTLINE)hand;
        }
        winrt::RDUILib::PDFOutline Next();
        winrt::RDUILib::PDFOutline Child();
        bool AddNext(winrt::hstring label, int32_t dest, float y);
        bool AddChild(winrt::hstring label, int32_t dest, float y);
        bool RemoveFromDoc();
        winrt::hstring label();
        int32_t dest();
    private:
        PDF_DOC m_doc;
        PDF_OUTLINE m_hand;
    };
}

namespace winrt::RDUILib::factory_implementation
{
    struct PDFOutline : PDFOutlineT<PDFOutline, implementation::PDFOutline>
    {
    };
}
