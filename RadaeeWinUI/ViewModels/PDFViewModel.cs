using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using RadaeeWinUI.Controls;
using RadaeeWinUI.Controls.PDFView;
using RadaeeWinUI.Helpers;
using RadaeeWinUI.Models;
using RadaeeWinUI.RadaeeUtil;
using RadaeeWinUI.Services;
using RDUILib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using static System.Net.Mime.MediaTypeNames;
namespace RadaeeWinUI.ViewModels
{
    public class PDFViewModel : ObservableObject
    {
        /// <summary>
        /// PDFViewModel is similar to PDFLayoutView in Android project.
        /// It is responsible for:
        ///  * Processing with touch and gesture events on PDFView
        ///  * Managing status STA.EVENT, STA.ZOOM, etc.
        ///  * Managing PDF view for page rendering based on different view mode and status
        /// </summary>
        private readonly IAnnotationManager _annotationManager;
        private readonly IPageRenderService _renderService;
        private readonly ILayoutManager _layoutManager;
        private PDFDoc? _currentDocument;
        private Controls.FormTextInput? _activeFormInput;
        private int _activeFormInputPageIndex = -1;
        private PDFPageState _currentState = PDFPageState.Normal;
        private Controls.PDFView.PDFView? _currentPDFView;
        private ViewMode _viewMode = ViewMode.SinglePage;
        private AnnotationType _selectedAnnotationType = AnnotationType.None;
        private AnnotationData? _selectedAnnotation;
        private PDFAnnot? _selectedAnnotObject;
        private uint _strokeColor = 0xFFFF0000;
        private uint _fillColor = 0x0;
        private float _strokeWidth = 2.0f;
        private bool _isAnnotationMode;
        private bool _pointPressed = false;
        private Point? _startPoint = null;
        private Point? _annotStartPoint = null;
        private RDInk? _currentInk = null;
        private List<Windows.Foundation.Point> _inkScreenPoints = new();
        private PDFSel? _pdfSel = null;
        private PDFPage? _annotPage = null;
        private int _annotPageIndex = -1;
        private ObservableCollection<AnnotationData> _annotations = new();
        // Search functionality fields
        private Dictionary<int, List<SearchResult>> _searchResultsByPage = new();
        private List<SearchResult>? _flatSearchResults = null;
        private int _currentSearchIndex = -1;
        private string _lastSearchText = string.Empty;
        private bool _lastMatchCase = false;
        private bool _lastWholeWord = false;
        private Dictionary<int, List<Microsoft.UI.Xaml.Shapes.Rectangle>> _searchHighlights = new();
        private CancellationTokenSource? _searchCancellationTokenSource;
        private bool _isSearching = false;
        private string _searchProgress = string.Empty;
        private readonly object _searchResultsLock = new object();
        public event EventHandler<PageChangedEventArgs> CurrentPageChanged;
        public PDFViewModel(IAnnotationManager annotationManager, IPageRenderService renderService, ILayoutManager layoutManager)
        {
            _annotationManager = annotationManager;
            _renderService = renderService;
            _layoutManager = layoutManager;
            SelectAnnotationToolCommand = new RelayCommand<AnnotationType>(SelectAnnotationTool);
            DeleteAnnotationCommand = new AsyncRelayCommand(DeleteSelectedAnnotationAsync, () => _selectedAnnotation != null);
        }
        public RelayCommand<AnnotationType> SelectAnnotationToolCommand { get; }
        public AsyncRelayCommand DeleteAnnotationCommand { get; }
        public PDFPageState CurrentState
        {
            get => _currentState;
            set
            {
                if (SetProperty(ref _currentState, value))
                {
                    if (_currentPDFView != null)
                        _currentPDFView.DragScrollEnabled = value == PDFPageState.Normal;
                }
            }
        }
        public Controls.PDFView.PDFView? CurrentPDFView
        {
            get => _currentPDFView;
            private set
            {
                if (SetProperty(ref _currentPDFView, value) && value != null)
                {
                    value.DragScrollEnabled = _currentState == PDFPageState.Normal;
                }
            }
        }
        public ViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                if (SetProperty(ref _viewMode, value))
                {
                    OnViewModeChanged();
                }
            }
        }
        private void OnViewModeChanged()
        {
        }
        public AnnotationType SelectedAnnotationType
        {
            get => _selectedAnnotationType;
            set
            {
                if (SetProperty(ref _selectedAnnotationType, value))
                {
                    IsAnnotationMode = value != AnnotationType.None;
                }
            }
        }
        public AnnotationData? SelectedAnnotation
        {
            get => _selectedAnnotation;
            set
            {
                if (SetProperty(ref _selectedAnnotation, value))
                {
                    DeleteAnnotationCommand.RaiseCanExecuteChanged();
                }
            }
        }
        public Windows.UI.Color StrokeColor
        {
            get
            {
                return Windows.UI.Color.FromArgb(
                    (byte)((_strokeColor >> 24) & 0xFF),
                    (byte)((_strokeColor >> 16) & 0xFF),
                    (byte)((_strokeColor >> 8) & 0xFF),
                    (byte)(_strokeColor & 0xFF));
            }
            set
            {
                _strokeColor = ((uint)value.A << 24) | ((uint)value.R << 16) | ((uint)value.G << 8) | value.B;
            }
        }
        public Windows.UI.Color FillColor
        {
            get
            {
                return Windows.UI.Color.FromArgb(
                    (byte)((_fillColor >> 24) & 0xFF),
                    (byte)((_fillColor >> 16) & 0xFF),
                    (byte)((_fillColor >> 8) & 0xFF),
                    (byte)(_fillColor & 0xFF));
            }
            set
            {
                _fillColor = ((uint)value.A << 24) | ((uint)value.R << 16) | ((uint)value.G << 8) | value.B;
            }
        }
        public float StrokeWidth
        {
            get => _strokeWidth;
            set => SetProperty(ref _strokeWidth, value);
        }
        public bool IsAnnotationMode
        {
            get => _isAnnotationMode;
            set => SetProperty(ref _isAnnotationMode, value);
        }
        public ObservableCollection<AnnotationData> Annotations
        {
            get => _annotations;
            set => SetProperty(ref _annotations, value);
        }

        public int SearchResultCount
        {
            get
            {
                lock (_searchResultsLock)
                {
                    return _searchResultsByPage.Values.Sum(list => list.Count);
                }
            }
        }
        public int CurrentSearchIndex => _currentSearchIndex;
        public bool IsSearching
        {
            get => _isSearching;
            private set => SetProperty(ref _isSearching, value);
        }
        public string SearchProgress
        {
            get => _searchProgress;
            private set => SetProperty(ref _searchProgress, value);
        }

        public void ClearAnnotationCanvas()
        {
            // Clear all visible page annotation canvases
            if (CurrentPDFView != null)
            {
                var visiblePages = CurrentPDFView.GetVisiblePageIndices();
                foreach (var pageIndex in visiblePages)
                {
                    var canvas = CurrentPDFView.GetPageAnnotationCanvas(pageIndex);
                    if (canvas != null)
                    {
                        canvas.Children.Clear();
                    }
                }
            }
        }
        public void ClearAnnotationCanvas(int pageIndex)
        {
            // Clear specific page annotation canvas
            if (CurrentPDFView != null)
            {
                var canvas = CurrentPDFView.GetPageAnnotationCanvas(pageIndex);
                if (canvas != null)
                {
                    canvas.Children.Clear();
                }
            }
        }
        private void HideFormInput()
        {
            if (_activeFormInput != null && CurrentPDFView != null && _activeFormInputPageIndex >= 0)
            {
                var canvas = CurrentPDFView.GetPageAnnotationCanvas(_activeFormInputPageIndex);
                if (canvas != null)
                {
                    canvas.Children.Remove(_activeFormInput);
                }
                _activeFormInput = null;
                _activeFormInputPageIndex = -1;
            }
        }
        private async void ShowFormTextInput(PDFAnnot annot, PDFPage page, int pageIndex)
        {
            if (CurrentPDFView == null)
                return;
            var canvas = CurrentPDFView.GetPageAnnotationCanvas(pageIndex);
            if (canvas == null)
                return;
            HideFormInput();
            int editType = annot.EditType;
            RDRect rect = annot.EditTextRect;
            string currentText = annot.EditText ?? string.Empty;
            // Get page position to calculate page-local coordinates
            var pagePosition = CurrentPDFView.GetPagePosition(pageIndex);
            float pageOriginX = pagePosition.x;
            float pageOriginY = pagePosition.y;
            // Calculate screen coordinates
            var screen = PDFToScreen(rect.left, rect.bottom, pageIndex);
            float screenWidth = (rect.right - rect.left) * CurrentPDFView.PDFScale;
            float screenHeight = (rect.bottom - rect.top) * CurrentPDFView.PDFScale;
            // Convert to page-local coordinates
            float pageLocalX = screen.screenX - pageOriginX;
            float pageLocalY = screen.screenY - pageOriginY;
            _activeFormInput = new Controls.FormTextInput();
            _activeFormInput.Initialize(annot, page, currentText, editType);
            _activeFormInput.Width = screenWidth;
            _activeFormInput.Height = screenHeight;
            _activeFormInput.TextSubmitted += async (s, text) =>
            {
                await RefreshPageAfterEditAsync(pageIndex);
            };
            _activeFormInput.Dismissed += (s, e) =>
            {
                HideFormInput();
            };
            Canvas.SetLeft(_activeFormInput, pageLocalX);
            Canvas.SetTop(_activeFormInput, pageLocalY);
            canvas.Children.Add(_activeFormInput);
            _activeFormInputPageIndex = pageIndex;
        }
        private async Task RefreshPageAfterEditAsync(int pageIndex)
        {
            if (CurrentPDFView == null || _renderService == null || _currentDocument == null)
                return;
            try
            {
                var page = _currentDocument.GetPage(pageIndex);
                if (page == null)
                    return;
                // Get render parameters based on current view type
                int renderWidth, renderHeight;
                RenderOptions options;
                float pageWidth = CurrentPDFView.vPageGetWidth(pageIndex);
                float pageHeight = CurrentPDFView.vPageGetHeight(pageIndex);
                float scale;
                if (CurrentPDFView is SinglePageView singleView)
                {
                    double availableWidth = singleView.ActualWidth > 0 ? singleView.ActualWidth : 800;
                    double availableHeight = singleView.ActualHeight > 0 ? singleView.ActualHeight : 600;
                    scale = (float)Math.Min(availableWidth / pageWidth, availableHeight / pageHeight) * CurrentPDFView.ZoomLevel;
                }
                else if (CurrentPDFView is VerticalScrollView vertView)
                {
                    double viewportWidth = vertView.ActualWidth > 0 ? vertView.ActualWidth : 800;
                    scale = (float)(viewportWidth / pageWidth);
                }
                else if (CurrentPDFView is HorizontalScrollView horzView)
                {
                    double viewportHeight = horzView.ActualHeight > 0 ? horzView.ActualHeight : 600;
                    scale = (float)(viewportHeight / pageHeight);
                }
                else if (CurrentPDFView is DualPageView dualView)
                {
                    double viewportWidth = dualView.ActualWidth > 0 ? dualView.ActualWidth : 800;
                    scale = (float)(viewportWidth / (pageWidth * 2)) * CurrentPDFView.ZoomLevel;
                }
                else if (CurrentPDFView is DualPageScrollView dualScrollView)
                {
                    double viewportWidth = dualScrollView.ActualWidth > 0 ? dualScrollView.ActualWidth : 800;
                    float basePageWidth = CurrentPDFView.vPageGetWidth(0);
                    scale = (float)(viewportWidth / (basePageWidth * 2)) * CurrentPDFView.ZoomLevel;
                }
                else
                {
                    // Fallback for unknown view types
                    _renderService.ClearCache(pageIndex);
                    CurrentPDFView.InvalidatePage(pageIndex);
                    return;
                }
                renderWidth = (int)(pageWidth * scale);
                renderHeight = (int)(pageHeight * scale);
                options = new RenderOptions
                {
                    Scale = scale,
                    RenderMode = RD_RENDER_MODE.mode_best,
                    ShowAnnotations = true
                };
                // Synchronously refresh the cache
                await _renderService.RefreshPageCacheAsync(pageIndex, page, renderWidth, renderHeight, options);
                // Trigger view update
                CurrentPDFView.InvalidatePage(pageIndex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to refresh page {pageIndex} after edit: {ex.Message}");
                // Fallback to old method
                _renderService.ClearCache(pageIndex);
                CurrentPDFView.InvalidatePage(pageIndex);
            }
        }
        private async void SelectAnnotationTool(AnnotationType type)
        {
            if (type == AnnotationType.Select)
            {
                CurrentState = PDFPageState.Selection;
            }
            else if (type == AnnotationType.Ink
                || type == AnnotationType.Rectangle
                || type == AnnotationType.Ellipse
                || type == AnnotationType.Line
                || type == AnnotationType.Polygon
                || type == AnnotationType.TextNote)
            {
                if (SelectedAnnotationType != type)
                {
                    SelectedAnnotationType = type;
                    CurrentState = PDFPageState.Annotation;
                }
                else
                {
                    SelectedAnnotationType = AnnotationType.None;
                    CurrentState = PDFPageState.Normal;
                }
            }
            else if (type == AnnotationType.Highlight
                || type == AnnotationType.Strikeout
                || type == AnnotationType.Underline)
            {
                if (_pdfSel == null || _currentState != PDFPageState.Selection)
                    return;
                PDFPage page = _pdfSel.GetPage();
                int markupType = 0;
                if (type == AnnotationType.Underline)
                    markupType = 1;
                else if (type == AnnotationType.Strikeout)
                    markupType = 2;
                page.AddAnnotMarkup(_pdfSel.Index1, _pdfSel.Index2, _strokeColor, markupType);
                ClearAnnotationCanvas();
                await RefreshPageAfterEditAsync(_pdfSel.PageIndex);
                _pdfSel.Clear();
            }
        }
        public async Task LoadAnnotationsForPageAsync(PDFPage page, int pageIndex)
        {
            var annotations = await _annotationManager.GetAnnotationsOnPageAsync(page, pageIndex);
            Annotations.Clear();
            foreach (var annot in annotations)
            {
                Annotations.Add(annot);
            }
        }
        public async Task<PDFAnnot?> AddTextNoteAsync(PDFPage page, float x, float y, string content)
        {
            return await _annotationManager.AddTextNoteAsync(page, x, y, content);
        }
        public async Task<PDFAnnot?> AddHighlightAsync(PDFPage page, int charIndex1, int charIndex2)
        {
            return await _annotationManager.AddHighlightAsync(page, charIndex1, charIndex2, StrokeColor);
        }
        public async Task<PDFAnnot?> AddUnderlineAsync(PDFPage page, int charIndex1, int charIndex2)
        {
            return await _annotationManager.AddUnderlineAsync(page, charIndex1, charIndex2, StrokeColor);
        }
        public async Task<PDFAnnot?> AddStrikeoutAsync(PDFPage page, int charIndex1, int charIndex2)
        {
            return await _annotationManager.AddStrikeoutAsync(page, charIndex1, charIndex2, StrokeColor);
        }
        public async Task<PDFAnnot?> AddRectangleAsync(PDFPage page, float x, float y, float width, float height)
        {
            if (CurrentPDFView == null)
                return null;
            return await _annotationManager.AddRectangleAsync(page, x, y, width, height, StrokeWidth / CurrentPDFView.PDFScale, StrokeColor, FillColor);
        }
        public async Task<PDFAnnot?> AddEllipseAsync(PDFPage page, float x, float y, float width, float height)
        {
            if (CurrentPDFView == null)
                return null;
            return await _annotationManager.AddEllipseAsync(page, x, y, width, height, StrokeWidth / CurrentPDFView.PDFScale, StrokeColor, FillColor);
        }
        public async Task<PDFAnnot?> AddLineAsync(PDFPage page, float x1, float y1, float x2, float y2, int style1 = 0, int style2 = 0)
        {
            if (CurrentPDFView == null)
                return null;
            return await _annotationManager.AddLineAsync(page, x1, y1, x2, y2, style1, style2, StrokeWidth / CurrentPDFView.PDFScale, StrokeColor, FillColor);
        }
        public async Task<bool> AddInkAsync(PDFPage page, RDInk ink)
        {
            if (CurrentPDFView == null)
                return false;
            return await _annotationManager.AddInkAsync(page, ink, StrokeWidth / CurrentPDFView.PDFScale, StrokeColor);
        }
        public async Task<bool> UpdateAnnotationAsync(PDFAnnot annot, AnnotationData data)
        {
            return await _annotationManager.UpdateAnnotationAsync(annot, data);
        }
        private async Task DeleteSelectedAnnotationAsync()
        {
            if (_selectedAnnotObject != null)
            {
                bool success = await _annotationManager.DeleteAnnotationAsync(_selectedAnnotObject);
                if (success)
                {
                    if (_selectedAnnotation != null)
                    {
                        Annotations.Remove(_selectedAnnotation);
                    }
                    _selectedAnnotObject = null;
                    SelectedAnnotation = null;
                }
            }
            await Task.CompletedTask;
        }
        public void SelectAnnotation(PDFAnnot? annot, AnnotationData? data)
        {
            _selectedAnnotObject = annot;
            SelectedAnnotation = data;
        }
        public async Task<PDFAnnot?> GetAnnotationAtAsync(PDFPage page, float x, float y)
        {
            return await _annotationManager.GetAnnotationAtAsync(page, x, y);
        }
        public async Task<PDFAnnot?> AddAnnotationAsync(PDFPage page, AnnotationData data)
        {
            PDFAnnot? result = null;
            switch (data.Type)
            {
                case AnnotationType.TextNote:
                    result = await AddTextNoteAsync(page, data.X, data.Y, data.Content);
                    break;
                case AnnotationType.Rectangle:
                    result = await AddRectangleAsync(page, data.X, data.Y, data.Width, data.Height);
                    break;
                case AnnotationType.Ellipse:
                    result = await AddEllipseAsync(page, data.X, data.Y, data.Width, data.Height);
                    break;
                case AnnotationType.Line:
                    result = await AddLineAsync(page, data.X, data.Y, data.X + data.Width, data.Y + data.Height);
                    break;
            }
            return result;
        }
        public void ClearSelection()
        {
            HideFormInput();
            SelectedAnnotation = null;
            SelectedAnnotationType = AnnotationType.None;
        }
        public void SwitchViewMode(ViewMode mode)
        {
            if (CurrentPDFView != null)
            {
                ClearAnnotationCanvas();
                CurrentPDFView.PagePointerPressed -= OnPagePointerPressed;
                CurrentPDFView.PagePointerMoved -= OnPagePointerMoved;
                CurrentPDFView.PagePointerReleased -= OnPagePointerReleased;
                CurrentPDFView.CurrentPageChanged -= OnCurrentPageChanged;
                CurrentPDFView.PinchGesture -= OnPinchGesture;
                CurrentPDFView.PanGesture -= OnPanGesture;
                CurrentPDFView.SingleTap -= OnSingleTap;
                CurrentPDFView.DoubleTap -= OnDoubleTap;
                CurrentPDFView.LongPress -= OnLongPress;
                CurrentPDFView.PDFVClose();
            }
            Controls.PDFView.PDFView? newView = mode switch
            {
                ViewMode.SinglePage => CreateSinglePageView(),
                ViewMode.VerticalContinuous => CreateVerticalScrollView(),
                ViewMode.HorizontalContinuous => CreateHorizontalScrollView(),
                ViewMode.DualPage => CreateDualPageView(),
                ViewMode.DualPageContinuous => CreateDualPageScrollView(),
                _ => CreateVerticalScrollView()
            };
            if (newView != null)
            {
                InitializeView(newView, mode);
            }
        }
        private Controls.PDFView.PDFView CreateSinglePageView()
        {
            var view = new Controls.PDFView.SinglePageView();
            view.SetRenderService(_renderService);
            return view;
        }
        private Controls.PDFView.PDFView CreateVerticalScrollView()
        {
            var view = new Controls.PDFView.VerticalScrollView();
            view.SetRenderService(_renderService);
            view.SetLayoutManager(_layoutManager);
            return view;
        }
        private Controls.PDFView.PDFView CreateHorizontalScrollView()
        {
            var view = new Controls.PDFView.HorizontalScrollView();
            view.SetRenderService(_renderService);
            view.SetLayoutManager(_layoutManager);
            return view;
        }
        private Controls.PDFView.PDFView CreateDualPageView()
        {
            var view = new Controls.PDFView.DualPageView();
            view.SetRenderService(_renderService);
            view.SetLayoutManager(_layoutManager);
            return view;
        }
        private Controls.PDFView.PDFView CreateDualPageScrollView()
        {
            var view = new Controls.PDFView.DualPageScrollView();
            view.SetRenderService(_renderService);
            view.SetLayoutManager(_layoutManager);
            return view;
        }
        private void InitializeView(Controls.PDFView.PDFView view, ViewMode mode)
        {
            if (_currentDocument != null && _currentDocument.IsOpened)
            {
                view.PDFVOpen(_currentDocument);
            }
            view.PagePointerPressed += OnPagePointerPressed;
            view.PagePointerMoved += OnPagePointerMoved;
            view.PagePointerReleased += OnPagePointerReleased;
            view.CurrentPageChanged += OnCurrentPageChanged;
            view.PinchGesture += OnPinchGesture;
            view.PanGesture += OnPanGesture;
            view.SingleTap += OnSingleTap;
            view.DoubleTap += OnDoubleTap;
            view.LongPress += OnLongPress;
            CurrentPDFView = view;
            ViewMode = mode;
            ClearAnnotationCanvas();
            RefreshSearchHighlights();
        }
        private void OnPagePointerPressed(object? sender, PDFPointerEventArgs e)
        {
            HandlePointerPressed(e);
        }
        private void OnPagePointerMoved(object? sender, PDFPointerEventArgs e)
        {
            HandlePointerMoved(e);
        }
        private void OnPagePointerReleased(object? sender, PDFPointerEventArgs e)
        {
            HandlePointerReleased(e);
        }
        private void OnCurrentPageChanged(object? sender, PageChangedEventArgs e)
        {
            Debug.WriteLine($"on page changed, old page index:  {e.OldPageIndex}; new page index: {e.NewPageIndex}");
            HideFormInput();
            RefreshSearchHighlights();
            CurrentPageChanged.Invoke(this, e);
        }
        public void OnDocumentLoaded(PDFDoc doc)
        {
            _currentDocument = doc;
            if (CurrentPDFView != null && doc != null && doc.IsOpened)
            {
                CurrentPDFView.PDFVOpen(doc);
            }
        }
        public void OnDocumentClosed()
        {
            HideFormInput();
            ClearAnnotationCanvas();
            CancelCurrentSearch();
            if (CurrentPDFView != null)
            {
                CurrentPDFView.PDFVClose();
            }
            _currentDocument = null;
        }
        public void HandlePointerPressed(PDFPointerEventArgs e)
        {
            switch (CurrentState)
            {
                case PDFPageState.Normal:
                    break;
                case PDFPageState.Annotation:
                    HandleAnnotationPointerPressed(e);
                    break;
                case PDFPageState.Zoom:
                    break;
                case PDFPageState.Selection:
                    _pointPressed = true;
                    _annotPageIndex = e.PageIndex;
                    PDFPage page = _currentDocument.GetPage(_annotPageIndex);
                    if (page != null)
                    {
                        if (_pdfSel != null)
                            _pdfSel.Clear();
                        _pdfSel = new PDFSel(page, e.PageIndex);
                        _annotStartPoint = new Point((int)e.PDFX, (int)e.PDFY);
                        _pdfSel.SetSel(e.PDFX, e.PDFY, e.PDFX, e.PDFY);
                    }
                    break;
                case PDFPageState.HandTool:
                    break;
            }
        }
        private void _drawAnnot(PDFPointerEventArgs e)
        {
            if (_startPoint == null || CurrentPDFView == null || _annotPageIndex < 0)
                return;
            // Clear the annotation canvas for the current annotation page
            ClearAnnotationCanvas(_annotPageIndex);
            Point endPoint = new Point((int)e.ScreenX, (int)e.ScreenY);
            switch (SelectedAnnotationType)
            {
                case AnnotationType.Rectangle:
                    drawRectangle(endPoint, _annotPageIndex);
                    break;
                case AnnotationType.Ellipse:
                    drawEllipse(endPoint, _annotPageIndex);
                    break;
                case AnnotationType.Line:
                    drawLine(endPoint, _annotPageIndex);
                    break;
                case AnnotationType.Polygon:
                    drawPolygon(endPoint, _annotPageIndex);
                    break;
                case AnnotationType.Ink:
                    if (_currentInk != null)
                    {
                        if (_annotPageIndex == e.PageIndex)
                        {
                            _currentInk.Move(e.PDFX, e.PDFY);
                        }
                        else
                        {
                            float pdfX = CurrentPDFView.ToPDFX(e.ScreenX, _annotPageIndex);
                            float pdfY = CurrentPDFView.ToPDFY(e.ScreenY, _annotPageIndex);
                            _currentInk.Move(pdfX, pdfY);
                        }
                        _inkScreenPoints.Add(new Windows.Foundation.Point(e.ScreenX, e.ScreenY));
                        drawInk();
                    }
                    break;
                default:
                    break;
            }
        }
        private void drawRectangle(Point endPoint, int pageIndex)
        {
            if (CurrentPDFView == null || _startPoint == null)
                return;
            Canvas? canvas = CurrentPDFView.GetPageAnnotationCanvas(pageIndex);
            if (canvas == null)
                return;
            var pagePosition = CurrentPDFView.GetPagePosition(pageIndex);
            float pageOriginX = pagePosition.x;
            float pageOriginY = pagePosition.y;
            double left = Math.Min(_startPoint.Value.X - pageOriginX, endPoint.X - pageOriginX);
            double top = Math.Min(_startPoint.Value.Y - pageOriginY, endPoint.Y - pageOriginY);
            double width = Math.Abs(endPoint.X - _startPoint.Value.X);
            double height = Math.Abs(endPoint.Y - _startPoint.Value.Y);
            var rect = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(StrokeColor),
                StrokeThickness = _strokeWidth,
                Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            canvas.Children.Add(rect);
        }
        private void drawEllipse(Point endPoint, int pageIndex)
        {
            if (CurrentPDFView == null || _startPoint == null)
                return;
            Canvas? canvas = CurrentPDFView.GetPageAnnotationCanvas(pageIndex);
            if (canvas == null)
                return;
            var pagePosition = CurrentPDFView.GetPagePosition(pageIndex);
            float pageOriginX = pagePosition.x;
            float pageOriginY = pagePosition.y;
            double left = Math.Min(_startPoint.Value.X - pageOriginX, endPoint.X - pageOriginX);
            double top = Math.Min(_startPoint.Value.Y - pageOriginY, endPoint.Y - pageOriginY);
            double width = Math.Abs(endPoint.X - _startPoint.Value.X);
            double height = Math.Abs(endPoint.Y - _startPoint.Value.Y);
            var ellipse = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = width,
                Height = height,
                Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(StrokeColor),
                StrokeThickness = _strokeWidth,
                Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            Canvas.SetLeft(ellipse, left);
            Canvas.SetTop(ellipse, top);
            canvas.Children.Add(ellipse);
        }
        private void drawLine(Point endPoint, int pageIndex)
        {
            if (CurrentPDFView == null || _startPoint == null)
                return;
            Canvas? canvas = CurrentPDFView.GetPageAnnotationCanvas(pageIndex);
            if (canvas == null)
                return;
            var pagePosition = CurrentPDFView.GetPagePosition(pageIndex);
            float pageOriginX = pagePosition.x;
            float pageOriginY = pagePosition.y;
            var line = new Microsoft.UI.Xaml.Shapes.Line
            {
                X1 = _startPoint.Value.X - pageOriginX,
                Y1 = _startPoint.Value.Y - pageOriginY,
                X2 = endPoint.X - pageOriginX,
                Y2 = endPoint.Y - pageOriginY,
                Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(StrokeColor),
                StrokeThickness = _strokeWidth
            };
            canvas.Children.Add(line);
        }
        private void drawPolygon(Point endPoint, int pageIndex)
        {
            if (CurrentPDFView == null || _startPoint == null)
                return;
            Canvas? canvas = CurrentPDFView.GetPageAnnotationCanvas(pageIndex);
            if (canvas == null)
                return;
            var pagePosition = CurrentPDFView.GetPagePosition(pageIndex);
            float pageOriginX = pagePosition.x;
            float pageOriginY = pagePosition.y;
            double startX = _startPoint.Value.X - pageOriginX;
            double startY = _startPoint.Value.Y - pageOriginY;
            double endX = endPoint.X - pageOriginX;
            double endY = endPoint.Y - pageOriginY;
            var polygon = new Microsoft.UI.Xaml.Shapes.Polygon
            {
                Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(StrokeColor),
                StrokeThickness = _strokeWidth,
                Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                Points = new Microsoft.UI.Xaml.Media.PointCollection
                {
                    new Windows.Foundation.Point(startX, startY),
                    new Windows.Foundation.Point(endX, startY),
                    new Windows.Foundation.Point(endX, endY),
                    new Windows.Foundation.Point(startX, endY)
                }
            };
            canvas.Children.Add(polygon);
        }
        private void drawInk()
        {
            if (CurrentPDFView == null || _currentInk == null || _inkScreenPoints.Count == 0 || _annotPageIndex < 0)
                return;
            Canvas? canvas = CurrentPDFView.GetPageAnnotationCanvas(_annotPageIndex);
            if (canvas == null)
                return;
            var pagePosition = CurrentPDFView.GetPagePosition(_annotPageIndex);
            float pageOriginX = pagePosition.x;
            float pageOriginY = pagePosition.y;
            var pathGeometry = new Microsoft.UI.Xaml.Media.PathGeometry();
            var pathFigure = new Microsoft.UI.Xaml.Media.PathFigure
            {
                StartPoint = new Windows.Foundation.Point(
                    _inkScreenPoints[0].X - pageOriginX,
                    _inkScreenPoints[0].Y - pageOriginY)
            };
            for (int i = 1; i < _inkScreenPoints.Count; i++)
            {
                pathFigure.Segments.Add(new Microsoft.UI.Xaml.Media.LineSegment
                {
                    Point = new Windows.Foundation.Point(
                        _inkScreenPoints[i].X - pageOriginX,
                        _inkScreenPoints[i].Y - pageOriginY)
                });
            }
            pathGeometry.Figures.Add(pathFigure);
            var path = new Path
            {
                Data = pathGeometry,
                Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(StrokeColor),
                StrokeThickness = _strokeWidth,
                StrokeLineJoin = Microsoft.UI.Xaml.Media.PenLineJoin.Round,
                StrokeStartLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round,
                StrokeEndLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round
            };
            canvas.Children.Add(path);
        }
        public void HandlePointerMoved(PDFPointerEventArgs e)
        {
            if (!_pointPressed)
            {
                return;
            }
            if (CurrentState == PDFPageState.Annotation)
            {
                _drawAnnot(e);
            }
            else if (CurrentState == PDFPageState.Selection)
            {
                if (_pdfSel != null && _annotPageIndex >= 0)
                {
                    _pdfSel.SetSel(_annotStartPoint.Value.X, _annotStartPoint.Value.Y, e.PDFX, e.PDFY);
                    ClearAnnotationCanvas(_annotPageIndex);
                    var canvas = CurrentPDFView.GetPageAnnotationCanvas(_annotPageIndex);
                    if (canvas != null)
                    {
                        float scale = CurrentPDFView.PDFScale;
                        _pdfSel.DrawSel(canvas, scale, _currentDocument.GetPageHeight(e.PageIndex));
                    }
                }
            }
        }
        public async void HandlePointerReleased(PDFPointerEventArgs e)
        {
            _pointPressed = false;
            if (_currentDocument == null || CurrentPDFView == null || _startPoint == null || _annotStartPoint == null || !_currentDocument.IsOpened)
                return;
            PDFPage page = _currentDocument.GetPage(e.PageIndex);
            if (_annotPage == null)
            {
                return;
            }
            Point endPoint;
            if (e.PageIndex == _annotPageIndex)
            {
                endPoint = new Point((int)e.PDFX, (int)e.PDFY);
            }
            else
            {
                endPoint = new Point((int)CurrentPDFView.ToPDFX(e.ScreenX, _annotPageIndex), (int)CurrentPDFView.ToPDFY(e.ScreenY, _annotPageIndex));
            }
            float left = Math.Min(_annotStartPoint.Value.X, endPoint.X);
            float top = Math.Min(_annotStartPoint.Value.Y, endPoint.Y);
            float width = Math.Abs(endPoint.X - _annotStartPoint.Value.X);
            float height = Math.Abs(endPoint.Y - _annotStartPoint.Value.Y);
            PDFAnnot? annot = null;
            if (CurrentState == PDFPageState.Annotation)
            {
                _annotPage.ObjsStart();
                switch (SelectedAnnotationType)
                {
                    case AnnotationType.Rectangle:
                        annot = await AddRectangleAsync(_annotPage, left, top, width, height);
                        break;
                    case AnnotationType.Ellipse:
                        annot = await AddEllipseAsync(_annotPage, left, top, width, height);
                        break;
                    case AnnotationType.Line:
                        annot = await AddLineAsync(_annotPage, _annotStartPoint.Value.X, _annotStartPoint.Value.Y, endPoint.X, endPoint.Y);
                        break;
                    case AnnotationType.Polygon:
                        //await AddPolygonAsync(page, left, top, width, height);
                        break;
                    case AnnotationType.Ink:
                        if (_currentInk != null)
                        {
                            if (_annotPageIndex == e.PageIndex)
                            {
                                _currentInk.Up(e.PDFX, e.PDFY);
                            }
                            else
                            {
                                float pdfX = CurrentPDFView.ToPDFX(e.ScreenX, _annotPageIndex);
                                float pdfY = CurrentPDFView.ToPDFY(e.ScreenY, _annotPageIndex);
                                _currentInk.Up(pdfX, pdfY);
                            }
                            if (await AddInkAsync(_annotPage, _currentInk))
                            {
                                await RefreshPageAfterEditAsync(_annotPageIndex);
                            }
                            _currentInk = null;
                            _annotPageIndex = -1;
                            _inkScreenPoints.Clear();
                        }
                        break;
                    default:
                        break;
                }
            }
            if (annot != null)
            {
                await RefreshPageAfterEditAsync(_annotPageIndex);
            }
            ClearAnnotationCanvas();
            _annotPage.Close();
            _startPoint = null;
            _annotPage = null;
            _annotPageIndex = -1;
            _annotStartPoint = null;
        }
        private void HandleAnnotationPointerPressed(PDFPointerEventArgs e)
        {
            _pointPressed = true;
            if (CurrentState == PDFPageState.Annotation)
            {
                switch (SelectedAnnotationType)
                {
                    case AnnotationType.Rectangle:
                        break;
                    case AnnotationType.Ellipse:
                        break;
                    case AnnotationType.Line:
                        break;
                    case AnnotationType.Polygon:
                        break;
                    case AnnotationType.Ink:
                        _currentInk = new RDInk(_strokeWidth / CurrentPDFView.PDFScale, _strokeColor);
                        _currentInk.Down(e.PDFX, e.PDFY);
                        _inkScreenPoints.Clear();
                        _inkScreenPoints.Add(new Windows.Foundation.Point(e.ScreenX, e.ScreenY));
                        break;
                    default:
                        break;
                }
                _startPoint = new Point((int)e.ScreenX, (int)e.ScreenY);
                _annotPage = _currentDocument.GetPage(e.PageIndex);
                _annotPageIndex = e.PageIndex;
                _annotStartPoint = new Point((int)e.PDFX, (int)e.PDFY);
            }
        }
        public (float pdfX, float pdfY) ScreenToPDF(float screenX, float screenY, int pageIndex)
        {
            if (CurrentPDFView == null)
                return (0, 0);
            return (
                CurrentPDFView.ToPDFX(screenX, pageIndex),
                CurrentPDFView.ToPDFY(screenY, pageIndex)
            );
        }
        public (float screenX, float screenY) PDFToScreen(float pdfX, float pdfY, int pageIndex)
        {
            if (CurrentPDFView == null)
                return (0, 0);
            return (
                CurrentPDFView.ToScreenX(pdfX, pageIndex),
                CurrentPDFView.ToScreenY(pdfY, pageIndex)
            );
        }
        private void OnPinchGesture(object? sender, PinchGestureEventArgs e)
        {
            HandlePinchGesture(e);
        }
        private void OnPanGesture(object? sender, PanGestureEventArgs e)
        {
            HandlePanGesture(e);
        }
        private void OnSingleTap(object? sender, SingleTapEventArgs e)
        {
            HandleSingleTap(e);
        }
        private void OnDoubleTap(object? sender, DoubleTapEventArgs e)
        {
            HandleDoubleTap(e);
        }
        private void OnLongPress(object? sender, LongPressEventArgs e)
        {
            HandleLongPress(e);
        }
        public void HandlePinchGesture(PinchGestureEventArgs e)
        {
            if (CurrentPDFView == null)
                return;
            float currentZoom = CurrentPDFView.ZoomLevel;
            float newZoom = currentZoom * e.Scale;
            newZoom = Math.Clamp(newZoom, 0.5f, 5.0f);
            CurrentPDFView.vSetZoom(newZoom);
            // Note: RefreshSearchHighlights will be called automatically after render completes
        }
        public void HandlePanGesture(PanGestureEventArgs e)
        {
            if (CurrentState == PDFPageState.HandTool)
            {
                // Hand tool pan gesture - scroll the view
                // This will be handled by the ScrollViewer in the PDFView
            }
        }
        public async void HandleSingleTap(SingleTapEventArgs e)
        {
            if (_currentDocument == null || !_currentDocument.IsOpened || CurrentPDFView == null)
                return;
            // Single tap can be used for different purposes based on current state
            switch (CurrentState)
            {
                case PDFPageState.Normal:
                    {
                        // Normal mode - could be used for link navigation or other interactions
                        PDFPage page = _currentDocument.GetPage(e.PageIndex);
                        if (page != null)
                        {
                            float pdfX = CurrentPDFView.ToPDFX(e.X, e.PageIndex);
                            float pdfY = CurrentPDFView.ToPDFY(e.Y, e.PageIndex);
                            PDFAnnot annot = page.GetAnnot(pdfX, pdfY);
                            if (annot != null)
                            {
                                processAnnotationTap(annot, page);
                            }
                            else
                            {
                                // Clicked outside any annotation, hide form input if active
                                HideFormInput();
                            }
                        }
                    }
                    break;
                case PDFPageState.Annotation:
                    // Annotation mode - handle annotation creation or selection
                    switch (SelectedAnnotationType)
                    {
                        case AnnotationType.TextNote:
                            {
                                float pdfX = CurrentPDFView.ToPDFX(e.X, e.PageIndex);
                                float pdfY = CurrentPDFView.ToPDFY(e.Y, e.PageIndex);
                                PDFPage page = _currentDocument.GetPage(e.PageIndex);
                                if (page != null)
                                {
                                    page.ObjsStart();
                                    if (page.AddAnnotTextNote(pdfX, pdfY))
                                    {
                                        await RefreshPageAfterEditAsync(e.PageIndex);
                                    }
                                    page.Close();
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case PDFPageState.Selection:
                    // Text selection mode
                    break;
            }
        }
        private async void processAnnotationTap(PDFAnnot annot, PDFPage page)
        {
            switch (annot.Type)
            {
                case 1: // Text note
                    {
                        var dialog = new Controls.TextNoteDialog(annot);
                        var result = await dialog.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            page.ObjsStart();
                            annot.PopupSubject = dialog.NoteSubject;
                            annot.PopupText = dialog.NoteContent;
                            page.Close();
                            if (CurrentPDFView != null && _currentDocument != null)
                            {
                                await RefreshPageAfterEditAsync(CurrentPDFView.CurrentPageIndex);
                            }
                        }
                        else if (result == ContentDialogResult.Secondary)
                        {
                            if (dialog.IsDeleteRequested)
                            {
                                page.ObjsStart();
                                annot.RemoveFromPage();
                                await RefreshPageAfterEditAsync(CurrentPDFView.CurrentPageIndex);
                                page.Close();
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        //Link
                        if (annot.IsURI)
                        {
                            String uri = annot.URI;
                            if (uri != null && uri.Length > 0)
                            {
                                var confirmDialog = new ContentDialog
                                {
                                    Title = "Open Link",
                                    Content = $"Do you want to open the following link?\n{uri}",
                                    PrimaryButtonText = "OK",
                                    CloseButtonText = "Cancel",
                                    XamlRoot = CurrentPDFView?.XamlRoot
                                };
                                var dialogResult = await confirmDialog.ShowAsync();
                                if (dialogResult == ContentDialogResult.Primary)
                                {
                                    await Windows.System.Launcher.LaunchUriAsync(new Uri(uri));
                                }
                            }
                        }
                        else if (annot.IsFileLink)
                        {
                            String filePath = annot.FileLink;
                        }
                        else if (annot.IsRemoteDest)
                        {
                            String remoteDest = annot.RemoteDest;
                        }
                        else if (annot.IsAttachment)
                        {
                            //Attachment
                            String attachmentName = annot.GetAttachmentName();
                        }
                        break;
                    }
                case 4: // Line
                case 15: // Ink
                    {
                        var dialog = new Controls.LineAnnotDialog(annot);
                        var result = await dialog.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            page.ObjsStart();
                            annot.LineStyle = dialog.LineStyle;
                            annot.StrokeColor = (int)dialog.StrokeColor;
                            annot.StrokeWidth = dialog.StrokeWidth;
                            await RefreshPageAfterEditAsync(CurrentPDFView.CurrentPageIndex);
                            page.Close();
                        }
                        else if (result == ContentDialogResult.Secondary)
                        {
                            if (dialog.IsDeleteRequested)
                            {
                                page.ObjsStart();
                                annot.RemoveFromPage();
                                await RefreshPageAfterEditAsync(CurrentPDFView.CurrentPageIndex);
                                page.Close();
                            }
                        }
                        break;
                    }
                case 5: // Rect
                case 6: // Ellipse
                case 7: // Polygon
                    {
                        var dialog = new GraphicAnnotDialog(annot);
                        var result = await dialog.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            page.ObjsStart();
                            annot.LineStyle = dialog.LineStyle;
                            annot.StrokeColor = (int)dialog.StrokeColor;
                            annot.FillColor = (int)dialog.FillColor;
                            annot.StrokeWidth = dialog.StrokeWidth;
                            await RefreshPageAfterEditAsync(CurrentPDFView.CurrentPageIndex);
                            page.Close();
                        }
                        else if (result == ContentDialogResult.Secondary)
                        {
                            if (dialog.IsDeleteRequested)
                            {
                                page.ObjsStart();
                                annot.RemoveFromPage();
                                await RefreshPageAfterEditAsync(CurrentPDFView.CurrentPageIndex);
                                page.Close();
                            }
                        }
                        break;
                    }
                case 20:
                    {
                        //Form
                        /*0: unknown
                         *1: button field
                         *2: text field
                         *3: choice field
                         *4: signature field
                         */
                        int fieldType = annot.FieldType;
                        switch (fieldType)
                        {
                            case 1:
                                {
                                    //Button field
                                    int editType = annot.EditType;
                                    String fieldName = annot.FieldName;
                                    String FieldFullName = annot.FieldFullName;
                                    String FieldFullName2 = annot.FieldFullName2;
                                    String FieldNameWithNO = annot.FieldNameWithNO;
                                    //-1 if annotation is not valid control.
                                    //0 if check-box is not checked.
                                    //1 if check-box checked.
                                    //2 if radio-box is not checked.
                                    //3 if radio-box checked
                                    int status = annot.GetCheckStatus();
                                    if (status == 0 || status == 1)
                                    {
                                        //Check box
                                        annot.SetCheckValue(status == 0);
                                    }
                                    else if (status == 2 || status == 3)
                                    {
                                        //Radio box 
                                        annot.SetRadio();
                                    }
                                    RefreshPageAfterEditAsync(CurrentPDFView.CurrentPageIndex);
                                    break;
                                }
                            case 2:
                                {
                                    //Text field
                                    String fieldName = annot.FieldName;
                                    String FieldFullName = annot.FieldFullName;
                                    String FieldFullName2 = annot.FieldFullName2;
                                    String FieldNameWithNO = annot.FieldNameWithNO;
                                    // -1: this annotation is not text - box.
                                    // 1: normal single line.
                                    // 2: password.
                                    // 3: MultiLine edit area.
                                    int editType = annot.EditType;
                                    RDRect rect = annot.EditTextRect;
                                    if (CurrentPDFView != null)
                                    {
                                        ShowFormTextInput(annot, page, CurrentPDFView.CurrentPageIndex);
                                    }
                                    break;
                                }
                        }
                        break;
                    }
            }
        }
        public void HandleDoubleTap(DoubleTapEventArgs e)
        {
            if (CurrentPDFView == null)
                return;
            float currentZoom = CurrentPDFView.ZoomLevel;
            CurrentPDFView.vSetZoom(currentZoom *= 1.2f);
            // Note: RefreshSearchHighlights will be called automatically after render completes
        }
        public void HandleLongPress(LongPressEventArgs e)
        {
            // Long press can be used for context menu or selection
            // For now, we'll leave it empty for future implementation
        }

        #region Search Functionality
        /// <summary>
        /// Perform search in the PDF document using hybrid mode
        /// Phase 1: Search visible pages immediately
        /// Phase 2: Search remaining pages asynchronously in batches
        /// </summary>
        /// <param name="searchText">Text to search for</param>
        /// <param name="matchCase">Whether to match case</param>
        /// <param name="wholeWord">Whether to match whole word</param>
        /// <returns>Task that completes when search is done</returns>
        public async Task<int> PerformSearch(string searchText, bool matchCase, bool wholeWord)
        {
            if (_currentDocument == null || string.IsNullOrWhiteSpace(searchText) || CurrentPDFView == null)
                return 0;

            // Cancel any existing search
            CancelCurrentSearch();

            // Clear previous search results
            ClearSearch();
            
            _lastSearchText = searchText;
            _lastMatchCase = matchCase;
            _lastWholeWord = wholeWord;
            IsSearching = true;

            // Create new cancellation token
            _searchCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _searchCancellationTokenSource.Token;

            int pageCount = _currentDocument.PageCount;
            
            // Phase 1: Search visible pages immediately (on UI thread)
            var visiblePageIndices = CurrentPDFView.GetVisiblePageIndices();
            var visiblePagesSet = new HashSet<int>(visiblePageIndices ?? new List<int>());
            
            foreach (var pageIndex in visiblePagesSet)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    IsSearching = false;
                    return 0;
                }
                
                SearchPageAndAddResults(pageIndex, searchText, matchCase, wholeWord);
            }

            // Update UI with initial results
            int visibleResultCount = SearchResultCount;
            if (visibleResultCount > 0)
            {
                _currentSearchIndex = 0;
                RenderAllSearchHighlights();
                NavigateToSearchResult(_currentSearchIndex);
            }

            SearchProgress = $"Searching... {visiblePagesSet.Count}/{pageCount} pages, found {visibleResultCount} results";

            // Phase 2: Search remaining pages asynchronously in background
            var remainingPages = new List<int>();
            for (int i = 0; i < pageCount; i++)
            {
                if (!visiblePagesSet.Contains(i))
                {
                    remainingPages.Add(i);
                }
            }

            if (remainingPages.Count > 0)
            {
                await Task.Run(async () =>
                {
                    const int batchSize = 10;
                    int processedPages = visiblePagesSet.Count;

                    for (int batchStart = 0; batchStart < remainingPages.Count; batchStart += batchSize)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        int batchEnd = Math.Min(batchStart + batchSize, remainingPages.Count);
                        var batchResults = new List<SearchResult>();

                        for (int i = batchStart; i < batchEnd; i++)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            int pageIndex = remainingPages[i];
                            var pageResults = SearchPage(pageIndex, searchText, matchCase, wholeWord);
                            batchResults.AddRange(pageResults);
                        }

                        if (cancellationToken.IsCancellationRequested)
                            break;

                        // Update results on UI thread
                        CurrentPDFView.DispatcherQueue.TryEnqueue(() =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return;

                            lock (_searchResultsLock)
                            {
                                // Add batch results to dictionary by page
                                foreach (var result in batchResults)
                                {
                                    if (!_searchResultsByPage.ContainsKey(result.PageIndex))
                                    {
                                        _searchResultsByPage[result.PageIndex] = new List<SearchResult>();
                                    }
                                    _searchResultsByPage[result.PageIndex].Add(result);
                                }
                                // Invalidate flat list cache
                                _flatSearchResults = null;
                            }

                            processedPages += (batchEnd - batchStart);
                            SearchProgress = $"Searching... {processedPages}/{pageCount} pages, found {SearchResultCount} results";

                            // Refresh highlights if we have results and user is still on a page with results
                            if (SearchResultCount > 0 && _currentSearchIndex >= 0)
                            {
                                RenderAllSearchHighlights();
                            }
                        });

                        // Small delay to allow UI updates to process
                        await Task.Delay(10, cancellationToken);
                    }
                }, cancellationToken);
            }

            // Search complete
            if (!cancellationToken.IsCancellationRequested)
            {
                IsSearching = false;
                int totalResults = SearchResultCount;
                if (totalResults > 0)
                {
                    SearchProgress = $"Result {_currentSearchIndex + 1} of {totalResults}";
                }
                else
                {
                    SearchProgress = "No results found";
                }
            }

            return SearchResultCount;
        }

        /// <summary>
        /// Search a single page and return results
        /// </summary>
        private List<SearchResult> SearchPage(int pageIndex, string searchText, bool matchCase, bool wholeWord)
        {
            var results = new List<SearchResult>();
            
            if (_currentDocument == null)
                return results;

            var page = _currentDocument.GetPage(pageIndex);
            if (page == null)
                return results;

            page.ObjsStart();
            var finder = page.GetFinder(searchText, matchCase, wholeWord);
            if (finder != null)
            {
                int resultCount = finder.Count;
                for (int i = 0; i < resultCount; i++)
                {
                    int firstChar = finder.GetFirstChar(i);
                    int lastChar = finder.GetLastChar(i);
                    var result = new SearchResult(pageIndex, firstChar, lastChar, searchText);
                    results.Add(result);
                }
            }
            page.Close();

            return results;
        }

        /// <summary>
        /// Search a single page and add results to dictionary by page
        /// </summary>
        private void SearchPageAndAddResults(int pageIndex, string searchText, bool matchCase, bool wholeWord)
        {
            var results = SearchPage(pageIndex, searchText, matchCase, wholeWord);
            if (results.Count > 0)
            {
                lock (_searchResultsLock)
                {
                    _searchResultsByPage[pageIndex] = results;
                    // Invalidate flat list cache
                    _flatSearchResults = null;
                }
            }
        }

        /// <summary>
        /// Get flattened and sorted list of all search results
        /// </summary>
        private List<SearchResult> GetFlatSearchResults()
        {
            lock (_searchResultsLock)
            {
                if (_flatSearchResults == null)
                {
                    _flatSearchResults = new List<SearchResult>();
                    // Get all page indices and sort them
                    var sortedPageIndices = _searchResultsByPage.Keys.OrderBy(k => k).ToList();
                    // Add results from each page in order
                    foreach (var pageIndex in sortedPageIndices)
                    {
                        var pageResults = _searchResultsByPage[pageIndex];
                        // Sort results within the page by character index
                        var sortedPageResults = pageResults.OrderBy(r => r.FirstCharIndex).ToList();
                        _flatSearchResults.AddRange(sortedPageResults);
                    }
                }
                return _flatSearchResults;
            }
        }

        /// <summary>
        /// Navigate to the next search result
        /// </summary>
        public void SearchNext()
        {
            var results = GetFlatSearchResults();
            if (results.Count == 0)
                return;
            int oldPageIndex = CurrentPDFView?.CurrentPageIndex ?? -1;
            _currentSearchIndex = (_currentSearchIndex + 1) % results.Count;
            NavigateToSearchResult(_currentSearchIndex);
            // If staying on same page, manually refresh highlights
            // (OnCurrentPageChanged won't be triggered)
            if (results[_currentSearchIndex].PageIndex == oldPageIndex)
            {
                RenderAllSearchHighlights();
            }
        }
        /// <summary>
        /// Navigate to the previous search result
        /// </summary>
        public void SearchPrevious()
        {
            var results = GetFlatSearchResults();
            if (results.Count == 0)
                return;
            int oldPageIndex = CurrentPDFView?.CurrentPageIndex ?? -1;
            _currentSearchIndex--;
            if (_currentSearchIndex < 0)
                _currentSearchIndex = results.Count - 1;
            NavigateToSearchResult(_currentSearchIndex);
            // If staying on same page, manually refresh highlights
            // (OnCurrentPageChanged won't be triggered)
            if (results[_currentSearchIndex].PageIndex == oldPageIndex)
            {
                RenderAllSearchHighlights();
            }
        }
        /// <summary>
        /// Cancel current search operation
        /// </summary>
        public void CancelCurrentSearch()
        {
            if (_searchCancellationTokenSource != null)
            {
                _searchCancellationTokenSource.Cancel();
                _searchCancellationTokenSource.Dispose();
                _searchCancellationTokenSource = null;
            }
            IsSearching = false;
        }

        /// <summary>
        /// Clear all search results and highlights
        /// </summary>
        public void ClearSearch()
        {
            // Cancel any ongoing search
            CancelCurrentSearch();

            // Clear all search highlights from canvases
            foreach (var kvp in _searchHighlights)
            {
                int pageIndex = kvp.Key;
                var highlights = kvp.Value;
                var canvas = CurrentPDFView?.GetPageAnnotationCanvas(pageIndex);
                if (canvas != null)
                {
                    foreach (var highlight in highlights)
                    {
                        canvas.Children.Remove(highlight);
                    }
                }
            }
            _searchHighlights.Clear();
            
            lock (_searchResultsLock)
            {
                _searchResultsByPage.Clear();
                _flatSearchResults = null;
            }
            
            _currentSearchIndex = -1;
            _lastSearchText = string.Empty;
            SearchProgress = string.Empty;
        }
        /// <summary>
        /// Render all search highlights on visible pages
        /// </summary>
        private void RenderAllSearchHighlights()
        {
            if (CurrentPDFView == null || _currentDocument == null)
                return;
            
            // Clear existing highlights
            foreach (var kvp in _searchHighlights)
            {
                var canvas = CurrentPDFView.GetPageAnnotationCanvas(kvp.Key);
                if (canvas != null)
                {
                    foreach (var highlight in kvp.Value)
                    {
                        canvas.Children.Remove(highlight);
                    }
                }
            }
            _searchHighlights.Clear();
            
            // Get visible page indices
            var visiblePageIndices = CurrentPDFView.GetVisiblePageIndices();
            if (visiblePageIndices == null || visiblePageIndices.Count == 0)
                return;
            
            // Create a HashSet for faster lookup
            var visiblePagesSet = new HashSet<int>(visiblePageIndices);
            
            // Render highlights only for search results on visible pages
            var results = GetFlatSearchResults();
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                
                // Only render if the result is on a visible page
                if (visiblePagesSet.Contains(result.PageIndex))
                {
                    bool isCurrent = (i == _currentSearchIndex);
                    RenderSearchHighlight(result, isCurrent);
                }
            }
        }
        /// <summary>
        /// Render a single search highlight
        /// </summary>
        /// <param name="result">Search result to highlight</param>
        /// <param name="isCurrent">Whether this is the current search result</param>
        private void RenderSearchHighlight(SearchResult result, bool isCurrent)
        {
            if (CurrentPDFView == null || _currentDocument == null)
                return;
            var canvas = CurrentPDFView.GetPageAnnotationCanvas(result.PageIndex);
            if (canvas == null)
                return;
            var page = _currentDocument.GetPage(result.PageIndex);
            if (page == null)
                return;
            // Get character rectangles for the search result
            page.ObjsStart();
            var rects = new List<RDRect>();
            for (int charIndex = result.FirstCharIndex; charIndex <= result.LastCharIndex; charIndex++)
            {
                var charRect = page.ObjsGetCharRect(charIndex);
                if (charRect != null)
                {
                    rects.Add(charRect);
                }
            }
            if (rects.Count == 0)
                return;
            // Merge adjacent rectangles on the same line
            var mergedRects = MergeCharacterRects(rects);
            // Create highlight rectangles
            var highlightShapes = new List<Microsoft.UI.Xaml.Shapes.Rectangle>();
            foreach (var rect in mergedRects)
            {
                // Convert PDF coordinates to page-local screen coordinates
                var topLeft = PDFToScreen(rect.left, rect.top, result.PageIndex);
                var bottomRight = PDFToScreen(rect.right, rect.bottom, result.PageIndex);
                // Get page position to calculate page-local coordinates
                var pagePosition = CurrentPDFView.GetPagePosition(result.PageIndex);
                float pageOriginX = pagePosition.x;
                float pageOriginY = pagePosition.y;
                // Calculate page-local coordinates and dimensions
                float pageLocalX = Math.Min(topLeft.screenX, bottomRight.screenX) - pageOriginX;
                float pageLocalY = Math.Min(topLeft.screenY, bottomRight.screenY) - pageOriginY;
                float width = Math.Abs(bottomRight.screenX - topLeft.screenX);
                float height = Math.Abs(bottomRight.screenY - topLeft.screenY);
                // Create highlight rectangle
                var highlightRect = new Microsoft.UI.Xaml.Shapes.Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        isCurrent 
                            ? Windows.UI.Color.FromArgb(128, 255, 165, 0)  // Orange for current result
                            : Windows.UI.Color.FromArgb(128, 255, 255, 0)  // Yellow for other results
                    ),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(highlightRect, pageLocalX);
                Canvas.SetTop(highlightRect, pageLocalY);
                canvas.Children.Add(highlightRect);
                highlightShapes.Add(highlightRect);
            }
            page.Close();
            // Store highlights for this page
            if (!_searchHighlights.ContainsKey(result.PageIndex))
            {
                _searchHighlights[result.PageIndex] = new List<Microsoft.UI.Xaml.Shapes.Rectangle>();
            }
            _searchHighlights[result.PageIndex].AddRange(highlightShapes);
        }
        /// <summary>
        /// Merge adjacent character rectangles on the same line
        /// </summary>
        private List<RDRect> MergeCharacterRects(List<RDRect> rects)
        {
            if (rects.Count == 0)
                return new List<RDRect>();
            var merged = new List<RDRect>();
            var current = rects[0];
            for (int i = 1; i < rects.Count; i++)
            {
                var next = rects[i];
                // Check if rectangles are on the same line (similar top/bottom values)
                bool sameLine = Math.Abs(current.top - next.top) < 2 && Math.Abs(current.bottom - next.bottom) < 2;
                // Check if rectangles are adjacent (small gap)
                bool adjacent = Math.Abs(current.right - next.left) < 5;
                if (sameLine && adjacent)
                {
                    // Merge rectangles
                    current = new RDRect
                    {
                        left = current.left,
                        top = Math.Min(current.top, next.top),
                        right = next.right,
                        bottom = Math.Max(current.bottom, next.bottom)
                    };
                }
                else
                {
                    // Add current and start new
                    merged.Add(current);
                    current = next;
                }
            }
            // Add the last rectangle
            merged.Add(current);
            return merged;
        }
        /// <summary>
        /// Navigate to a specific search result
        /// </summary>
        private void NavigateToSearchResult(int index)
        {
            var results = GetFlatSearchResults();
            if (index < 0 || index >= results.Count || CurrentPDFView == null)
                return;
            var result = results[index];
            // Navigate to the page containing the result
            CurrentPDFView.vPageGoto(result.PageIndex);
        }
        /// <summary>
        /// Refresh search highlights when view changes (e.g., zoom, scroll)
        /// </summary>
        public void RefreshSearchHighlights()
        {
            if (SearchResultCount > 0)
            {
                RenderAllSearchHighlights();
            }
        }
        #endregion
    }
}

