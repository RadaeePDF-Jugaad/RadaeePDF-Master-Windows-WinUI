using RDUILib;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using RadaeeWinUI.Models;
using RadaeeWinUI.Services;
using RadaeeWinUI.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Windows.Foundation;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

namespace RadaeeWinUI.Controls.PDFView
{
    public sealed partial class VerticalScrollView : PDFView
    {
        private IPageRenderService? _renderService;
        private ILayoutManager? _layoutManager;
        private Dictionary<int, PageContainer> _pageImages = new();
        private Dictionary<int, CancellationTokenSource> _renderCancellationTokens = new();
        private List<PageLayoutInfo> _visiblePages = new();
        private DispatcherTimer? _scrollDebounceTimer;
        private DispatcherTimer? _resizeDebounceTimer;
        private double _lastScrollOffset = 0;
        private double _lastViewportWidth = 0;
        private bool _isLoaded = false;
        private bool _needsInitialization = false;
        private bool _isResizing = false;
        private GestureRecognizer? _gestureRecognizer;

        public VerticalScrollView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            InitializeScrollDebounceTimer();
            InitializeResizeDebounceTimer();
            InitializeGestureRecognizer();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;

            if (_needsInitialization && mPDFDoc != null && mPDFDoc.IsOpened)
            {
                _needsInitialization = false;
                InitializeLayout();
                UpdateVisiblePages();
            }
        }

        private void InitializeScrollDebounceTimer()
        {
            _scrollDebounceTimer = new DispatcherTimer();
            _scrollDebounceTimer.Interval = TimeSpan.FromMilliseconds(100);
            _scrollDebounceTimer.Tick += OnScrollDebounceTimerTick;
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
            _gestureRecognizer.SingleTap += OnGestureSingleTap;
            _gestureRecognizer.DoubleTap += OnGestureDoubleTap;
            _gestureRecognizer.LongPress += OnGestureLongPress;
        }

        private void OnGesturePinch(object? sender, PinchGestureEventArgs e)
        {
            e.PageIndex = GetPageAtPoint(e.CenterX, e.CenterY);
            e.CenterX = (float)(e.CenterX - MainScrollViewer.HorizontalOffset);
            e.CenterY = (float)(e.CenterY - MainScrollViewer.VerticalOffset);
            RaisePinchGesture(e);
        }

        private void OnGesturePan(object? sender, PanGestureEventArgs e)
        {
            e.PageIndex = _currentPageIndex;
            RaisePanGesture(e);
        }

        private void OnGestureSingleTap(object? sender, SingleTapEventArgs e)
        {
            e.PageIndex = GetPageAtPoint(e.X, e.Y);
            RaiseSingleTap(e);
        }

        private void OnGestureDoubleTap(object? sender, DoubleTapEventArgs e)
        {
            e.PageIndex = GetPageAtPoint(e.X, e.Y);
            RaiseDoubleTap(e);
        }

        private void OnGestureLongPress(object? sender, LongPressEventArgs e)
        {
            e.PageIndex = GetPageAtPoint(e.X, e.Y);
            RaiseLongPress(e);
        }

        public void SetRenderService(IPageRenderService renderService)
        {
            _renderService = renderService;
        }

        public void SetLayoutManager(ILayoutManager layoutManager)
        {
            _layoutManager = layoutManager;
        }

        public override void PDFVOpen(PDFDoc doc)
        {
            mPDFDoc = doc;
            if (doc != null && doc.IsOpened)
            {
                _currentPageIndex = 0;

                if (_isLoaded)
                {
                    InitializeLayout();
                    UpdateVisiblePages();
                }
                else
                {
                    _needsInitialization = true;
                }
            }
        }

        public override Canvas? GetPageAnnotationCanvas(int pageIndex)
        {
            if (_pageImages.TryGetValue(pageIndex, out var pageContainer))
            {
                return pageContainer.AnnotationCanvasControl;
            }
            return null;
        }

        public override List<int> GetVisiblePageIndices()
        {
            return new List<int>(_pageImages.Keys);
        }

        public override void PDFVClose()
        {
            CancelAllRenders();
            _scrollDebounceTimer?.Stop();
            _resizeDebounceTimer?.Stop();
            _isResizing = false;
            _gestureRecognizer?.Dispose();
            _gestureRecognizer = null;
            ClearAllPages();
            mPDFDoc = null;
            _renderService?.ClearCache();
            _renderService?.ClearTileCache();
        }

