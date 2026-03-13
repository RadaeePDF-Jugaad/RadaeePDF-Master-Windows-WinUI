using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RadaeeWinUI.Models;
using RDUILib;
using System;

namespace RadaeeWinUI.Controls.PDFView
{
    public abstract class PDFView : UserControl
    {
        protected PDFDoc mPDFDoc;
        public float _zoomLevel = 1.0f;
        protected float _scale;
        private float _maxZoomLevel = 5.0f;
        private float _minZoomLevel = 0.5f;
        protected int _currentPageIndex = 0;
        protected Boolean _pointPressed = false;

        public int CurrentPageIndex => _currentPageIndex;

        public abstract Canvas GetAnnotationCanvas();

        public abstract void PDFVOpen(PDFDoc doc);
        public abstract void PDFVClose();

        public abstract float ToPDFX(float screenX, int pageIndex);
        public abstract float ToPDFY(float screenY, int pageIndex);
        public abstract float ToScreenX(float pdfX, int pageIndex);
        public abstract float ToScreenY(float pdfY, int pageIndex);

        public abstract float vPageGetWidth(int pageIndex);
        public abstract float vPageGetHeight(int pageIndex);
        public abstract int GetPageAtPoint(float screenX, float screenY);
        public abstract (float x, float y) GetPagePosition(int pageIndex);

        public abstract void vPageGoto(int pageIndex);
        public abstract void vRefresh();
        public abstract void vSetZoom(float zoomLevel);
        public abstract void InvalidatePage(int pageIndex);
        public abstract void InvalidateAll();
        protected abstract void OnSizeChanged(object sender, SizeChangedEventArgs e);

        public event EventHandler<PageChangedEventArgs> CurrentPageChanged;
        public event EventHandler<PDFPointerEventArgs> PagePointerPressed;
        public event EventHandler<PDFPointerEventArgs> PagePointerMoved;
        public event EventHandler<PDFPointerEventArgs> PagePointerReleased;
        
        public event EventHandler<PinchGestureEventArgs> PinchGesture;
        public event EventHandler<PanGestureEventArgs> PanGesture;
        public event EventHandler<SingleTapEventArgs> SingleTap;
        public event EventHandler<DoubleTapEventArgs> DoubleTap;
        public event EventHandler<LongPressEventArgs> LongPress;

        public float ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                if (_zoomLevel != value && value <= _maxZoomLevel && value >= _minZoomLevel)
                {
                    _zoomLevel = value;
                    vSetZoom(_zoomLevel);
                }
            }
        }

        public float PDFScale => _scale * ZoomLevel;

        protected void RaiseCurrentPageChanged(int oldIndex, int newIndex)
        {
            CurrentPageChanged?.Invoke(this, new PageChangedEventArgs
            {
                OldPageIndex = oldIndex,
                NewPageIndex = newIndex
            });
        }

        protected void RaisePagePointerPressed(PDFPointerEventArgs args)
        {
            PagePointerPressed?.Invoke(this, args);
        }

        protected void RaisePagePointerMoved(PDFPointerEventArgs args)
        {
            PagePointerMoved?.Invoke(this, args);
        }

        protected void RaisePagePointerReleased(PDFPointerEventArgs args)
        {
            PagePointerReleased?.Invoke(this, args);
        }

        protected void RaisePinchGesture(PinchGestureEventArgs args)
        {
            PinchGesture?.Invoke(this, args);
        }

        protected void RaisePanGesture(PanGestureEventArgs args)
        {
            PanGesture?.Invoke(this, args);
        }

        protected void RaiseSingleTap(SingleTapEventArgs args)
        {
            SingleTap?.Invoke(this, args);
        }

        protected void RaiseDoubleTap(DoubleTapEventArgs args)
        {
            DoubleTap?.Invoke(this, args);
        }

        protected void RaiseLongPress(LongPressEventArgs args)
        {
            LongPress?.Invoke(this, args);
        }
    }
}
