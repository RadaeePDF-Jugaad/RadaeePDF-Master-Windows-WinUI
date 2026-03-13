#pragma once
using namespace winrt::Windows::UI::Xaml;
using namespace winrt::Windows::UI::Xaml::Controls;
using namespace winrt::Windows::UI::Core;
namespace RDDLib
{
	namespace pdfv
	{
        interface IVCallback
        {
            virtual void vpDrawSelRect(double left, double top, double right, double bottom) = 0;
            virtual void vpDrawMarkRect(double left, double top, double right, double bottom) = 0;
            virtual void vpOnFound(bool found) = 0;
            virtual CoreDispatcher vpGetDisp() = 0;
            virtual void vpShowBlock(Image img, double x, double y, double w, double h) = 0;
            virtual void vpRemoveBlock(Image img) = 0;
            virtual void vpShowPNO(TextBlock txt, double left, double top, double right, double bottom) = 0;
            virtual void vpRemovePNO(TextBlock txt) = 0;
            virtual void vpDetachBmp(WriteableBitmap bmp) = 0;
            virtual void vpAttachBmp(WriteableBitmap bmp, const Array<byte> arr) = 0;
        };
    }
}
