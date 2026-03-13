#pragma once

#include "RDUILib.PDFFinder.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct PDFFinder : PDFFinderT<PDFFinder>
    {
        PDFFinder(int64_t hand)
        {
            m_hand = (PDF_FINDER)hand;
        }
		~PDFFinder()
		{
			PDF_Page_findClose(m_hand);
		}
        int64_t Handle() { return (int64_t)m_hand; }
        PDF_FINDER m_hand;
		int Count()
		{
			return PDF_Page_findGetCount(m_hand);
		}
		int GetFirstChar(int index)
		{
			return PDF_Page_findGetFirstChar(m_hand, index);
		}
		int GetLastChar(int index)
		{
			return PDF_Page_findGetEndChar(m_hand, index);
		}
	};
}

namespace winrt::RDUILib::factory_implementation
{
    struct PDFFinder : PDFFinderT<PDFFinder, implementation::PDFFinder>
    {
    };
}
