#include "pch.h"
#include "PDFDoc.h"
#include "RDUILib.PDFDoc.g.cpp"
#include "PDFPage.h"
#include "PDFOutline.h"
#include "PDFDocTag.h"
#include "PDFDocForm.h"
#include "PDFDocFont.h"
#include "PDFDocImage.h"
#include "PDFDocGState.h"
#include "PDFImportor.h"

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace winrt::RDUILib::implementation
{
    int32_t PDFDoc::CreatePath(winrt::hstring path)
    {
        char stxt[64];
        WideCharToMultiByte(CP_ACP, 0, path.c_str(), -1, stxt, 63, NULL, NULL);
        PDF_ERR err;
        m_doc = PDF_Document_createForPath(stxt, &err);
        return err;
    }
    int32_t PDFDoc::Create(IRandomAccessStream stream)
    {
        PDF_ERR err;
        m_doc = PDF_Document_create(stream, &err);
        return err;
    }
    int32_t PDFDoc::OpenPath(winrt::hstring path, winrt::hstring password)
    {
        char stxt[64];
        WideCharToMultiByte(CP_ACP, 0, password.c_str(), -1, stxt, 63, NULL, NULL);
        PDF_ERR err;
        m_doc = PDF_Document_openPathW(path.c_str(), stxt, &err);
        return err;
    }
    int32_t PDFDoc::Open(IRandomAccessStream stream, winrt::hstring password)
    {
        char stxt[64];
        WideCharToMultiByte(CP_ACP, 0, password.c_str(), -1, stxt, 63, NULL, NULL);
        PDF_ERR err;
        m_doc = PDF_Document_open(stream, stxt, &err);
        return err;
    }
    int32_t PDFDoc::PageCount()
    {
        return PDF_Document_getPageCount(m_doc);
    }
    float PDFDoc::GetPageWidth(int32_t pageno)
    {
        return PDF_Document_getPageWidth(m_doc, pageno);
    }
    float PDFDoc::GetPageHeight(int32_t pageno)
    {
        return PDF_Document_getPageHeight(m_doc, pageno);
    }
    winrt::RDUILib::PDFPage PDFDoc::GetPage(int32_t pageno)
    {
        int64_t hand = (int64_t)PDF_Document_getPage(m_doc, pageno);
        if (!hand) return nullptr;
        return winrt::make<implementation::PDFPage>(hand);
    }
    void PDFDoc::Close()
    {
        if (m_doc)
        {
            PDF_Document_close(m_doc);
            m_doc = NULL;
        }
    }
    winrt::RDUILib::PDFOutline PDFDoc::GetRootOutline()
    {
        int64_t hand = (int64_t)PDF_Document_getOutlineNext(m_doc, NULL);
        if (!hand) return nullptr;
        return winrt::make<implementation::PDFOutline>((int64_t)m_doc, hand);
    }
    bool PDFDoc::AddRootOutline(winrt::hstring label, int dest, float y)
    {
        return PDF_Document_addOutlineNext(m_doc, NULL, label.c_str(), dest, y);
    }
    int32_t PDFDoc::Permission()
    {
        return PDF_Document_getPermission(m_doc);
    }
    winrt::hstring PDFDoc::XMP()
    {
        return PDF_Document_getXMP(m_doc);
    }
    int32_t PDFDoc::Perm()
    {
        return PDF_Document_getPerm(m_doc);
    }
    int32_t PDFDoc::EFCount()
    {
        return PDF_Document_getEFCount(m_doc);
    }
    winrt::hstring PDFDoc::GetEFDesc(int32_t index)
    {
        return PDF_Document_getEFDesc(m_doc, index);
    }
    winrt::hstring PDFDoc::GetEFName(int32_t index)
    {
        return PDF_Document_getEFName(m_doc, index);
    }
    bool PDFDoc::GetEFData(int32_t index, winrt::hstring path)
    {
        return PDF_Document_getEFData(m_doc, index, path);
    }
    bool PDFDoc::DelEFData(int32_t index)
    {
        return PDF_Document_delEF(m_doc, index);
    }
    bool PDFDoc::NewEF(winrt::hstring path)
    {
        return PDF_Document_newEF(m_doc, path);
    }
    int32_t PDFDoc::JSCount()
    {
        return PDF_Document_getJSCount(m_doc);
    }
    winrt::hstring PDFDoc::GetJS(int32_t index)
    {
        return PDF_Document_getJS(m_doc, index);
    }
    winrt::hstring PDFDoc::GetJSName(int32_t index)
    {
        return PDF_Document_getJSName(m_doc, index);
    }
    bool PDFDoc::CanSave()
    {
        return PDF_Document_canSave(m_doc);
    }
    bool PDFDoc::IsEncrypted()
    {
        return PDF_Document_isEncrypted(m_doc);
    }
    bool PDFDoc::IsOpened()
    {
        return (m_doc != NULL);
    }
    winrt::RDUILib::PDFDocTag PDFDoc::NewTagGroup(winrt::RDUILib::PDFDocTag parent, winrt::hstring stag)
    {
        PDF_TAG ret = PDF_Document_newTagGroup(m_doc, (parent == nullptr) ? NULL : (PDF_TAG)parent.Handle(), stag);
        if (!ret) return nullptr;
        else return winrt::make<implementation::PDFDocTag>((int64_t)ret);
    }
    int32_t PDFDoc::VerifySign(winrt::RDUILib::PDFSign sign)
    {
        return PDF_Document_verifySign(m_doc, (PDF_SIGN)sign.Handle());
    }
    winrt::RDUILib::PDFPage PDFDoc::NewPage(int32_t pageno, float w, float h)
    {
        int64_t hand = (int64_t)PDF_Document_newPage(m_doc, pageno, w, h);
        if (!hand) return nullptr;
        return winrt::make<implementation::PDFPage>(hand);
    }
    bool PDFDoc::RemovePage(int32_t pageno)
    {
        return PDF_Document_removePage(m_doc, pageno);
    }
    bool PDFDoc::MovePage(int32_t srcno, int32_t dstno)
    {
        return PDF_Document_movePage(m_doc, srcno, dstno);
    }
    winrt::RDUILib::PDFImportor PDFDoc::ImportStart(winrt::RDUILib::PDFDoc src)
    {
        int64_t hand = (int64_t)PDF_Document_importStart(m_doc, (PDF_DOC)src.Handle());
        return (!hand) ? nullptr : winrt::make<implementation::PDFImportor>((int64_t)m_doc, hand);
    }
    winrt::RDUILib::PDFDocImage PDFDoc::NewImage0(winrt::Microsoft::UI::Xaml::Media::Imaging::WriteableBitmap bitmap, bool has_alpha, bool interpolate)
    {
        int64_t hand = (int64_t)PDF_Document_newImage(m_doc, bitmap.try_as<winrt::Windows::UI::Xaml::Media::Imaging::WriteableBitmap>(), has_alpha, interpolate);
        return (!hand) ? nullptr : winrt::make<implementation::PDFDocImage>(hand);
    }
    winrt::RDUILib::PDFDocImage PDFDoc::NewImage1(winrt::Windows::Graphics::Imaging::SoftwareBitmap bitmap, bool has_alpha, bool interpolate)
    {
        int64_t hand = (int64_t)PDF_Document_newImage2(m_doc, bitmap, has_alpha, interpolate);
        return (!hand) ? nullptr : winrt::make<implementation::PDFDocImage>(hand);
    }
    winrt::RDUILib::PDFDocImage PDFDoc::NewImage2(winrt::Microsoft::UI::Xaml::Media::Imaging::WriteableBitmap bitmap, int32_t matte, bool interpolate)
    {
        int64_t hand = (int64_t)PDF_Document_newImageMatte(m_doc, bitmap.try_as<winrt::Windows::UI::Xaml::Media::Imaging::WriteableBitmap>(), matte, interpolate);
        return (!hand) ? nullptr : winrt::make<implementation::PDFDocImage>(hand);
    }
    winrt::RDUILib::PDFDocImage PDFDoc::NewImage3(winrt::Windows::Graphics::Imaging::SoftwareBitmap bitmap, int32_t matte, bool interpolate)
    {
        int64_t hand = (int64_t)PDF_Document_newImage2Matte(m_doc, bitmap, matte, interpolate);
        return (!hand) ? nullptr : winrt::make<implementation::PDFDocImage>(hand);
    }
    winrt::RDUILib::PDFDocImage PDFDoc::NewImageJPEG(winrt::hstring path, bool interpolate)
    {
        char stxt[512];
        WideCharToMultiByte(CP_ACP, 0, path.c_str(), -1, stxt, 511, NULL, NULL);
        int64_t hand = (int64_t)PDF_Document_newImageJPEG(m_doc, stxt, interpolate);
        return (!hand) ? nullptr : winrt::make<implementation::PDFDocImage>(hand);
    }
    winrt::RDUILib::PDFDocImage PDFDoc::NewImageJPX(winrt::hstring path, bool interpolate)
    {
        char stxt[512];
        WideCharToMultiByte(CP_ACP, 0, path.c_str(), -1, stxt, 511, NULL, NULL);
        int64_t hand = (int64_t)PDF_Document_newImageJPX(m_doc, stxt, interpolate);
        return (!hand) ? nullptr : winrt::make<implementation::PDFDocImage>(hand);
    }
    winrt::RDUILib::PDFDocFont PDFDoc::NewFontCID(winrt::hstring name, int32_t style)
    {
        char stxt[64];
        WideCharToMultiByte(CP_ACP, 0, name.c_str(), -1, stxt, 63, NULL, NULL);
        int64_t hand = (int64_t)PDF_Document_newFontCID(m_doc, stxt, style);
        return (!hand) ? nullptr : winrt::make<implementation::PDFDocFont>((int64_t)m_doc, hand);
    }
    winrt::RDUILib::PDFDocGState PDFDoc::NewGState()
    {
        int64_t hand = (int64_t)PDF_Document_newGState(m_doc);
        return (!hand) ? nullptr : winrt::make<implementation::PDFDocGState>((int64_t)m_doc, hand);
    }
    winrt::RDUILib::PDFDocForm PDFDoc::NewForm()
    {
        int64_t hand = (int64_t)PDF_Document_newForm(m_doc);
        return (!hand) ? nullptr : winrt::make<implementation::PDFDocForm>((int64_t)m_doc, hand);
    }
}
