using System;
using System.Threading.Tasks;
using RDUILib;
using RadaeeWinUI.Helpers;
using RadaeeWinUI.Models;
using RadaeeWinUI.Services;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace RadaeeWinUI.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly IDocumentManager _documentManager;
        private readonly INavigationService _navigationService;
        private readonly PDFViewModel _pdfViewModel;

        private PDFDoc? _currentDocument;
        private DocumentInfo? _documentInfo;
        private bool _isDocumentLoaded;

        public MainViewModel(
            IDocumentManager documentManager,
            INavigationService navigationService,
            PDFViewModel pdfViewModel)
        {
            _documentManager = documentManager;
            _navigationService = navigationService;
            _pdfViewModel = pdfViewModel;

            _pdfViewModel.CurrentPageChanged += _pdfPageChanged;

            OpenDocumentCommand = new AsyncRelayCommand(OpenDocumentAsync);
            NextPageCommand = new AsyncRelayCommand(GoToNextPageAsync, () => _navigationService.CanGoToNextPage);
            PreviousPageCommand = new AsyncRelayCommand(GoToPreviousPageAsync, () => _navigationService.CanGoToPreviousPage);
        }

        private void _pdfPageChanged(object? sender, PageChangedEventArgs e)
        {
            _navigationService.GoToPage(e.NewPageIndex);
            OnPropertyChanged(nameof(CurrentPageNumber));

        }

        public AsyncRelayCommand OpenDocumentCommand { get; }
        public AsyncRelayCommand NextPageCommand { get; }
        public AsyncRelayCommand PreviousPageCommand { get; }

        public PDFViewModel PDFViewModel => _pdfViewModel;

        public DocumentInfo? DocumentInfo
        {
            get => _documentInfo;
            set => SetProperty(ref _documentInfo, value);
        }

        public bool IsDocumentLoaded
        {
            get => _isDocumentLoaded;
            set => SetProperty(ref _isDocumentLoaded, value);
        }

        public int CurrentPageNumber => _navigationService.CurrentPageIndex + 1;
        public int CurrentPageIndex => _navigationService.CurrentPageIndex;
        public int TotalPages => _navigationService.TotalPages;

        public PDFPage? CurrentPage
        {
            get
            {
                if (_currentDocument == null || !_currentDocument.IsOpened)
                    return null;
                return _currentDocument.GetPage(_navigationService.CurrentPageIndex);
            }
        }

        public async Task OpenDocumentAsync()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".pdf");

            var window = App.MainWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile? file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await LoadDocumentAsync(file);
            }
        }

        private async Task LoadDocumentAsync(StorageFile file, string password = "")
        {
            _pdfViewModel.OnDocumentClosed();
            _documentManager.CloseDocument(_currentDocument);

            _currentDocument = await _documentManager.OpenDocumentAsync(file, password);

            if (_currentDocument != null)
            {
                DocumentInfo = _documentManager.GetDocumentInfo(_currentDocument);
                _navigationService.SetTotalPages(DocumentInfo?.PageCount ?? 0);
                _navigationService.GoToPage(0);
                IsDocumentLoaded = true;

                _pdfViewModel.OnDocumentLoaded(_currentDocument);

                OnPropertyChanged(nameof(CurrentPageNumber));
                OnPropertyChanged(nameof(TotalPages));
                UpdateNavigationCommands();
            }
        }


        private async Task GoToNextPageAsync()
        {
            if (_navigationService.GoToNextPage())
            {
                if (_pdfViewModel.CurrentPDFView != null)
                {
                    _pdfViewModel.CurrentPDFView.vPageGoto(_navigationService.CurrentPageIndex);
                }
                OnPropertyChanged(nameof(CurrentPageNumber));
                UpdateNavigationCommands();
            }
            await Task.CompletedTask;
        }

        private async Task GoToPreviousPageAsync()
        {
            if (_navigationService.GoToPreviousPage())
            {
                if (_pdfViewModel.CurrentPDFView != null)
                {
                    _pdfViewModel.CurrentPDFView.vPageGoto(_navigationService.CurrentPageIndex);
                }
                OnPropertyChanged(nameof(CurrentPageNumber));
                UpdateNavigationCommands();
            }
            await Task.CompletedTask;
        }

        public void GoToPage(int pageIndex)
        {
            if (_navigationService.GoToPage(pageIndex))
            {
                if (_pdfViewModel.CurrentPDFView != null)
                {
                    _pdfViewModel.CurrentPDFView.vPageGoto(_navigationService.CurrentPageIndex);
                }
                OnPropertyChanged(nameof(CurrentPageNumber));
                UpdateNavigationCommands();
            }
        }

        private void UpdateNavigationCommands()
        {
            NextPageCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
        }

        public void Cleanup()
        {
            _pdfViewModel.OnDocumentClosed();
            _documentManager.CloseDocument(_currentDocument);
        }

        public PDFDoc? GetCurrentDocument()
        {
            return _currentDocument;
        }
    }
}