        private void InitializeLayout()
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _layoutManager == null)
                return;

            double containerWidth = MainScrollViewer.ViewportWidth > 0 ? MainScrollViewer.ViewportWidth : (ActualWidth > 0 ? ActualWidth : 800);
            double containerHeight = MainScrollViewer.ViewportHeight > 0 ? MainScrollViewer.ViewportHeight : (ActualHeight > 0 ? ActualHeight : 600);

            _layoutManager.CurrentViewMode = ViewMode.VerticalContinuous;
            _layoutManager.Initialize(mPDFDoc.PageCount, containerWidth, containerHeight);

            for (int i = 0; i < mPDFDoc.PageCount; i++)
            {
                float pageWidth = vPageGetWidth(i);
                float pageHeight = vPageGetHeight(i);

                float scale = (float)(containerWidth / pageWidth);

                _layoutManager.UpdatePageSize(i, pageWidth * ZoomLevel * scale, pageHeight * ZoomLevel * scale);
            }

            var totalSize = _layoutManager.GetTotalSize();
            PageCanvas.Width = Math.Max(totalSize.width, containerWidth);
            PageCanvas.Height = totalSize.height;
        }

        private float GetCurrentScale()
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened)
                return 1.0f;

            double viewportWidth = MainScrollViewer.ViewportWidth > 0 ? MainScrollViewer.ViewportWidth : (ActualWidth > 0 ? ActualWidth : 800);
            float pageWidth = vPageGetWidth(CurrentPageIndex);

            return (float)(viewportWidth / pageWidth) * ZoomLevel;
        }

        public override float ToPDFX(float screenX, int pageIndex)
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _layoutManager == null)
                return 0;

            var pagePos = _layoutManager.GetPagePosition(pageIndex);
            float relativeX = screenX - (float)pagePos.x;
            float scale = GetCurrentScale();
            return relativeX / scale;
        }

        public override float ToPDFY(float screenY, int pageIndex)
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _layoutManager == null)
                return 0;

            var pagePos = _layoutManager.GetPagePosition(pageIndex);
            float pageHeight = vPageGetHeight(pageIndex);
            float relativeY = screenY - (float)pagePos.y;
            float scale = GetCurrentScale();

            return pageHeight - (relativeY / scale);
        }

        public override float ToScreenX(float pdfX, int pageIndex)
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _layoutManager == null)
                return 0;

            var pagePos = _layoutManager.GetPagePosition(pageIndex);
            float scale = GetCurrentScale();
            return pdfX * scale + (float)pagePos.x;
        }

        public override float ToScreenY(float pdfY, int pageIndex)
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _layoutManager == null)
                return 0;

            var pagePos = _layoutManager.GetPagePosition(pageIndex);
            float scrollY = (float)MainScrollViewer.VerticalOffset;
            float pageHeight = vPageGetHeight(pageIndex);
            float scale = GetCurrentScale();
            return (pageHeight - pdfY) * scale + (float)pagePos.y;
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
            foreach (var pageInfo in _visiblePages)
            {
                if (screenX >= pageInfo.X && screenX <= pageInfo.X + pageInfo.Width &&
                    screenY >= pageInfo.Y && screenY <= pageInfo.Y + pageInfo.Height)
                {
                    return pageInfo.PageIndex;
                }
            }

            return -1;
        }

        public override (float x, float y) GetPagePosition(int pageIndex)
        {
            if (_layoutManager == null)
                return (0, 0);

            var pos = _layoutManager.GetPagePosition(pageIndex);
            return ((float)pos.x, (float)pos.y);
        }

        public override void vPageGoto(int pageIndex)
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _layoutManager == null)
                return;

            if (pageIndex < 0 || pageIndex >= mPDFDoc.PageCount)
                return;

            int oldIndex = _currentPageIndex;
            _currentPageIndex = pageIndex;

            var pagePos = _layoutManager.GetPagePosition(pageIndex);
            MainScrollViewer.ChangeView(null, pagePos.y, null, false);

            // Force immediate update of visible pages to ensure target page container is created
            UpdateVisiblePages();

            // Now raise the event after page containers are ready
            RaiseCurrentPageChanged(oldIndex, _currentPageIndex);
        }

        public override void vRefresh()
        {
            UpdateVisiblePages();
        }

        public override void vSetZoom(float zoomLevel)
        {
            if (Math.Abs(ZoomLevel - zoomLevel) < 0.001f)
                return;

            ZoomLevel = zoomLevel;
            InitializeLayout();
            UpdateVisiblePages();
            _renderService?.ClearCache();
            _renderService?.ClearTileCache();
            InvalidatePage(CurrentPageIndex);

            // Trigger page changed event to refresh highlights with new zoom level
            RaiseCurrentPageChanged(_currentPageIndex, _currentPageIndex);
        }

        public override void InvalidatePage(int pageIndex)
        {
            if (_pageImages.ContainsKey(pageIndex))
            {
                _ = RenderPageAsync(pageIndex);
            }
        }

        public override void InvalidateAll()
        {
            UpdateVisiblePages();
        }

        private void UpdateVisiblePages()
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _layoutManager == null)
                return;

            double scrollOffsetY = MainScrollViewer.VerticalOffset;
            double viewportWidth = MainScrollViewer.ViewportWidth;
            double viewportHeight = MainScrollViewer.ViewportHeight;

            var newVisiblePages = _layoutManager.CalculateLayout(0, scrollOffsetY, viewportWidth, viewportHeight);

            var pagesToRemove = new List<int>();
            foreach (var pageIndex in _pageImages.Keys)
            {
                bool isStillVisible = false;
                foreach (var pageInfo in newVisiblePages)
                {
                    if (pageInfo.PageIndex == pageIndex)
                    {
                        isStillVisible = true;
                        break;
                    }
                }

                if (!isStillVisible)
                {
                    pagesToRemove.Add(pageIndex);
                }
            }

            foreach (var pageIndex in pagesToRemove)
            {
                RemovePage(pageIndex);
            }

            _visiblePages = newVisiblePages;

            foreach (var pageInfo in _visiblePages)
            {
                if (!_pageImages.ContainsKey(pageInfo.PageIndex))
                {
                    CreatePageImage(pageInfo);
                }
                else
                {
                    UpdatePageImagePosition(pageInfo);
                }
            }

            UpdateCurrentPage(scrollOffsetY);
        }

        private void CreatePageImage(PageLayoutInfo pageInfo)
        {
            var pageContainer = new PageContainer();
            pageContainer.PageIndex = pageInfo.PageIndex;
            pageContainer.SetSize(pageInfo.Width, pageInfo.Height);
            pageContainer.SetPosition(pageInfo.X, pageInfo.Y);

            PageCanvas.Children.Add(pageContainer);
            _pageImages[pageInfo.PageIndex] = pageContainer;

            _ = RenderPageAsync(pageInfo.PageIndex);
        }

        private void UpdatePageImagePosition(PageLayoutInfo pageInfo)
        {
            if (_pageImages.TryGetValue(pageInfo.PageIndex, out var pageContainer))
            {
                pageContainer.SetPosition(pageInfo.X, pageInfo.Y);
                pageContainer.SetSize(pageInfo.Width, pageInfo.Height);
            }
        }

        private void RemovePage(int pageIndex)
        {
            if (_pageImages.TryGetValue(pageIndex, out var pageContainer))
            {
                PageCanvas.Children.Remove(pageContainer);
                _pageImages.Remove(pageIndex);
            }

            if (_renderCancellationTokens.TryGetValue(pageIndex, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _renderCancellationTokens.Remove(pageIndex);
            }
        }

        private void ClearAllPages()
        {
            foreach (var pageContainer in _pageImages.Values)
            {
                PageCanvas.Children.Remove(pageContainer);
            }
            _pageImages.Clear();
            _visiblePages.Clear();
        }

        private void CancelAllRenders()
        {
            foreach (var cts in _renderCancellationTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _renderCancellationTokens.Clear();
        }

        private async Task RenderPageAsync(int pageIndex)
        {
            if (_isResizing)
                return;

            if (mPDFDoc == null || !mPDFDoc.IsOpened || _renderService == null)
                return;

            if (!_pageImages.ContainsKey(pageIndex))
                return;

            if (_renderCancellationTokens.TryGetValue(pageIndex, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }

            var cts = new CancellationTokenSource();
            _renderCancellationTokens[pageIndex] = cts;

            try
            {
                var page = mPDFDoc.GetPage(pageIndex);
                if (page == null)
                    return;

                float pageWidth = vPageGetWidth(pageIndex);
                float pageHeight = vPageGetHeight(pageIndex);

                double viewportWidth = MainScrollViewer.ViewportWidth > 0 ? MainScrollViewer.ViewportWidth : (ActualWidth > 0 ? ActualWidth : 800);

                _scale = (float)(viewportWidth / pageWidth);

                int renderWidth = (int)(pageWidth * _scale);
                int renderHeight = (int)(pageHeight * _scale);

                // Compute viewport rect in page-local pixel coordinates
                Rect? viewportRect = null;
                if (_layoutManager != null)
                {
                    var pagePos = _layoutManager.GetPagePosition(pageIndex);
                    double scrollY = MainScrollViewer.VerticalOffset;
                    double vpWidth = MainScrollViewer.ViewportWidth;
                    double vpHeight = MainScrollViewer.ViewportHeight;

                    double localLeft = Math.Max(0, -pagePos.x);
                    double localTop = Math.Max(0, scrollY - pagePos.y);
                    double localRight = Math.Min(renderWidth, vpWidth - pagePos.x);
                    double localBottom = Math.Min(renderHeight, scrollY + vpHeight - pagePos.y);

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

                // Try to get from page cache first
                string cacheKey = _renderService.GenerateCacheKey(pageIndex, renderWidth, renderHeight, options);
                var cachedBitmap = _renderService.GetCachedPage(cacheKey);

                WriteableBitmap? bitmap = cachedBitmap;

                // Cache miss - render using tiled approach with progressive display
                if (bitmap == null && !cts.Token.IsCancellationRequested)
                {
                    // Create WriteableBitmap on UI thread and set as source immediately
                    bitmap = new WriteableBitmap(renderWidth, renderHeight);
                    if (_pageImages.TryGetValue(pageIndex, out var container))
                    {
                        container.PageImageControl.Source = bitmap;
                    }

                    // Capture bitmap reference for tile callback
                    var targetBitmap = bitmap;
                    int bitmapWidth = renderWidth;

                    await _renderService.RenderPageTiledAsync(
                        pageIndex, page, renderWidth, renderHeight, options,
                        tileCallback: (result) =>
                        {
                            if (!result.Success || result.PixelData == null || cts.Token.IsCancellationRequested)
                                return;

                            // Dispatch tile writing to UI thread
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                if (cts.Token.IsCancellationRequested)
                                    return;
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
                        cancellationToken: cts.Token
                    );

                    // Cache the completed bitmap
                    if (!cts.Token.IsCancellationRequested)
                    {
                        _renderService.CacheRenderedPage(cacheKey, bitmap);
                    }
                }
                else if (bitmap != null && !cts.Token.IsCancellationRequested && _pageImages.TryGetValue(pageIndex, out var pageContainer))
                {
                    // Cache hit - set directly
                    pageContainer.PageImageControl.Source = bitmap;
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (_renderCancellationTokens.TryGetValue(pageIndex, out var currentCts) && currentCts == cts)
                {
                    _renderCancellationTokens.Remove(pageIndex);
                }
                cts.Dispose();
            }
        }

        private void UpdateCurrentPage(double scrollOffsetY)
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened)
                return;

            double viewportCenter = scrollOffsetY + MainScrollViewer.ViewportHeight / 2;

            int newCurrentPage = _currentPageIndex;
            double minDistance = double.MaxValue;

            foreach (var pageInfo in _visiblePages)
            {
                double pageCenter = pageInfo.Y + pageInfo.Height / 2;
                double distance = Math.Abs(pageCenter - viewportCenter);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    newCurrentPage = pageInfo.PageIndex;
                }
            }

            if (newCurrentPage != _currentPageIndex)
            {
                int oldIndex = _currentPageIndex;
                _currentPageIndex = newCurrentPage;
                RaiseCurrentPageChanged(oldIndex, _currentPageIndex);
            }
        }

        private void MainScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            double currentScrollOffset = MainScrollViewer.VerticalOffset;

            if (Math.Abs(currentScrollOffset - _lastScrollOffset) > 1.0)
            {
                _lastScrollOffset = currentScrollOffset;
                _scrollDebounceTimer?.Stop();
                _scrollDebounceTimer?.Start();
            }
        }

        private void OnScrollDebounceTimerTick(object? sender, object e)
        {
            _scrollDebounceTimer?.Stop();
            UpdateVisiblePages();
        }

        private void PageCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(PageCanvas);
            float screenX = (float)point.Position.X;
            float screenY = (float)point.Position.Y;

            int pageIndex = GetPageAtPoint(screenX, screenY);
            if (pageIndex < 0)
                return;

            _pointPressed = true;

            float pdfX = ToPDFX(screenX, pageIndex);
            float pdfY = ToPDFY(screenY, pageIndex);

            var args = new PDFPointerEventArgs
            {
                PageIndex = pageIndex,
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

            int pageIndex = GetPageAtPoint(screenX, screenY);
            if (pageIndex < 0)
                return;

            float pdfX = ToPDFX(screenX, pageIndex);
            float pdfY = ToPDFY(screenY, pageIndex);

            var args = new PDFPointerEventArgs
            {
                PageIndex = pageIndex,
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

            int pageIndex = GetPageAtPoint(screenX, screenY);
            if (pageIndex < 0)
                return;

            float pdfX = ToPDFX(screenX, pageIndex);
            float pdfY = ToPDFY(screenY, pageIndex);

            var args = new PDFPointerEventArgs
            {
                PageIndex = pageIndex,
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
                CancelAllRenders();
                _resizeDebounceTimer?.Stop();
                _resizeDebounceTimer?.Start();
            }
        }

        private void OnResizeDebounceTimerTick(object? sender, object e)
        {
            _resizeDebounceTimer?.Stop();
            _isResizing = false;

            double currentViewportWidth = MainScrollViewer.ViewportWidth;
            _lastViewportWidth = currentViewportWidth;

            _renderService?.ClearCache();
            _renderService?.ClearTileCache();

            InitializeLayout();
            UpdateVisiblePages();
        }
    }
}
