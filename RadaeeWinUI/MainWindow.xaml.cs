using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RDUILib;
using RadaeeWinUI.Models;
using RadaeeWinUI.RadaeeUtil;
using RadaeeWinUI.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using RadaeeWinUI.Controls;



namespace RadaeeWinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        private bool _isConfirmingClose;
        private bool _isCloseConfirmed;



        public MainWindow()
        {
            InitializeComponent();

            if (!initLib())
            {
                Debug.WriteLine("Failed to initialize Radaee library.");
            }

            ViewModel = App.GetService<MainViewModel>();
            AnnotationToolbar.ViewModel = ViewModel.PDFViewModel;
            SearchToolbar.ViewModel = ViewModel.PDFViewModel;
            SearchToolbar.CloseRequested += (s, e) =>
            {
                SearchToolbar.Visibility = Visibility.Collapsed;
            };

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.PDFViewModel.PropertyChanged += PDFViewModel_PropertyChanged;
            ViewModel.DOCXViewModel.PropertyChanged += DOCXViewModel_PropertyChanged;
            AppWindow.Closing += AppWindow_Closing;
            UpdateUI();
        }

        private bool initLib()
        {
            return RadaeeUtil.RDGlobal.init();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ViewModel.IsDocumentLoaded):
                        UpdateUI();
                        if (ViewModel.IsDocumentLoaded)
                        {
                            InitializeDocumentView();
                        }
                        break;
                    case nameof(ViewModel.CurrentPageNumber):
                        PageNumberText.Text = ViewModel.CurrentPageNumber.ToString();
                        break;
                    case nameof(ViewModel.TotalPages):
                        TotalPagesText.Text = ViewModel.TotalPages.ToString();
                        break;
                    case nameof(ViewModel.HasAttachments):
                        UpdateAttachmentButtonVisibility();
                        break;
                }
            });
        }

        private void UpdateUI()
        {
            UpdateCommandBarState();
            if (ViewModel.IsDocumentLoaded)
            {
                EmptyMessage.Visibility = Visibility.Collapsed;
                PageNumberText.Text = ViewModel.CurrentPageNumber.ToString();
                TotalPagesText.Text = ViewModel.TotalPages.ToString();
                UpdateZoomLevel();
            }
            else
            {
                EmptyMessage.Visibility = Visibility.Visible;
                SearchToolbar.Visibility = Visibility.Collapsed;
                PDFViewContainer.Visibility = Visibility.Collapsed;
                DOCXViewContainer.Visibility = Visibility.Collapsed;
                PageNumberText.Text = "0";
                TotalPagesText.Text = "0";
                ZoomLevelText.Text = "100%";
            }
        }

        /// <summary>
        /// Shows only the menu groups that apply to the loaded document type.
        /// </summary>
        private void UpdateCommandBarState()
        {
            bool isLoaded = ViewModel.IsDocumentLoaded;
            bool isPdf = isLoaded && ViewModel.CurrentDocumentType == DocumentType.PDF;
            Visibility documentVisibility = isLoaded ? Visibility.Visible : Visibility.Collapsed;
            Visibility pdfVisibility = isPdf ? Visibility.Visible : Visibility.Collapsed;

            DocumentNameText.Text = isLoaded ? Path.GetFileName(ViewModel.DocumentInfo?.FilePath ?? string.Empty) : string.Empty;
            DocumentCommandsSeparator.Visibility = documentVisibility;
            BtnView.Visibility = documentVisibility;
            BtnTools.Visibility = pdfVisibility;
            BtnAnnotations.Visibility = pdfVisibility;
            BtnFields.Visibility = pdfVisibility;
        }

        /// <summary>
        /// Swaps the main command bar and the annotation edit bar, like enter_annot/enter_normal in the UWP reader.
        /// </summary>
        private void UpdateAnnotationEditUI()
        {
            var pdfViewModel = ViewModel.PDFViewModel;
            if (pdfViewModel.IsAnnotationMode)
            {
                AnnotationToolbar.ShowTool(pdfViewModel.SelectedAnnotationType);
                MainCommandBar.Visibility = Visibility.Collapsed;
                SearchToolbar.Visibility = Visibility.Collapsed;
                AnnotationToolbar.Visibility = Visibility.Visible;
            }
            else
            {
                AnnotationToolbar.Visibility = Visibility.Collapsed;
                MainCommandBar.Visibility = Visibility.Visible;
            }
        }

        private void SetViewModeChecked(ViewMode mode)
        {
            ViewModeSinglePage.IsChecked = mode == ViewMode.SinglePage;
            ViewModeVertical.IsChecked = mode == ViewMode.VerticalContinuous;
            ViewModeHorizontal.IsChecked = mode == ViewMode.HorizontalContinuous;
            ViewModeDualPage.IsChecked = mode == ViewMode.DualPageContinuous;
        }

        private void UpdateZoomLevel()
        {
            if (ViewModel.CurrentDocumentType == DocumentType.PDF && ViewModel.PDFViewModel.CurrentPDFView != null)
            {
                float zoomLevel = ViewModel.PDFViewModel.CurrentPDFView.ZoomLevel;
                ZoomLevelText.Text = $"{(int)(zoomLevel * 100)}%";
            }
            else if (ViewModel.CurrentDocumentType == DocumentType.DOCX && ViewModel.DOCXViewModel.CurrentDOCXView != null)
            {
                float zoomLevel = ViewModel.DOCXViewModel.CurrentDOCXView.ZoomLevel;
                ZoomLevelText.Text = $"{(int)(zoomLevel * 100)}%";
            }
            else
            {
                ZoomLevelText.Text = "100%";
            }
        }

        private void InitializeDocumentView()
        {
            if (!ViewModel.IsDocumentLoaded)
                return;

            var doc = ViewModel.GetCurrentDocument();
            if (doc == null)
                return;

            if (doc.Type == DocumentType.PDF)
            {
                ViewModel.DOCXViewModel.OnDocumentClosed();
                ViewModel.PDFViewModel.SwitchViewMode(ViewMode.SinglePage);
                PDFViewContainer.Content = ViewModel.PDFViewModel.CurrentPDFView;
                PDFViewContainer.Visibility = Visibility.Visible;
                DOCXViewContainer.Visibility = Visibility.Collapsed;

                if (doc is PDFDocumentWrapper pdfWrapper)
                {
                    PDFDoc pdfDoc = pdfWrapper.InnerDoc;
                    MenuOutline.Visibility = pdfDoc.GetRootOutline() != null ? Visibility.Visible : Visibility.Collapsed;
                }

                UpdateAttachmentButtonVisibility();
            }
            else if (doc.Type == DocumentType.DOCX)
            {
                ViewModel.DOCXViewModel.SwitchViewMode(ViewMode.SinglePage);
                DOCXViewContainer.Content = ViewModel.DOCXViewModel.CurrentDOCXView;
                DOCXViewContainer.Visibility = Visibility.Visible;
                PDFViewContainer.Visibility = Visibility.Collapsed;
                SearchToolbar.Visibility = Visibility.Collapsed;
                MenuOutline.Visibility = Visibility.Collapsed;
                MenuAttachment.Visibility = Visibility.Collapsed;
            }
            SetViewModeChecked(ViewMode.SinglePage);
            UpdateCommandBarState();
        }

        private async void ShowPDFOutline()
        {
            var doc = ViewModel.GetCurrentDocument();
            if (doc is PDFDocumentWrapper pdfWrapper)
            {
                var outline = pdfWrapper.InnerDoc.GetRootOutline();
                if (outline != null)
                {
                    var dialog = new OutlineDialog();
                    dialog.LoadOutline(outline);

                    await dialog.ShowAsync();

                    if (dialog.SelectedPageIndex >= 0)
                    {
                        ViewModel.GoToPage(dialog.SelectedPageIndex);
                    }
                }
            }
        }

        private void PDFViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (e.PropertyName == nameof(ViewModel.PDFViewModel.CurrentPDFView))
                {
                    PDFViewContainer.Content = ViewModel.PDFViewModel.CurrentPDFView;
                    UpdateZoomLevel();
                }
                else if (e.PropertyName == nameof(ViewModel.PDFViewModel.IsAnnotationMode))
                {
                    UpdateAnnotationEditUI();
                }
            });
        }

        private void DOCXViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (e.PropertyName == nameof(ViewModel.DOCXViewModel.CurrentDOCXView))
                {
                    DOCXViewContainer.Content = ViewModel.DOCXViewModel.CurrentDOCXView;
                    UpdateZoomLevel();
                }
            });
        }

        private async void OpenDocument_Click(object sender, RoutedEventArgs e)
        {
            var file = await ViewModel.PickDocumentFileAsync();
            if (file == null)
                return;
            if (!await ConfirmCloseDocumentAsync())
                return;
            await ViewModel.OpenFileAsync(file);
        }

        private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (_isCloseConfirmed || !ViewModel.HasUnsavedChanges)
                return;

            // The dialog is asynchronous: cancel this close and close again once the user decided.
            args.Cancel = true;
            if (await ConfirmCloseDocumentAsync())
            {
                _isCloseConfirmed = true;
                Close();
            }
        }

        /// <summary>
        /// Asks whether to save the changes of the current document before it is closed.
        /// </summary>
        /// <returns>true if the document may be closed, false to keep it open</returns>
        private async Task<bool> ConfirmCloseDocumentAsync()
        {
            if (!ViewModel.HasUnsavedChanges)
                return true;
            if (_isConfirmingClose)
                return false;

            _isConfirmingClose = true;
            try
            {
                string fileName = Path.GetFileName(ViewModel.DocumentInfo?.FilePath ?? string.Empty);
                var dialog = new ContentDialog
                {
                    Title = "Save changes?",
                    Content = $"The document \"{fileName}\" has been modified. Do you want to save the changes?",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Don't Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.Content.XamlRoot
                };

                ContentDialogResult result;
                try
                {
                    result = await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    // Only one ContentDialog can be open at a time; keep the document open.
                    Debug.WriteLine($"Failed to show the save dialog: {ex.Message}");
                    return false;
                }

                switch (result)
                {
                    case ContentDialogResult.Primary:
                        if (ViewModel.SaveDocument())
                            return true;
                        var errorDialog = new ContentDialog
                        {
                            Title = "Save failed",
                            Content = $"The document \"{fileName}\" could not be saved. It stays open.",
                            CloseButtonText = "OK",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await errorDialog.ShowAsync();
                        return false;
                    case ContentDialogResult.Secondary:
                        return true;
                    default:
                        return false;
                }
            }
            finally
            {
                _isConfirmingClose = false;
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NextPageCommand.Execute(null);
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PreviousPageCommand.Execute(null);
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CurrentDocumentType == DocumentType.PDF && ViewModel.PDFViewModel.CurrentPDFView != null)
            {
                float currentZoom = ViewModel.PDFViewModel.CurrentPDFView.ZoomLevel;
                float newZoom = Math.Min(currentZoom * 1.2f, 5.0f);
                ViewModel.PDFViewModel.CurrentPDFView.vSetZoom(newZoom);
                UpdateZoomLevel();
            }
            else if (ViewModel.CurrentDocumentType == DocumentType.DOCX && ViewModel.DOCXViewModel.CurrentDOCXView != null)
            {
                float currentZoom = ViewModel.DOCXViewModel.CurrentDOCXView.ZoomLevel;
                float newZoom = Math.Min(currentZoom * 1.2f, 5.0f);
                ViewModel.DOCXViewModel.CurrentDOCXView.vSetZoom(newZoom);
                UpdateZoomLevel();
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CurrentDocumentType == DocumentType.PDF && ViewModel.PDFViewModel.CurrentPDFView != null)
            {
                float currentZoom = ViewModel.PDFViewModel.CurrentPDFView.ZoomLevel;
                float newZoom = Math.Max(currentZoom / 1.2f, 0.5f);
                ViewModel.PDFViewModel.CurrentPDFView.vSetZoom(newZoom);
                UpdateZoomLevel();
            }
            else if (ViewModel.CurrentDocumentType == DocumentType.DOCX && ViewModel.DOCXViewModel.CurrentDOCXView != null)
            {
                float currentZoom = ViewModel.DOCXViewModel.CurrentDOCXView.ZoomLevel;
                float newZoom = Math.Max(currentZoom / 1.2f, 0.5f);
                ViewModel.DOCXViewModel.CurrentDOCXView.vSetZoom(newZoom);
                UpdateZoomLevel();
            }
        }

        private void FitWidth_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CurrentDocumentType == DocumentType.PDF && ViewModel.PDFViewModel.CurrentPDFView != null)
            {
                ViewModel.PDFViewModel.CurrentPDFView.vSetZoom(1.0f);
                UpdateZoomLevel();
            }
            else if (ViewModel.CurrentDocumentType == DocumentType.DOCX && ViewModel.DOCXViewModel.CurrentDOCXView != null)
            {
                ViewModel.DOCXViewModel.CurrentDOCXView.vSetZoom(1.0f);
                UpdateZoomLevel();
            }
        }

        private void ViewMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item || item.Tag is not string tag || !Enum.TryParse(tag, out ViewMode mode))
                return;

            if (ViewModel.CurrentDocumentType == DocumentType.PDF)
                ViewModel.PDFViewModel.SwitchViewMode(mode);
            else if (ViewModel.CurrentDocumentType == DocumentType.DOCX)
                ViewModel.DOCXViewModel.SwitchViewMode(mode);
            SetViewModeChecked(mode);
        }

        private async void AnnotationTool_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item || item.Tag is not string tag || !Enum.TryParse(tag, out AnnotationType type))
                return;
            if (!Controls.AnnotationToolbar.IsSupported(type) || ViewModel.GetCurrentDocument() is not PDFDocumentWrapper pdfWrapper)
                return;

            if (!pdfWrapper.InnerDoc.CanSave)
            {
                var dialog = new ContentDialog
                {
                    Title = "Readonly",
                    Content = "The PDF file is not writable, maybe opened by another app.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            ViewModel.PDFViewModel.BeginAnnotationEdit(type);
        }

        private void Outline_Click(object sender, RoutedEventArgs e)
        {
            ShowPDFOutline();
        }

        private void UpdateAttachmentButtonVisibility()
        {
            MenuAttachment.Visibility = ViewModel.HasAttachments ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void Attachment_Click(object sender, RoutedEventArgs e)
        {
            var doc = ViewModel.GetCurrentDocument();
            if (doc is PDFDocumentWrapper pdfWrapper)
            {
                var dialog = new AttachmentListDialog
                {
                    XamlRoot = this.Content.XamlRoot
                };
                dialog.DocumentModified += (s, args) => ViewModel.PDFViewModel.MarkModified();
                dialog.LoadAttachments(pdfWrapper.InnerDoc);
                await dialog.ShowAsync();
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CurrentDocumentType != DocumentType.PDF)
                return;

            SearchToolbar.Visibility = SearchToolbar.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}



