#include "pch.h"
#include "PDFPage.h"
#include "RDUILib.PDFPage.g.cpp"
#include "PDFDocFont.h"
#include "PDFResFont.h"
#include "PDFDocForm.h"
#include "PDFResForm.h"
#include "PDFDocGState.h"
#include "PDFResGState.h"
#include "PDFDocImage.h"
#include "PDFResImage.h"
#include "PDFAnnot.h"
#include "PDFFinder.h"
#include "PDFEditNode.h"
#include "PDFPageTag.h"

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace winrt::RDUILib::implementation
{
    RDUILib::PDFResFont PDFPage::AddResFont(RDUILib::PDFDocFont font)
    {
        int64_t pf = (int64_t)PDF_Page_addResFont(m_page, (PDF_DOC_FONT)font.Handle());
        return (pf) ? winrt::make<implementation::PDFResFont>(pf) : nullptr;
    }
    RDUILib::PDFResForm PDFPage::AddResForm(RDUILib::PDFDocForm form)
    {
        int64_t pf = (int64_t)PDF_Page_addResForm(m_page, (PDF_DOC_FORM)form.Handle());
        return (pf) ? winrt::make<implementation::PDFResForm>(pf) : nullptr;
    }
    RDUILib::PDFResGState PDFPage::AddResGState(RDUILib::PDFDocGState gs)
    {
        int64_t pf = (int64_t)PDF_Page_addResGState(m_page, (PDF_DOC_GSTATE)gs.Handle());
        return (pf) ? winrt::make<implementation::PDFResGState>(pf) : nullptr;
    }
    RDUILib::PDFResImage PDFPage::AddResImage(RDUILib::PDFDocImage image)
    {
        int64_t pf = (int64_t)PDF_Page_addResImage(m_page, (PDF_DOC_IMAGE)image.Handle());
        return (pf) ? winrt::make<implementation::PDFResImage>(pf) : nullptr;
    }
    RDUILib::PDFAnnot PDFPage::GetAnnot(float x, float y)
    {
        PDF_ANNOT annot = PDF_Page_getAnnotFromPoint(m_page, x, y);
        if (annot) return winrt::make<implementation::PDFAnnot>((int64_t)m_page, (int64_t)annot);
        else return nullptr;
    }
    RDUILib::PDFAnnot PDFPage::GetAnnot(int index)
    {
        PDF_ANNOT annot = PDF_Page_getAnnot(m_page, index);
        if (annot) return winrt::make<implementation::PDFAnnot>((int64_t)m_page, (int64_t)annot);
        else return nullptr;
    }
    RDUILib::PDFFinder PDFPage::GetFinder(winrt::hstring key, bool match_case, bool whole_word)
    {
        PDF_FINDER find = PDF_Page_findOpenW(m_page, key.c_str(), match_case, whole_word);
        if (find) return winrt::make<implementation::PDFFinder>((int64_t)find);
        else return nullptr;
    }
    RDUILib::PDFFinder PDFPage::GetFinder(winrt::hstring key, bool match_case, bool whole_word, bool skip_blanks)
    {
        PDF_FINDER find = PDF_Page_findOpen2W(m_page, key.c_str(), match_case, whole_word, skip_blanks);
        if (find) return winrt::make<implementation::PDFFinder>((int64_t)find);
        else return nullptr;
    }
    RDUILib::PDFEditNode PDFPage::GetPGEditorNode(float pdfx, float pdfy)
    {
        PDF_EDITNODE node = PDF_Page_getPGEditorNode2(m_page, pdfx, pdfy);
        if (!node) return nullptr;
        return winrt::make <implementation::PDFEditNode>((int64_t)node);
    }
    RDUILib::PDFEditNode PDFPage::GetPGEditorNode(int index)
    {
        PDF_EDITNODE node = PDF_Page_getPGEditorNode1(m_page, index);
        if (!node) return nullptr;
        return winrt::make <implementation::PDFEditNode>((int64_t)node);
    }
    RDUILib::PDFPageTag PDFPage::NewTagBlock(RDUILib::PDFDocTag parent, winrt::hstring stag)
    {
        PDF_TAG ret = NULL;
        if (parent) ret = PDF_Page_newTagBlock(m_page, (PDF_TAG)parent.Handle(), stag);
        else ret = PDF_Page_newTagBlock(m_page, NULL, stag);
        if (!ret) return nullptr;
        return winrt::make <implementation::PDFPageTag>((int64_t)ret);
    }
}
