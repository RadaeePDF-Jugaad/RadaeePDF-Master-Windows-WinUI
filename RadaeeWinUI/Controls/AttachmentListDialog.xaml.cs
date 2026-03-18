using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RadaeeWinUI.Models;
using RDUILib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace RadaeeWinUI.Controls
{
    public sealed partial class AttachmentListDialog : ContentDialog
    {
        private PDFDoc? _document;
        private ObservableCollection<AttachmentData> _attachments;

        public AttachmentListDialog()
        {
            InitializeComponent();
            _attachments = new ObservableCollection<AttachmentData>();
            AttachmentListView.ItemsSource = _attachments;
        }

        public void LoadAttachments(PDFDoc document)
        {
            _document = document;
            _attachments.Clear();

            if (_document == null)
                return;

            int efCount = _document.EFCount;
            for (int i = 0; i < efCount; i++)
            {
                string name = _document.GetEFName(i);
                string description = _document.GetEFDesc(i);

                _attachments.Add(new AttachmentData
                {
                    Index = i,
                    Name = name,
                    Description = description
                });
            }
        }

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int index)
            {
                if (_document == null)
                    return;

                var savePicker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    SuggestedFileName = _document.GetEFName(index)
                };
                savePicker.FileTypeChoices.Add("All Files", new List<string> { "." });

                var window = App.MainWindow;
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                StorageFile? file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    bool success = _document.GetEFData(index, file.Path);
                    if (success)
                    {
                        ShowStatus($"Attachment saved to: {file.Path}", InfoBarSeverity.Success);
                    }
                    else
                    {
                        ShowStatus("Failed to save attachment.", InfoBarSeverity.Error);
                    }
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int index)
            {
                if (_document == null)
                    return;

                string attachmentName = _document.GetEFName(index);
                
                this.Hide();
                await Task.Delay(100);

                var confirmDialog = new ContentDialog
                {
                    Title = "Confirm Delete",
                    Content = $"Are you sure you want to delete the attachment '{attachmentName}'?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    _document.DelEFData(index);
                    LoadAttachments(_document);
                    ShowStatus("Attachment deleted successfully.", InfoBarSeverity.Success);
                }
                
                await this.ShowAsync();
            }
        }

        private void ShowStatus(string message, InfoBarSeverity severity)
        {
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = severity;
            StatusInfoBar.IsOpen = true;
        }
    }
}
