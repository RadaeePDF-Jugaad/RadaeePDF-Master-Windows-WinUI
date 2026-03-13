#pragma once

#include "RDUILib.PDFEditNode.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct PDFEditNode : PDFEditNodeT<PDFEditNode>
    {
        PDFEditNode(int64_t hand)
        {
            m_hand = (PDF_EDITNODE)hand;
        }
		~PDFEditNode()
		{
			PDF_EditNode_destroy(m_hand);
			m_hand = NULL;
		}
		int64_t Handle() { return (int64_t)m_hand; }
        PDF_EDITNODE m_hand;
		static void SetDefFont(winrt::hstring fname)
		{
			PDF_EditNode_setDefFont(fname);
		}
		static void SetDefCJKFont(winrt::hstring fname)
		{
			PDF_EditNode_setDefCJKFont(fname);
		}
		static bool caret_is_end(int64_t pos)
		{
			return (pos & 1) != 0;
		}
		static bool caret_is_vert(int64_t pos)
		{
			return (pos & 2) != 0;
		}
		static bool caret_is_same(int64_t pos0, int64_t pos1)
		{
			if (pos0 == pos1) return true;
			int ic0 = (int)((pos0 >> 16) & 65535);
			int ic1 = (int)((pos1 >> 16) & 65535);
			return ((pos0 >> 32) == (pos1 >> 32) && ic0 + 1 == ic1 && !caret_is_end(pos0) && caret_is_end(pos1));
		}
		static int64_t caret_regular_end(int64_t pos)
		{
			if (caret_is_end(pos))
			{
				int ic0 = ((int)((pos >> 16) & 65535)) + 1;
				int if0 = ((int)(pos & 65535)) & (~1);
				pos &= (~0xffffffffl);
				pos += (ic0 << 16) + if0;
			}
			return pos;
		}
		static bool caret_is_first(int64_t pos)
		{
			return ((pos >> 32) == 0 && ((pos >> 16) & 65535) == 0 && (pos & 1) == 0);
		}
		int64_t caret_regular_start(int64_t pos)
		{
			if (caret_is_end(pos))
			{
				pos &= (~1l);
				pos = GetCharNext(pos);
			}
			return pos;
		}
		int64_t GetCharPos(float pdfx, float pdfy)
		{
			return PDF_EditNode_getCharPos(m_hand, pdfx, pdfy);
		}
		int64_t GetCharPrev(int64_t pos)
		{
			return PDF_EditNode_getCharPrev(m_hand, pos);
		}
		int64_t GetCharNext(int64_t pos)
		{
			return PDF_EditNode_getCharNext(m_hand, pos);
		}
		int64_t GetCharPrevLine(float y, int64_t pos)
		{
			return PDF_EditNode_getCharPrevLine(m_hand, y, pos);
		}
		int64_t GetCharNextLine(float y, int64_t pos)
		{
			return PDF_EditNode_getCharNextLine(m_hand, y, pos);
		}
		RDRect GetCharRect(int64_t pos)
		{
			PDF_RECT rect;
			if (!PDF_EditNode_getCharRect(m_hand, pos, &rect))
			{
				rect.left = 0;
				rect.right = 0;
				rect.top = 0;
				rect.bottom = 0;
			}
			return *(RDRect*)&rect;
		}
		void CharDelete(int64_t start, int64_t end)
		{
			PDF_EditNode_charDelete(m_hand, start, end);
		}
		winrt::hstring CharGetString(int64_t start, int64_t end)
		{
			return PDF_EditNode_charGetString(m_hand, start, end);
		}
		void CharReturn(int64_t pos)
		{
			PDF_EditNode_charReturn(m_hand, pos);
		}
		int64_t CharInsert(int64_t pos, winrt::hstring sval)
		{
			return PDF_EditNode_charInsert(m_hand, pos, sval);
		}
		int64_t CharHome(int64_t pos)
		{
			return PDF_EditNode_getCharBegLine(m_hand, pos);
		}
		int64_t CharEnd(int64_t pos)
		{
			return PDF_EditNode_getCharEndLine(m_hand, pos);
		}
		int Type()
		{
			return PDF_EditNode_getType(m_hand);
		}
		RDRect Rect()
		{
			PDF_RECT rect;
			if (!PDF_EditNode_getRect(m_hand, &rect))
			{
				rect.left = 0;
				rect.right = 0;
				rect.top = 0;
				rect.bottom = 0;
			}
			return *(RDRect*)&rect;
		}
		void Rect(RDRect rect)
		{
			PDF_EditNode_setRect(m_hand, (PDF_RECT*)&rect);
		}
		void UpdateRect()
		{
			PDF_EditNode_updateRect(m_hand);
		}
		void Delete()
		{
			PDF_EditNode_delete(m_hand);
			m_hand = NULL;
		}
	};
}

namespace winrt::RDUILib::factory_implementation
{
    struct PDFEditNode : PDFEditNodeT<PDFEditNode, implementation::PDFEditNode>
    {
    };
}
