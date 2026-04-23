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

using Windows.Foundation;

using System.IO;

using System.Runtime.InteropServices.WindowsRuntime;

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

        private bool _isResizing = false;

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

            _renderService?.ClearTileCache();

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

            if (_isResizing)

                return;

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



                // Compute viewport rect in page-local pixel coordinates

                // SinglePageView: the visible area is the entire available area, offset by page position

                Rect? viewportRect = null;

                {

                    double localLeft = Math.Max(0, MainScrollViewer.HorizontalOffset - Math.Max(0, (availableWidth - renderWidth) / 2));

                    double localTop = Math.Max(0, MainScrollViewer.VerticalOffset - Math.Max(0, (availableHeight - renderHeight) / 2));

                    double localRight = Math.Min(renderWidth, localLeft + MainScrollViewer.ViewportWidth);

                    double localBottom = Math.Min(renderHeight, localTop + MainScrollViewer.ViewportHeight);



                    if (localRight > localLeft && localBottom > localTop)

                    {

                        viewportRect = new Rect(localLeft, localTop, localRight - localLeft, localBottom - localTop);

                    }

                }



                var options = new RenderOptions

                {

                    Scale = _scale,

                    RenderMode = RD_RENDER_MODE.mode_best,

                    ShowAnnotations = true,

                    ViewportRect = viewportRect

                };

                // Try to get from cache first

                string cacheKey = _renderService.GenerateCacheKey(_currentPageIndex, renderWidth, renderHeight, options);

                var cachedBitmap = _renderService.GetCachedPage(cacheKey);

                WriteableBitmap? bitmap = cachedBitmap;

                // Setup container and page offset before rendering

                _pageOffsetX = (float)Math.Max(0, (availableWidth - renderWidth) / 2);

                _pageOffsetY = (float)Math.Max(0, (availableHeight - renderHeight) / 2);

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

                _pageContainer.Width = renderWidth;

                _pageContainer.Height = renderHeight;

                Canvas.SetLeft(_pageContainer, _pageOffsetX);

                Canvas.SetTop(_pageContainer, _pageOffsetY);

                PageCanvas.Width = Math.Max(renderWidth, availableWidth);

                PageCanvas.Height = Math.Max(renderHeight, availableHeight);



                // Cache miss - render using tiled approach with progressive display

                if (bitmap == null && !cancellationToken.IsCancellationRequested)

                {

                    bitmap = new WriteableBitmap(renderWidth, renderHeight);

                    _pageContainer.PageImageControl.Source = bitmap;



                    var targetBitmap = bitmap;

                    int bitmapWidth = renderWidth;



                    await _renderService.RenderPageTiledAsync(

                        _currentPageIndex, page, renderWidth, renderHeight, options,

                        tileCallback: (result) =>

                        {

                            if (!result.Success || result.PixelData == null || cancellationToken.IsCancellationRequested)

                                return;

                            DispatcherQueue.TryEnqueue(() =>

                            {

                                if (cancellationToken.IsCancellationRequested) return;

                                try

                                {

                                    var tile = result.Tile;

                                    using (var stream = targetBitmap.PixelBuffer.AsStream())

                                    {

                                        int bytesPerPixel = 4;

                                        int tileStride = tile.Width * bytesPerPixel;

                                        int bmpStride = bitmapWidth * bytesPerPixel;

                                        for (int row = 0; row < tile.Height; row++)

                                        {

                                            stream.Seek((tile.Y + row) * bmpStride + tile.X * bytesPerPixel, SeekOrigin.Begin);

                                            stream.Write(result.PixelData, row * tileStride, tileStride);

                                        }

                                    }

                                    targetBitmap.Invalidate();

                                }

                                catch (Exception ex)

                                {

                                    System.Diagnostics.Debug.WriteLine($"Error writing tile to bitmap: {ex.Message}");

                                }

                            });

                        },

                        cancellationToken: cancellationToken

                    );



                    if (!cancellationToken.IsCancellationRequested)

                    {

                        _renderService.CacheRenderedPage(cacheKey, bitmap);

                    }

                }

                else if (bitmap != null && !cancellationToken.IsCancellationRequested)

                {

                    _pageContainer.PageImageControl.Source = bitmap;

                }



                if (!cancellationToken.IsCancellationRequested)

                {

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

                _isResizing = true;

                CancelCurrentRender();

                _resizeDebounceTimer?.Stop();

                _resizeDebounceTimer?.Start();

            }

        }

        private void OnResizeDebounceTimerTick(object? sender, object e)

        {

            _resizeDebounceTimer?.Stop();

            _isResizing = false;

            _renderService?.ClearCache();

            _renderService?.ClearTileCache();

            _ = RenderCurrentPageAsync();

        }

    }

}



