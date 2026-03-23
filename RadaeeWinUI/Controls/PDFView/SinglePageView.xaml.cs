using RDUILib;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using RadaeeWinUI.Models;
using RadaeeWinUI.Services;
using RadaeeWinUI.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Windows.Media.Devices;
using System.Collections.Generic;
namespace RadaeeWinUI.Controls.PDFView
{
    public sealed partial class SinglePageView : PDFView
    {
        private IPageRenderService? _renderService;
        private PageContainer? _pageContainer;
        private float _pageOffsetX;
        private float _pageOffsetY;
        private CancellationTokenSource? _renderCancellationTokenSource;
        private DispatcherTimer? _resizeDebounceTimer;
        private GestureRecognizer? _gestureRecognizer;
        private bool _shouldRaisePageChanged = false;
        private int _oldPageIndex = 0;
        public SinglePageView()
        {
            InitializeComponent();
            SizeChanged += OnSizeChanged;
            InitializeResizeDebounceTimer();
            InitializeGestureRecognizer();
        }
        private void InitializeResizeDebounceTimer()
        {
            _resizeDebounceTimer = new DispatcherTimer();
            _resizeDebounceTimer.Interval = TimeSpan.FromMilliseconds(200);
            _resizeDebounceTimer.Tick += OnResizeDebounceTimerTick;
        }
        private void InitializeGestureRecognizer()
        {
            _gestureRecognizer = new GestureRecognizer(PageCanvas);
            _gestureRecognizer.PinchGesture += OnGesturePinch;
            _gestureRecognizer.PanGesture += OnGesturePan;
            _gestureRecognizer.DoubleTap += OnGestureDoubleTap;
            _gestureRecognizer.LongPress += OnGestureLongPress;
            _gestureRecognizer.SingleTap += OnGestureSingleTap;
        }
        private void OnGesturePinch(object? sender, PinchGestureEventArgs e)
        {
            e.PageIndex = _currentPageIndex;
            RaisePinchGesture(e);
        }
        private void OnGesturePan(object? sender, PanGestureEventArgs e)
        {
            e.PageIndex = _currentPageIndex;
            RaisePanGesture(e);
        }
        private void OnGestureDoubleTap(object? sender, DoubleTapEventArgs e)
        {
            e.PageIndex = _currentPageIndex;
            RaiseDoubleTap(e);
        }
        private void OnGestureSingleTap(object? sender, SingleTapEventArgs e)
        {
            e.PageIndex = _currentPageIndex;
            RaiseSingleTap(e);
        }
        private void OnGestureLongPress(object? sender, LongPressEventArgs e)
        {
            e.PageIndex = _currentPageIndex;
            RaiseLongPress(e);
        }
        public void SetRenderService(IPageRenderService renderService)
        {
            _renderService = renderService;
        }
        public override void PDFVOpen(PDFDoc doc)
        {
            mPDFDoc = doc;
            if (doc != null && doc.IsOpened)
            {
                _currentPageIndex = 0;
                _ = RenderCurrentPageAsync();
            }
        }
        public override Canvas? GetPageAnnotationCanvas(int pageIndex)
        {
            if (pageIndex == _currentPageIndex && _pageContainer != null)
            {
                return _pageContainer.AnnotationCanvasControl;
            }
            return null;
        }
        public override List<int> GetVisiblePageIndices()
        {
            return new List<int> { _currentPageIndex };
        }
        public override void PDFVClose()
        {
            CancelCurrentRender();
            _resizeDebounceTimer?.Stop();
            _gestureRecognizer?.Dispose();
            _gestureRecognizer = null;
            if (_pageContainer != null)
            {
                PageCanvas.Children.Remove(_pageContainer);
                _pageContainer = null;
            }
            mPDFDoc = null;
            _renderService?.ClearCache();
        }
        public override float ToPDFX(float screenX, int pageIndex)
        {
            if (pageIndex != _currentPageIndex)
                return 0;
            float relativeX = screenX - _pageOffsetX;
            return relativeX / _scale;// / ZoomLevel;
        }
        public override float ToPDFY(float screenY, int pageIndex)
        {
            if (pageIndex != _currentPageIndex)
                return 0;
            float pageHeight = vPageGetHeight(pageIndex);
            float relativeY = screenY - _pageOffsetY;
            return pageHeight - (relativeY / _scale);// / ZoomLevel);
        }
        public override float ToScreenX(float pdfX, int pageIndex)
        {
            if (pageIndex != _currentPageIndex)
                return 0;
            return pdfX * _scale + _pageOffsetX;
        }
        public override float ToScreenY(float pdfY, int pageIndex)
        {
            if (pageIndex != _currentPageIndex)
                return 0;
            float pageHeight = vPageGetHeight(pageIndex);
            return (pageHeight - pdfY) * _scale + _pageOffsetY;
        }
        public override float vPageGetWidth(int pageIndex)
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened)
                return 0;
            if (pageIndex < 0 || pageIndex >= mPDFDoc.PageCount)
                return 0;
            return mPDFDoc.GetPageWidth(pageIndex);
        }
        public override float vPageGetHeight(int pageIndex)
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened)
                return 0;
            if (pageIndex < 0 || pageIndex >= mPDFDoc.PageCount)
                return 0;
            return mPDFDoc.GetPageHeight(pageIndex);
        }
        public override int GetPageAtPoint(float screenX, float screenY)
        {
            return _currentPageIndex;
        }
        public override (float x, float y) GetPagePosition(int pageIndex)
        {
            if (pageIndex != _currentPageIndex)
                return (0, 0);
            return (_pageOffsetX, _pageOffsetY);
        }
        public override void vPageGoto(int pageIndex)
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened)
                return;
            if (pageIndex < 0 || pageIndex >= mPDFDoc.PageCount)
                return;
            if (pageIndex == _currentPageIndex)
                return;
            _oldPageIndex = _currentPageIndex;
            _currentPageIndex = pageIndex;
            _shouldRaisePageChanged = true;
            _ = RenderCurrentPageAsync();
            // Note: RaiseCurrentPageChanged will be called after rendering completes
        }
        public override void vRefresh()
        {
            _ = RenderCurrentPageAsync();
        }
        public override void vSetZoom(float zoomLevel)
        {
            ZoomLevel = zoomLevel;
            if (_renderService != null)
                _renderService.ClearCache();
            _ = RenderCurrentPageAsync();
        }
        public override void InvalidatePage(int pageIndex)
        {
            if (pageIndex == _currentPageIndex)
            {
                _ = RenderCurrentPageAsync();
            }
        }
        public override void InvalidateAll()
        {
            _ = RenderCurrentPageAsync();
        }
        private void CancelCurrentRender()
        {
            if (_renderCancellationTokenSource != null)
            {
                _renderCancellationTokenSource.Cancel();
                _renderCancellationTokenSource.Dispose();
                _renderCancellationTokenSource = null;
            }
        }
        private async Task RenderCurrentPageAsync()
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _renderService == null)
                return;
            CancelCurrentRender();
            _renderCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _renderCancellationTokenSource.Token;
            try
            {
                var page = mPDFDoc.GetPage(_currentPageIndex);
                if (page == null)
                    return;
                float pageWidth = vPageGetWidth(_currentPageIndex);
                float pageHeight = vPageGetHeight(_currentPageIndex);
                double availableWidth = ActualWidth > 0 ? ActualWidth : 800;
                double availableHeight = ActualHeight > 0 ? ActualHeight : 600;
                _scale = (float)Math.Min(availableWidth / pageWidth, availableHeight / pageHeight);
                _scale *= ZoomLevel;
                int renderWidth = (int)(pageWidth * _scale);
                int renderHeight = (int)(pageHeight * _scale);
                var options = new RenderOptions
                {
                    Scale = _scale,
                    RenderMode = RD_RENDER_MODE.mode_best,
                    ShowAnnotations = true
                };
                // Try to get from cache first
                string cacheKey = _renderService.GenerateCacheKey(_currentPageIndex, renderWidth, renderHeight, options);
                var cachedBitmap = _renderService.GetCachedPage(cacheKey);
                WriteableBitmap? bitmap = cachedBitmap;
                // Cache miss - render the page
                if (bitmap == null)
                {
                    bitmap = await _renderService.RenderPageAsync(page, renderWidth, renderHeight, options, cancellationToken);
                    // Store in cache
                    if (bitmap != null && !cancellationToken.IsCancellationRequested)
                    {
                        _renderService.CacheRenderedPage(cacheKey, bitmap);
                    }
                }
                if (bitmap != null && !cancellationToken.IsCancellationRequested)
                {
                    // Calculate page offset for centering when page is smaller than container
                    _pageOffsetX = (float)Math.Max(0, (availableWidth - renderWidth) / 2);
                    _pageOffsetY = (float)Math.Max(0, (availableHeight - renderHeight) / 2);
                    // Create or update PageContainer
                    if (_pageContainer == null)
                    {
                        _pageContainer = new PageContainer
                        {
                            PageIndex = _currentPageIndex
                        };
                        PageCanvas.Children.Add(_pageContainer);
                    }
                    else
                    {
                        // Clear annotation canvas when reusing container for different page
                        _pageContainer.AnnotationCanvasControl.Children.Clear();
                        _pageContainer.PageIndex = _currentPageIndex;
                    }
                    _pageContainer.PageImageControl.Source = bitmap;
                    _pageContainer.Width = renderWidth;
                    _pageContainer.Height = renderHeight;
                    // Position the container on canvas
                    Canvas.SetLeft(_pageContainer, _pageOffsetX);
                    Canvas.SetTop(_pageContainer, _pageOffsetY);
                    // Set canvas size to accommodate the page with offset
                    PageCanvas.Width = Math.Max(renderWidth, availableWidth);
                    PageCanvas.Height = Math.Max(renderHeight, availableHeight);

                    // Trigger page changed event after rendering is complete
                    // This ensures AnnotationCanvas is ready for highlights
                    if (_shouldRaisePageChanged)
                    {
                        _shouldRaisePageChanged = false;
                        RaiseCurrentPageChanged(_oldPageIndex, _currentPageIndex);
                    }
                    else
                    {
                        // If not a page change (e.g., zoom or resize), still trigger a refresh
                        // to ensure highlights are re-rendered with correct coordinates
                        RaiseCurrentPageChanged(_currentPageIndex, _currentPageIndex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Render was cancelled, this is expected
            }
            finally
            {
                if (_renderCancellationTokenSource != null && !_renderCancellationTokenSource.IsCancellationRequested)
                {
                    _renderCancellationTokenSource.Dispose();
                    _renderCancellationTokenSource = null;
                }
            }
        }
        private void PageCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _pointPressed = true;
            var point = e.GetCurrentPoint(PageCanvas);
            float screenX = (float)point.Position.X;
            float screenY = (float)point.Position.Y;
            float pdfX = ToPDFX(screenX, _currentPageIndex);
            float pdfY = ToPDFY(screenY, _currentPageIndex);
            var args = new PDFPointerEventArgs
            {
                PageIndex = _currentPageIndex,
                ScreenX = screenX,
                ScreenY = screenY,
                PDFX = pdfX,
                PDFY = pdfY,
                PointerPoint = point
            };
            RaisePagePointerPressed(args);
        }
        private void PageCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_pointPressed)
                return;
            var point = e.GetCurrentPoint(PageCanvas);
            float screenX = (float)point.Position.X;
            float screenY = (float)point.Position.Y;
            float pdfX = ToPDFX(screenX, _currentPageIndex);
            float pdfY = ToPDFY(screenY, _currentPageIndex);
            var args = new PDFPointerEventArgs
            {
                PageIndex = _currentPageIndex,
                ScreenX = screenX,
                ScreenY = screenY,
                PDFX = pdfX,
                PDFY = pdfY,
                PointerPoint = point
            };
            RaisePagePointerMoved(args);
        }
        private void PageCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _pointPressed = false;
            var point = e.GetCurrentPoint(PageCanvas);
            float screenX = (float)point.Position.X;
            float screenY = (float)point.Position.Y;
            float pdfX = ToPDFX(screenX, _currentPageIndex);
            float pdfY = ToPDFY(screenY, _currentPageIndex);
            var args = new PDFPointerEventArgs
            {
                PageIndex = _currentPageIndex,
                ScreenX = screenX,
                ScreenY = screenY,
                PDFX = pdfX,
                PDFY = pdfY,
                PointerPoint = point
            };
            RaisePagePointerReleased(args);
        }
        protected override void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                _resizeDebounceTimer?.Stop();
                _resizeDebounceTimer?.Start();
            }
        }
        private void OnResizeDebounceTimerTick(object? sender, object e)
        {
            _resizeDebounceTimer?.Stop();
            _renderService?.ClearCache();
            _ = RenderCurrentPageAsync();
        }
    }
}

