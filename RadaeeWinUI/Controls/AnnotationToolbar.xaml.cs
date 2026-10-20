using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using RadaeeWinUI.Models;
using RadaeeWinUI.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace RadaeeWinUI.Controls
{
    /// <summary>
    /// Command bar shown while an annotation tool is active.
    /// Done keeps the annotations added in the session, Cancel removes them.
    /// </summary>
    public sealed partial class AnnotationToolbar : UserControl
    {
        private sealed record ToolInfo(string Name, string Glyph, string Hint, bool UsesColor, bool UsesStrokeWidth, bool UsesTextActions = false);

        private static readonly Dictionary<AnnotationType, ToolInfo> Tools = new()
        {
            [AnnotationType.Ink] = new("Ink", "", "Draw freehand strokes on the page", true, true),
            [AnnotationType.TextNote] = new("Note", "", "Tap on the page to add notes", false, false),
            [AnnotationType.Line] = new("Line", "", "Drag on the page to draw lines", true, true),
            [AnnotationType.Rectangle] = new("Rectangle", "", "Drag on the page to draw rectangles", true, true),
            [AnnotationType.Ellipse] = new("Ellipse", "", "Drag on the page to draw ellipses", true, true),
            [AnnotationType.Select] = new("Text Selection", "", "Select text, then choose an action", true, false, true),
            [AnnotationType.Highlight] = new("Highlight", "", "Select text to highlight it", true, false),
            [AnnotationType.Underline] = new("Underline", "", "Select text to underline it", true, false),
            [AnnotationType.Strikeout] = new("Strikeout", "", "Select text to strike it out", true, false),
            [AnnotationType.EditBox] = new("Edit Box", "", "Drag on the page to add text fields", false, false),
        };

        private bool _isClosing;
        private PDFViewModel? _viewModel;

        public PDFViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                if (_viewModel != null)
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _viewModel = value;
                if (_viewModel != null)
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        public AnnotationToolbar()
        {
            this.InitializeComponent();
        }

        public static bool IsSupported(AnnotationType type) => Tools.ContainsKey(type);

        /// <summary>
        /// Updates the bar for the given tool. Called when the edit session starts.
        /// </summary>
        public void ShowTool(AnnotationType type)
        {
            if (!Tools.TryGetValue(type, out var tool))
                return;

            _isClosing = false;
            ToolIcon.Glyph = tool.Glyph;
            ToolNameText.Text = tool.Name;
            ToolHintText.Text = tool.Hint;

            ColorButton.Visibility = tool.UsesColor ? Visibility.Visible : Visibility.Collapsed;
            StrokeWidthPanel.Visibility = tool.UsesStrokeWidth ? Visibility.Visible : Visibility.Collapsed;
            PropertySeparator.Visibility = tool.UsesColor || tool.UsesStrokeWidth || tool.UsesTextActions ? Visibility.Visible : Visibility.Collapsed;

            Visibility textActionsVisibility = tool.UsesTextActions ? Visibility.Visible : Visibility.Collapsed;
            TextActionsSeparator.Visibility = textActionsVisibility;
            CopyTextButton.Visibility = textActionsVisibility;
            HighlightButton.Visibility = textActionsVisibility;
            UnderlineButton.Visibility = textActionsVisibility;
            StrikeoutButton.Visibility = textActionsVisibility;
            SquigglyButton.Visibility = textActionsVisibility;
            UpdateTextActionsEnabled();

            if (ViewModel != null)
            {
                StrokeWidthSlider.Value = ViewModel.StrokeWidth;
                StrokeWidthText.Text = ((int)ViewModel.StrokeWidth).ToString();
                ColorSwatch.Foreground = new SolidColorBrush(ViewModel.StrokeColor);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PDFViewModel.HasTextSelection))
            {
                DispatcherQueue.TryEnqueue(UpdateTextActionsEnabled);
            }
        }

        private void UpdateTextActionsEnabled()
        {
            bool hasSelection = ViewModel?.HasTextSelection == true;
            CopyTextButton.IsEnabled = hasSelection;
            HighlightButton.IsEnabled = hasSelection;
            UnderlineButton.IsEnabled = hasSelection;
            StrikeoutButton.IsEnabled = hasSelection;
            SquigglyButton.IsEnabled = hasSelection;
        }

        private void CopyText_Click(object sender, RoutedEventArgs e)
        {
            string? text = ViewModel?.GetSelectedText();
            if (string.IsNullOrEmpty(text))
                return;

            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
            ViewModel!.ClearTextSelection();
        }

        private async void TextMarkup_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null || sender is not AppBarButton button || button.Tag is not string tag
                || !System.Enum.TryParse(tag, out AnnotationType type))
                return;

            await ViewModel.ApplyMarkupToSelectionAsync(type);
        }

        private void ColorItem_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null || sender is not RadioMenuFlyoutItem item || item.Tag is not string colorString)
                return;

            Color color = ParseColor(colorString);
            ViewModel.StrokeColor = color;
            ColorSwatch.Foreground = new SolidColorBrush(color);
        }

        private void StrokeWidthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            // Raised while InitializeComponent is still creating the named elements.
            if (StrokeWidthText != null)
                StrokeWidthText.Text = ((int)e.NewValue).ToString();
            if (ViewModel != null)
            {
                ViewModel.StrokeWidth = (float)e.NewValue;
            }
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null || _isClosing)
                return;
            _isClosing = true;
            ViewModel.ConfirmAnnotationEdit();
        }

        private async void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null || _isClosing)
                return;
            _isClosing = true;
            await ViewModel.CancelAnnotationEditAsync();
        }

        private Color ParseColor(string hexColor)
        {
            hexColor = hexColor.TrimStart('#');
            byte a = byte.Parse(hexColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte r = byte.Parse(hexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hexColor.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            return Color.FromArgb(a, r, g, b);
        }
    }
}
