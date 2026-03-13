#include "pch.h"
#include "PDFAnnot.h"
#include "RDUILib.PDFAnnot.g.cpp"
#include "RDPath.h"
#include "PDFSign.h"

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace winrt::RDUILib::implementation
{
    RDUILib::RDPath PDFAnnot::InkPath()
    {
        PDF_PATH path = PDF_Page_getAnnotInkPath(m_page, m_hand);
        return (!path) ? nullptr : winrt::make<implementation::RDPath>((int64_t)path);
    }
    RDUILib::RDPath PDFAnnot::PolygonPath()
    {
        PDF_PATH path = PDF_Page_getAnnotPolygonPath(m_page, m_hand);
        return (!path) ? nullptr : winrt::make<implementation::RDPath>((int64_t)path);
    }
    RDUILib::RDPath PDFAnnot::PolylinePath()
    {
        PDF_PATH path = PDF_Page_getAnnotPolylinePath(m_page, m_hand);
        return (!path) ? nullptr : winrt::make<implementation::RDPath>((int64_t)path);
    }
    RDUILib::PDFSign PDFAnnot::Sign()
    {
        PDF_SIGN sign = PDF_Page_getAnnotSign(m_page, m_hand);
        if (!sign) return nullptr;
        return winrt::make< implementation::PDFSign>((int64_t)sign);
    }
}
