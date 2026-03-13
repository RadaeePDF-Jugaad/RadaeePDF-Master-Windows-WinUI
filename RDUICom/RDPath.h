#pragma once

#include "RDUILib.RDPath.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct RDPath : RDPathT<RDPath>
    {
		RDPath()
		{
			m_hand = Path_create();
		}
		RDPath(int64_t hand)
		{
			m_hand = (PDF_PATH)hand;
		}
		~RDPath()
        {
            Path_destroy(m_hand);
        }
		int64_t Handle() { return (int64_t)m_hand; }
        PDF_PATH m_hand;
		void MoveTo(float x, float y)
		{
			Path_moveTo(m_hand, x, y);
		}
		void LineTo(float x, float y)
		{
			Path_lineTo(m_hand, x, y);
		}
		void CurveTo(float x1, float y1, float x2, float y2, float x3, float y3)
		{
			Path_curveTo(m_hand, x1, y1, x2, y2, x3, y3);
		}
		void Close()
		{
			Path_closePath(m_hand);
		}
		int32_t NodesCnt()
		{
			return Path_getNodeCount(m_hand);
		}
		int32_t GetOP(int32_t index)
		{
			PDF_POINT pt;
			return Path_getNode(m_hand, index, &pt);
		}
		RDPoint GetPoint(int32_t index)
		{
			PDF_POINT pt;
			Path_getNode(m_hand, index, &pt);
			return*(RDPoint*)&pt;
		}
	};
}

namespace winrt::RDUILib::factory_implementation
{
    struct RDPath : RDPathT<RDPath, implementation::RDPath>
    {
    };
}
