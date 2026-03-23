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

namespace RadaeeWinUI.Controls.PDFView
{
    public sealed partial class DualPageView : PDFView
    {
        private IPageRenderService? _renderService;
        private ILayoutManager? _layoutManager;
        private Dictionary<int, PageContainer> _pageContainers = new();
        private Dictionary<int, CancellationTokenSource> _renderCancellationTokens = new();
        private bool _isLoaded = false;
        private bool _needsInitialization = false;
        private int _currentBasePage = 0;
        private GestureRecognizer? _gestureRecognizer;

        public DualPageView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            InitializeGestureRecognizer();
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

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            
            if (_needsInitialization && mPDFDoc != null && mPDFDoc.IsOpened)
            {
                _needsInitialization = false;
                InitializeLayout();
                RenderCurrentPages();
            }
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
                _currentBasePage = 0;
                _currentPageIndex = 0;
                
                if (_isLoaded)
                {
                    InitializeLayout();
                    RenderCurrentPages();
                }
                else
                {
                    _needsInitialization = true;
                }
            }
        }

        public override Canvas? GetPageAnnotationCanvas(int pageIndex)
        {
            if (_pageContainers.TryGetValue(pageIndex, out var container))
            {
                return container.AnnotationCanvasControl;
            }
            return null;
        }

        public override List<int> GetVisiblePageIndices()
        {
            return new List<int>(_pageContainers.Keys);
        }

        public override void PDFVClose()
        {
            CancelAllRenders();
            _gestureRecognizer?.Dispose();
            _gestureRecognizer = null;
            ClearAllPages();
            mPDFDoc = null;
            _renderService?.ClearCache();
        }

        private void InitializeLayout()
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _layoutManager == null)
                return;

            double containerWidth = MainScrollViewer.ViewportWidth > 0 ? MainScrollViewer.ViewportWidth : (ActualWidth > 0 ? ActualWidth : 800);
            double containerHeight = MainScrollViewer.ViewportHeight > 0 ? MainScrollViewer.ViewportHeight : (ActualHeight > 0 ? ActualHeight : 600);

            _layoutManager.CurrentViewMode = ViewMode.DualPage;
            _layoutManager.Initialize(mPDFDoc.PageCount, containerWidth, containerHeight);

            int leftPage = _currentBasePage;
            int rightPage = _currentBasePage + 1;

            if (leftPage < mPDFDoc.PageCount)
            {
                float leftPageWidth = vPageGetWidth(leftPage);
                float leftPageHeight = vPageGetHeight(leftPage);
                
                float scale = (float)(containerWidth / (leftPageWidth * 2));
                
                _layoutManager.UpdatePageSize(leftPage, leftPageWidth * scale, leftPageHeight * scale);

                if (rightPage < mPDFDoc.PageCount)
                {
                    float rightPageWidth = vPageGetWidth(rightPage);
                    float rightPageHeight = vPageGetHeight(rightPage);
                    
                    _layoutManager.UpdatePageSize(rightPage, rightPageWidth * ZoomLevel * scale, rightPageHeight * ZoomLevel * scale);
                }
            }

            var totalSize = _layoutManager.GetTotalSize();
            PageCanvas.Width = Math.Max(totalSize.width, containerWidth);
            PageCanvas.Height = Math.Max(totalSize.height, containerHeight);
        }

        private float GetCurrentScale()
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened)
                return 1.0f;

            double viewportWidth = MainScrollViewer.ViewportWidth > 0 ? MainScrollViewer.ViewportWidth : (ActualWidth > 0 ? ActualWidth : 800);
            float pageWidth = vPageGetWidth(_currentBasePage);
            
            return (float)(viewportWidth / (pageWidth * 2)) * ZoomLevel;
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
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _layoutManager == null)
                return -1;

            int leftPage = _currentBasePage;
            int rightPage = _currentBasePage + 1;

            if (leftPage < mPDFDoc.PageCount)
            {
                var leftPagePos = _layoutManager.GetPagePosition(leftPage);
                float leftPageWidth = vPageGetWidth(leftPage) * GetCurrentScale();
                float leftPageHeight = vPageGetHeight(leftPage) * GetCurrentScale();

                if (screenX >= leftPagePos.x && screenX <= leftPagePos.x + leftPageWidth &&
                    screenY >= leftPagePos.y && screenY <= leftPagePos.y + leftPageHeight)
                {
                    return leftPage;
                }
            }

            if (rightPage < mPDFDoc.PageCount)
            {
                var rightPagePos = _layoutManager.GetPagePosition(rightPage);
                float rightPageWidth = vPageGetWidth(rightPage) * GetCurrentScale();
                float rightPageHeight = vPageGetHeight(rightPage) * GetCurrentScale();

                if (screenX >= rightPagePos.x && screenX <= rightPagePos.x + rightPageWidth &&
                    screenY >= rightPagePos.y && screenY <= rightPagePos.y + rightPageHeight)
                {
                    return rightPage;
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
            if (mPDFDoc == null || !mPDFDoc.IsOpened)
                return;

            if (pageIndex < 0 || pageIndex >= mPDFDoc.PageCount)
                return;

            int oldIndex = _currentPageIndex;
            
            _currentBasePage = (pageIndex / 2) * 2;
            _currentPageIndex = pageIndex;

            InitializeLayout();
            RenderCurrentPages();

            RaiseCurrentPageChanged(oldIndex, _currentPageIndex);
        }

        public override void vRefresh()
        {
            RenderCurrentPages();
        }

        public override void vSetZoom(float zoomLevel)
        {
            if (Math.Abs(ZoomLevel - zoomLevel) < 0.001f)
                return;

            ZoomLevel = zoomLevel;
            InitializeLayout();
            RenderCurrentPages();
            InvalidatePage(CurrentPageIndex);
            InvalidatePage(CurrentPageIndex + 1);
        }

        public override void InvalidatePage(int pageIndex)
        {
            if (_pageContainers.ContainsKey(pageIndex))
            {
                _ = RenderPageAsync(pageIndex);
            }
        }

        public override void InvalidateAll()
        {
            RenderCurrentPages();
        }

        private void RenderCurrentPages()
        {
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _layoutManager == null)
                return;

            ClearAllPages();

            int leftPage = _currentBasePage;
            int rightPage = _currentBasePage + 1;

            if (leftPage < mPDFDoc.PageCount)
            {
                CreatePageContainer(leftPage);
            }

            if (rightPage < mPDFDoc.PageCount)
            {
                CreatePageContainer(rightPage);
            }
        }

        private void CreatePageContainer(int pageIndex)
        {
            if (_layoutManager == null)
                return;

            var pagePos = _layoutManager.GetPagePosition(pageIndex);
            float scale = GetCurrentScale();
            float pageWidth = vPageGetWidth(pageIndex) * scale;
            float pageHeight = vPageGetHeight(pageIndex) * scale;

            var container = new PageContainer
            {
                PageIndex = pageIndex
            };

            Canvas.SetLeft(container, pagePos.x);
            Canvas.SetTop(container, pagePos.y);
            container.Width = pageWidth;
            container.Height = pageHeight;

            PageCanvas.Children.Add(container);
            _pageContainers[pageIndex] = container;

            _ = RenderPageAsync(pageIndex);
        }

        private void ClearAllPages()
        {
            foreach (var container in _pageContainers.Values)
            {
                PageCanvas.Children.Remove(container);
            }
            _pageContainers.Clear();
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
            if (mPDFDoc == null || !mPDFDoc.IsOpened || _renderService == null)
                return;

            if (!_pageContainers.ContainsKey(pageIndex))
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
                _scale = GetCurrentScale();

                int renderWidth = (int)(pageWidth * _scale);
                int renderHeight = (int)(pageHeight * _scale);

                var options = new RenderOptions
                {
                    Scale = _scale,
                    RenderMode = RD_RENDER_MODE.mode_best,
                    ShowAnnotations = true
                };

                var bitmap = await _renderService.RenderPageAsync(page, renderWidth, renderHeight, options, cts.Token);
                
                if (bitmap != null && !cts.Token.IsCancellationRequested && _pageContainers.TryGetValue(pageIndex, out var container))
                {
                    container.PageImageControl.Source = bitmap;
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

        private void PageCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
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

            RaisePagePointerPressed(args);
        }

        private void PageCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
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
                _renderService?.ClearCache();
                CancelAllRenders();
                
                InitializeLayout();
                RenderCurrentPages();
            }
        }
    }
}
