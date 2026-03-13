using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RadaeeWinUI.ViewModels;
using Windows.UI;

namespace RadaeeWinUI.Controls
{
    public sealed partial class AnnotationToolbar : UserControl
    {
        public PDFViewModel ViewModel { get; set; }

        public AnnotationToolbar()
        {
            this.InitializeComponent();
            ColorPicker.SelectionChanged += ColorPicker_SelectionChanged;
        }

        private void ColorPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null && ColorPicker.SelectedItem is ComboBoxItem item && item.Tag is string colorString)
            {
                ViewModel.StrokeColor = ParseColor(colorString);
            }
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
