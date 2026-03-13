#pragma once

#include "RDUILib.RDMatrix.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct RDMatrix : RDMatrixT<RDMatrix>
    {
		RDMatrix(float scalex, float scaley, float x0, float y0)
		{
			m_hand = Matrix_createScale(scalex, scaley, x0, y0);
		}
		RDMatrix(float xx, float yx, float xy, float yy, float x0, float y0)
		{
			m_hand = Matrix_create(xx, yx, xy, yy, x0, y0);
		}
		~RDMatrix()
		{
			Matrix_destroy(m_hand);
		}
		int64_t Handle() { return (int64_t)m_hand; }
		void Invert()
		{
			Matrix_invert(m_hand);
		}
		void TransformPath(RDPath path)
		{
			Matrix_transformPath(m_hand, (PDF_PATH)path.Handle());
		}
		void TransformInk(RDInk ink)
		{
			Matrix_transformInk(m_hand, (PDF_INK)ink.Handle());
		}
		RDRect TransformRect(RDRect rect)
		{
			Matrix_transformRect(m_hand, (PDF_RECT*)&rect);
			return rect;
		}
		RDPoint TransformPoint(RDPoint point)
		{
			Matrix_transformPoint(m_hand, (PDF_POINT*)&point);
			return point;
		}
		PDF_MATRIX m_hand;
    };
}

namespace winrt::RDUILib::factory_implementation
{
    struct RDMatrix : RDMatrixT<RDMatrix, implementation::RDMatrix>
    {
    };
}
