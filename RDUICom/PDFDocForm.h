#pragma once

#include "RDUILib.PDFDocForm.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct PDFDocForm : PDFDocFormT<PDFDocForm>
    {
        PDFDocForm(int64_t doc, int64_t hand)
        {
            m_doc = (PDF_DOC)doc;
            m_hand = (PDF_DOC_FORM)hand;
        }
        int64_t Handle() { return (int64_t)m_hand; }
        winrt::RDUILib::PDFResForm AddResForm(winrt::RDUILib::PDFDocForm sub);
        winrt::RDUILib::PDFResFont AddResFont(winrt::RDUILib::PDFDocFont font);
        winrt::RDUILib::PDFResGState AddResGState(winrt::RDUILib::PDFDocGState gs);
        winrt::RDUILib::PDFResImage AddResImage(winrt::RDUILib::PDFDocImage img);
        void SetContent(winrt::RDUILib::PDFPageContent content, float x, float y, float w, float h);
        void SetTransparency(bool isolate, bool knockout)
        {
            PDF_Document_setFormTransparency(m_doc, m_hand, isolate, knockout);
        }
        PDF_DOC m_doc;
        PDF_DOC_FORM m_hand;
    };
}

namespace winrt::RDUILib::factory_implementation
{
    struct PDFDocForm : PDFDocFormT<PDFDocForm, implementation::PDFDocForm>
    {
    };
}
