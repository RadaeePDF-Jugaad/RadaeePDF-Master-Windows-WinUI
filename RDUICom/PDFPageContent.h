#pragma once

#include "RDUILib.PDFPageContent.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct PDFPageContent : PDFPageContentT<PDFPageContent>
    {
        PDFPageContent()
        {
            m_hand = PDF_PageContent_create();
        }
        ~PDFPageContent()
        {
            PDF_PageContent_destroy(m_hand);
        }
        int64_t Handle() { return (int64_t)m_hand; }
        PDF_PAGECONTENT m_hand;
        void ClipPath(winrt::RDUILib::RDPath path, bool winding)
        {
            PDF_PageContent_clipPath(m_hand, (PDF_PATH)path.Handle(), winding);
        }
        void DrawForm(winrt::RDUILib::PDFResForm form)
        {
            PDF_PageContent_drawForm(m_hand, (PDF_PAGE_FORM)form.Handle());
        }
        void DrawImage(winrt::RDUILib::PDFResImage img)
        {
            PDF_PageContent_drawImage(m_hand, (PDF_PAGE_IMAGE)img.Handle());
        }
        void DrawText(winrt::hstring text)
        {
            PDF_PageContent_drawTextW(m_hand, text.c_str());
        }
        int DrawText(winrt::hstring text, int align, float width)
        {
            return PDF_PageContent_drawText2W(m_hand, text.c_str(), align, width);
        }
        PDFTextRet DrawText(winrt::hstring text, int align, float width, int max_lines)
        {
            int val = PDF_PageContent_drawText3W(m_hand, text.c_str(), align, width, max_lines);
            PDFTextRet ret;
            ret.num_unicodes = val & ((1 << 20) - 1);
            ret.num_lines = val >> 20;
            return ret;
        }
        void FillPath(RDPath path, bool winding)
        {
            PDF_PageContent_fillPath(m_hand, (PDF_PATH)path.Handle(), winding);
        }
        RDPoint GetTextSize(winrt::hstring text, winrt::RDUILib::PDFResFont pfont, float width, float height, float char_space, float word_space)
        {
            PDF_POINT pt = PDF_PageContent_textGetSizeW(m_hand, (PDF_PAGE_FONT)pfont.Handle(), text.c_str(), width, height, char_space, word_space);
            return *(RDPoint*)&pt;
        }
        void GSRestore()
        {
            PDF_PageContent_gsRestore(m_hand);
        }
        void GSSave()
        {
            PDF_PageContent_gsSave(m_hand);
        }
        void GSSet(winrt::RDUILib::PDFResGState gs)
        {
            PDF_PageContent_gsSet(m_hand, (PDF_PAGE_GSTATE)gs.Handle());
        }
        void GSSetMatrix(RDMatrix mat)
        {
            PDF_PageContent_gsSetMatrix(m_hand, (PDF_MATRIX)mat.Handle());
        }
        void SetFillColor(unsigned int color)
        {
            PDF_PageContent_setFillColor(m_hand, color);
        }
        void SetStrokeCap(int cap)
        {
            PDF_PageContent_setStrokeCap(m_hand, cap);
        }
        void SetStrokeColor(unsigned int color)
        {
            PDF_PageContent_setStrokeColor(m_hand, color);
        }
        void SetStrokeDash(winrt::array_view<float const> dash, float phase)
        {
            if (dash.size() > 0)
                PDF_PageContent_setStrokeDash(m_hand, dash.data(), dash.size(), phase);
            else
                PDF_PageContent_setStrokeDash(m_hand, NULL, 0, phase);
        }
        void SetStrokeJoin(int join)
        {
            PDF_PageContent_setStrokeJoin(m_hand, join);
        }
        void SetStrokeMiter(float miter)
        {
            PDF_PageContent_setStrokeMiter(m_hand, miter);
        }
        void SetStrokeWidth(float w)
        {
            PDF_PageContent_setStrokeWidth(m_hand, w);
        }
        void StrokePath(RDPath path)
        {
            PDF_PageContent_strokePath(m_hand, (PDF_PATH)path.Handle());
        }
        void TagEnd()
        {
            PDF_PageContent_tagBlockEnd(m_hand);
        }
        void TagStart(PDFPageTag tag)
        {
            PDF_PageContent_tagBlockStart(m_hand, (PDF_TAG)tag.Handle());
        }
        void TextBegin()
        {
            PDF_PageContent_textBegin(m_hand);
        }
        void TextEnd()
        {
            PDF_PageContent_textEnd(m_hand);
        }
        void TextMove(float x, float y)
        {
            PDF_PageContent_textMove(m_hand, x, y);
        }
        void TextNextLine()
        {
            PDF_PageContent_textNextLine(m_hand);
        }
        void TextSetCharSpace(float space)
        {
            PDF_PageContent_textSetCharSpace(m_hand, space);
        }
        void TextSetFont(winrt::RDUILib::PDFResFont font, float size)
        {
            PDF_PageContent_textSetFont(m_hand, (PDF_PAGE_FONT)font.Handle(), size);
        }
        void TextSetHScale(int scale)
        {
            PDF_PageContent_textSetHScale(m_hand, scale);
        }
        void TextSetLeading(float leading)
        {
            PDF_PageContent_textSetLeading(m_hand, leading);
        }
        void TextSetRenderMode(int mode)
        {
            PDF_PageContent_textSetRenderMode(m_hand, mode);
        }
        void TextSetRise(float rise)
        {
            PDF_PageContent_textSetRise(m_hand, rise);
        }
        void TextSetWordSpace(float space)
        {
            PDF_PageContent_textSetWordSpace(m_hand, space);
        }
    };
}

namespace winrt::RDUILib::factory_implementation
{
    struct PDFPageContent : PDFPageContentT<PDFPageContent, implementation::PDFPageContent>
    {
    };
}
