#include "pch.h"
#include "PDFOutline.h"
#include "RDUILib.PDFOutline.g.cpp"

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace winrt::RDUILib::implementation
{
    winrt::RDUILib::PDFOutline PDFOutline::Next()
    {
        PDF_OUTLINE hand = PDF_Document_getOutlineNext(m_doc, m_hand);
        if (!hand) return nullptr;
        return winrt::make<implementation::PDFOutline>((int64_t)m_doc, (int64_t)hand);
    }
    winrt::RDUILib::PDFOutline PDFOutline::Child()
    {
        PDF_OUTLINE hand = PDF_Document_getOutlineChild(m_doc, m_hand);
        if (!hand) return nullptr;
        return winrt::make<implementation::PDFOutline>((int64_t)m_doc, (int64_t)hand);
    }
    bool PDFOutline::AddNext(winrt::hstring label, int32_t dest, float y)
    {
        return PDF_Document_addOutlineNext(m_doc, m_hand, label.c_str(), dest, y);
    }
    bool PDFOutline::AddChild(winrt::hstring label, int32_t dest, float y)
    {
        return PDF_Document_addOutlineChild(m_doc, m_hand, label.c_str(), dest, y);
    }
    bool PDFOutline::RemoveFromDoc()
    {
        bool ret = PDF_Document_removeOutline(m_doc, m_hand);
        m_doc = NULL;
        m_hand = NULL;
        return ret;
    }
    winrt::hstring PDFOutline::label()
    {
        return PDF_Document_getOutlineLabel(m_doc, m_hand);
    }
    int32_t PDFOutline::dest()
    {
        return PDF_Document_getOutlineDest(m_doc, m_hand);
    }
}
