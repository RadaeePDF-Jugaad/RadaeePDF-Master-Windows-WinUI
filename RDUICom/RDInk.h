#pragma once

#include "RDUILib.RDInk.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct RDInk : RDInkT<RDInk>
    {
		RDInk(float lw, uint32_t color)
		{
            m_hand = Ink_create(lw, color);
        }
        ~RDInk()
        {
            Ink_destroy(m_hand);
        }
		int64_t Handle() { return (int64_t)m_hand; }
        PDF_INK m_hand;
		void Down(float x, float y)
		{
			Ink_onDown(m_hand, x, y);
		}
		void Move(float x, float y)
		{
			Ink_onMove(m_hand, x, y);
		}
		void Up(float x, float y)
		{
			Ink_onUp(m_hand, x, y);
		}
		int32_t NodesCnt()
		{
			return Ink_getNodeCount(m_hand);
		}
		int32_t GetOP(int32_t index)
		{
			PDF_POINT pt;
			return Ink_getNode(m_hand, index, &pt);
		}
		RDPoint GetPoint(int32_t index)
		{
			PDF_POINT pt;
			Ink_getNode(m_hand, index, &pt);
			return *(RDPoint*)&pt;
		}
	};
}

namespace winrt::RDUILib::factory_implementation
{
    struct RDInk : RDInkT<RDInk, implementation::RDInk>
    {
    };
}
