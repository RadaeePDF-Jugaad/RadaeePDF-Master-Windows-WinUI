using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using RDUILib;
using Windows.UI;

namespace RadaeeWinUI.Controls
{
    public sealed partial class LineAnnotDialog : ContentDialog
    {
        public float StrokeWidth => (float)StrokeWidthSlider.Value;
        
        public uint StrokeColor
        {
            get
            {
                var color = StrokeColorPicker.Color;
                byte alpha = (byte)TransparencySlider.Value;
                return ((uint)alpha << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
            }
        }

        public Color StrokeColorValue
        {
            get
            {
                var color = StrokeColorPicker.Color;
                byte alpha = (byte)TransparencySlider.Value;
                return Color.FromArgb(alpha, color.R, color.G, color.B);
            }
        }

        public int LineStyle
        {
            get
            {
                if (LineStyleComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
                {
                    return int.Parse(tag);
                }
                return 0;
            }
        }

        public bool IsDeleteRequested { get; private set; }

        private PDFAnnot? _annot;

        public LineAnnotDialog()
        {
            this.InitializeComponent();
            this.XamlRoot = App.MainWindow.Content.XamlRoot;
            
            StrokeWidthSlider.Value = 2.0;
            TransparencySlider.Value = 255;
            StrokeColorPicker.Color = Color.FromArgb(255, 255, 0, 0);
        }

        public LineAnnotDialog(PDFAnnot annot) : this()
        {
            _annot = annot;
            if (annot != null)
            {
                LoadAnnotationProperties(annot);
            }
        }

        private void LoadAnnotationProperties(PDFAnnot annot)
        {
            try
            {
                uint strokeColor = (uint)annot.StrokeColor;
                byte alpha = (byte)((strokeColor >> 24) & 0xFF);
                byte red = (byte)((strokeColor >> 16) & 0xFF);
                byte green = (byte)((strokeColor >> 8) & 0xFF);
                byte blue = (byte)(strokeColor & 0xFF);

                StrokeColorPicker.Color = Color.FromArgb(255, red, green, blue);
                TransparencySlider.Value = alpha;

                float strokeWidth = annot.StrokeWidth;
                if (strokeWidth > 0)
                {
                    StrokeWidthSlider.Value = strokeWidth;
                }

                float[] dashPattern = annot.StrokeDash;
                if (dashPattern != null && dashPattern.Length > 0)
                {
                    LineStyleComboBox.SelectedIndex = 1;
                }
                else
                {
                    LineStyleComboBox.SelectedIndex = 0;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading annotation properties: {ex.Message}");
            }
        }

        private void OnStrokeWidthChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (StrokeWidthText != null)
            {
                StrokeWidthText.Text = e.NewValue.ToString("F1");
            }
        }

        private void OnTransparencyChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (TransparencyText != null)
            {
                double percentage = (e.NewValue / 255.0) * 100.0;
                TransparencyText.Text = $"{percentage:F0}%";
            }
        }

        private void OnDeleteButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            IsDeleteRequested = true;
        }
    }
}
