#include "pch.h"
#include "PDFDocForm.h"
#include "RDUILib.PDFDocForm.g.cpp"
#include "PDFDocFont.h"
#include "PDFDocImage.h"
#include "PDFDocGState.h"
#include "PDFResForm.h"
#include "PDFResFont.h"
#include "PDFResImage.h"
#include "PDFResGState.h"
#include "PDFPageContent.h"

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace winrt::RDUILib::implementation
{
    winrt::RDUILib::PDFResForm PDFDocForm::AddResForm(winrt::RDUILib::PDFDocForm sub)
    {
        int64_t hand = (int64_t)PDF_Document_addFormResForm(m_doc, m_hand, (PDF_DOC_FORM)sub.Handle());
        return (!hand) ? nullptr : winrt::make<implementation::PDFResForm>(hand);
    }
    winrt::RDUILib::PDFResFont PDFDocForm::AddResFont(winrt::RDUILib::PDFDocFont font)
    {
        int64_t hand = (int64_t)PDF_Document_addFormResFont(m_doc, m_hand, (PDF_DOC_FONT)font.Handle());
        return (!hand) ? nullptr : winrt::make<implementation::PDFResFont>(hand);
    }
    winrt::RDUILib::PDFResGState PDFDocForm::AddResGState(winrt::RDUILib::PDFDocGState gs)
    {
        int64_t hand = (int64_t)PDF_Document_addFormResGState(m_doc, m_hand, (PDF_DOC_GSTATE)gs.Handle());
        return (!hand) ? nullptr : winrt::make<implementation::PDFResGState>(hand);
    }
    winrt::RDUILib::PDFResImage PDFDocForm::AddResImage(winrt::RDUILib::PDFDocImage img)
    {
        int64_t hand = (int64_t)PDF_Document_addFormResImage(m_doc, m_hand, (PDF_DOC_IMAGE)img.Handle());
        return (!hand) ? nullptr : winrt::make<implementation::PDFResImage>(hand);
    }
    void PDFDocForm::SetContent(winrt::RDUILib::PDFPageContent content, float x, float y, float w, float h)
    {
        PDF_Document_setFormContent(m_doc, m_hand, x, y, w, h, (PDF_PAGECONTENT)content.Handle());
    }
}
