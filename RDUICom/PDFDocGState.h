#pragma once

#include "RDUILib.PDFDocGState.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct PDFDocGState : PDFDocGStateT<PDFDocGState>
    {
        PDFDocGState(int64_t doc, int64_t hand)
        {
            m_doc = (PDF_DOC)doc;
            m_hand = (PDF_DOC_GSTATE)hand;
        }
        int64_t Handle() { return (int64_t)m_hand; }
        void SetFillAlpha(int32_t alpha)
        {
            PDF_Document_setGStateFillAlpha(m_doc, m_hand, alpha);
        }
        void SetStrokeAlpha(int32_t alpha)
        {
            PDF_Document_setGStateStrokeAlpha(m_doc, m_hand, alpha);
        }
        void SetStrokeDash(winrt::array_view<float const> dash, float phase)
        {
            PDF_Document_setGStateStrokeDash(m_doc, m_hand, dash.data(), dash.size(), phase);
        }
        void SetBlendMode(int32_t bmode)
        {
            PDF_Document_setGStateBlendMode(m_doc, m_hand, bmode);
        }
        PDF_DOC m_doc;
        PDF_DOC_GSTATE m_hand;
    };
}

namespace winrt::RDUILib::factory_implementation
{
    struct PDFDocGState : PDFDocGStateT<PDFDocGState, implementation::PDFDocGState>
    {
    };
}
