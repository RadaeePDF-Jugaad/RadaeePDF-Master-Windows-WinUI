using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RadaeeWinUI.ViewModels;
using System;
using System.ComponentModel;

namespace RadaeeWinUI.Controls
{
    public sealed partial class SearchToolbar : UserControl
    {
        public PDFViewModel ViewModel { get; set; }
        public event EventHandler CloseRequested;

        private bool _isSearchActive = false;

        public SearchToolbar()
        {
            this.InitializeComponent();
            this.Loaded += SearchToolbar_Loaded;
            this.Unloaded += SearchToolbar_Unloaded;
        }

        private void SearchToolbar_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void SearchToolbar_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PDFViewModel.SearchProgress))
            {
                UpdateResultsText();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchButton.IsEnabled = !string.IsNullOrWhiteSpace(SearchTextBox.Text);
        }

        private void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && SearchButton.IsEnabled)
            {
                PerformSearch();
                e.Handled = true;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private async void PerformSearch()
        {
            if (ViewModel == null || string.IsNullOrWhiteSpace(SearchTextBox.Text))
                return;

            string searchText = SearchTextBox.Text;
            bool matchCase = MatchCaseCheckBox.IsChecked ?? false;
            bool wholeWord = WholeWordCheckBox.IsChecked ?? false;

            int resultCount = await ViewModel.PerformSearch(searchText, matchCase, wholeWord);

            _isSearchActive = true;
            UpdateButtonStates();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null || !_isSearchActive)
                return;

            int currentIndex = ViewModel.CurrentSearchIndex;
            int totalResults = ViewModel.SearchResultCount;

            if (currentIndex >= totalResults - 1)
            {
                ShowNoMoreFoundMessage();
                return;
            }

            ViewModel.SearchNext();
            UpdateResultsText();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null || !_isSearchActive)
                return;

            int currentIndex = ViewModel.CurrentSearchIndex;

            if (currentIndex <= 0)
            {
                ShowNoMoreFoundMessage();
                return;
            }

            ViewModel.SearchPrevious();
            UpdateResultsText();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetSearch();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ClearSearch();
            }
            SearchTextBox.Text = string.Empty;
            _isSearchActive = false;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ResetSearch()
        {
            if (ViewModel != null)
            {
                ViewModel.ClearSearch();
            }

            SearchTextBox.Text = string.Empty;
            _isSearchActive = false;
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool hasText = !string.IsNullOrWhiteSpace(SearchTextBox.Text);
            SearchButton.IsEnabled = hasText;

            NextButton.IsEnabled = _isSearchActive;
            PrevButton.IsEnabled = _isSearchActive;
            ResetButton.IsEnabled = _isSearchActive;
        }

        private void UpdateResultsText()
        {
            if (ViewModel == null)
            {
                ResultsTextBlock.Text = string.Empty;
                return;
            }

            // Use SearchProgress from ViewModel if available
            if (!string.IsNullOrEmpty(ViewModel.SearchProgress))
            {
                ResultsTextBlock.Text = ViewModel.SearchProgress;
            }
            else if (!_isSearchActive)
            {
                ResultsTextBlock.Text = string.Empty;
            }
        }

        private async void ShowNoMoreFoundMessage()
        {
            var dialog = new ContentDialog
            {
                Title = "Search",
                Content = "No more found",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
