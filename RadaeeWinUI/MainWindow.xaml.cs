using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using RadaeeWinUI.Models;
using RadaeeWinUI.RadaeeUtil;
using RadaeeWinUI.Services;
using RadaeeWinUI.ViewModels;
using RDUILib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RadaeeWinUI.Controls.PDFView;
using RadaeeWinUI.Controls;



namespace RadaeeWinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }



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
                if (ViewModel.IsDocumentLoaded)
                {
                    AnnotationToolbar.Visibility = Visibility.Visible;
                }
            };

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.PDFViewModel.PropertyChanged += PDFViewModel_PropertyChanged;
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
                            InitializePDFView();
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
            if (ViewModel.IsDocumentLoaded)
            {
                EmptyMessage.Visibility = Visibility.Collapsed;
                AnnotationToolbar.Visibility = Visibility.Visible;
                PDFViewContainer.Visibility = Visibility.Visible;
                PageNumberText.Text = ViewModel.CurrentPageNumber.ToString();
                TotalPagesText.Text = ViewModel.TotalPages.ToString();
                UpdateZoomLevel();
            }
            else
            {
                EmptyMessage.Visibility = Visibility.Visible;
                AnnotationToolbar.Visibility = Visibility.Collapsed;
                PDFViewContainer.Visibility = Visibility.Collapsed;
                PageNumberText.Text = "0";
                TotalPagesText.Text = "0";
                ZoomLevelText.Text = "100%";
            }
        }

        private void UpdateZoomLevel()
        {
            if (ViewModel.PDFViewModel.CurrentPDFView != null)
            {
                float zoomLevel = ViewModel.PDFViewModel.CurrentPDFView.ZoomLevel;
                ZoomLevelText.Text = $"{(int)(zoomLevel * 100)}%";
            }
            else
            {
                ZoomLevelText.Text = "100%";
            }
        }

        private void InitializePDFView()
        {
            if (!ViewModel.IsDocumentLoaded)
                return;

            var doc = ViewModel.GetCurrentDocument();
            if (doc != null)
            {
                ViewModel.PDFViewModel.SwitchViewMode(ViewMode.SinglePage);
                PDFViewContainer.Content = ViewModel.PDFViewModel.CurrentPDFView;

                if (doc.GetRootOutline() != null)
                {
                    BtnOutline.Visibility = Visibility.Visible;
                }
                else
                {
                    BtnOutline.Visibility = Visibility.Collapsed;
                }

                UpdateAttachmentButtonVisibility();
            }
        }

        private async void ShowPDFOutline()
        {
            var doc = ViewModel.GetCurrentDocument();
            if (doc != null)
            {
                var outline = doc.GetRootOutline();
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
            });
        }

        private async void OpenDocument_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.OpenDocumentAsync();
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
            if (ViewModel.PDFViewModel.CurrentPDFView != null)
            {
                float currentZoom = ViewModel.PDFViewModel.CurrentPDFView.ZoomLevel;
                float newZoom = Math.Min(currentZoom * 1.2f, 5.0f);
                ViewModel.PDFViewModel.CurrentPDFView.vSetZoom(newZoom);
                UpdateZoomLevel();
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.PDFViewModel.CurrentPDFView != null)
            {
                float currentZoom = ViewModel.PDFViewModel.CurrentPDFView.ZoomLevel;
                float newZoom = Math.Max(currentZoom / 1.2f, 0.5f);
                ViewModel.PDFViewModel.CurrentPDFView.vSetZoom(newZoom);
                UpdateZoomLevel();
            }
        }

        private void FitWidth_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.PDFViewModel.CurrentPDFView != null)
            {
                ViewModel.PDFViewModel.CurrentPDFView.vSetZoom(1.0f);
                UpdateZoomLevel();
            }
        }

        private void ViewMode_SinglePage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PDFViewModel.SwitchViewMode(ViewMode.SinglePage);
        }

        private void ViewMode_VerticalContinuous_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PDFViewModel.SwitchViewMode(ViewMode.VerticalContinuous);
        }

        private void ViewMode_HorizontalContinuous_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PDFViewModel.SwitchViewMode(ViewMode.HorizontalContinuous);
        }

        private void ViewMode_DualPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PDFViewModel.SwitchViewMode(ViewMode.DualPageContinuous);
        }

        private void Outline_Click(object sender, RoutedEventArgs e)
        {
            ShowPDFOutline();
        }

        private void UpdateAttachmentButtonVisibility()
        {
            BtnAttachment.Visibility = ViewModel.HasAttachments ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void Attachment_Click(object sender, RoutedEventArgs e)
        {
            var doc = ViewModel.GetCurrentDocument();
            if (doc != null)
            {
                var dialog = new AttachmentListDialog
                {
                    XamlRoot = this.Content.XamlRoot
                };
                dialog.LoadAttachments(doc);
                await dialog.ShowAsync();



                ViewModel.GetCurrentDocument();
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            if (SearchToolbar.Visibility == Visibility.Visible)
            {
                SearchToolbar.Visibility = Visibility.Collapsed;
            }
            else
            {
                AnnotationToolbar.Visibility = Visibility.Collapsed;
                SearchToolbar.Visibility = Visibility.Visible;
            }
        }
    }
}



