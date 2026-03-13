using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using RDUILib;
using Windows.UI;

namespace RadaeeWinUI.Controls
{
    public sealed partial class GraphicAnnotDialog : ContentDialog
    {
        public float StrokeWidth => (float)StrokeWidthSlider.Value;
        
        public uint StrokeColor
        {
            get
            {
                var color = StrokeColorPicker.Color;
                byte alpha = (byte)StrokeTransparencySlider.Value;
                return ((uint)alpha << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
            }
        }

        public Color StrokeColorValue
        {
            get
            {
                var color = StrokeColorPicker.Color;
                byte alpha = (byte)StrokeTransparencySlider.Value;
                return Color.FromArgb(alpha, color.R, color.G, color.B);
            }
        }

        public uint FillColor
        {
            get
            {
                var color = FillColorPicker.Color;
                byte alpha = (byte)FillTransparencySlider.Value;
                return ((uint)alpha << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
            }
        }

        public Color FillColorValue
        {
            get
            {
                var color = FillColorPicker.Color;
                byte alpha = (byte)FillTransparencySlider.Value;
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

        public GraphicAnnotDialog()
        {
            this.InitializeComponent();
            this.XamlRoot = App.MainWindow.Content.XamlRoot;
            
            StrokeWidthSlider.Value = 2.0;
            StrokeTransparencySlider.Value = 255;
            StrokeColorPicker.Color = Color.FromArgb(255, 255, 0, 0);
            
            FillTransparencySlider.Value = 0;
            FillColorPicker.Color = Color.FromArgb(255, 255, 255, 255);
        }

        public GraphicAnnotDialog(PDFAnnot annot) : this()
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
                byte strokeAlpha = (byte)((strokeColor >> 24) & 0xFF);
                byte strokeRed = (byte)((strokeColor >> 16) & 0xFF);
                byte strokeGreen = (byte)((strokeColor >> 8) & 0xFF);
                byte strokeBlue = (byte)(strokeColor & 0xFF);

                StrokeColorPicker.Color = Color.FromArgb(255, strokeRed, strokeGreen, strokeBlue);
                StrokeTransparencySlider.Value = strokeAlpha;

                uint fillColor = (uint)annot.FillColor;
                byte fillAlpha = (byte)((fillColor >> 24) & 0xFF);
                byte fillRed = (byte)((fillColor >> 16) & 0xFF);
                byte fillGreen = (byte)((fillColor >> 8) & 0xFF);
                byte fillBlue = (byte)(fillColor & 0xFF);

                FillColorPicker.Color = Color.FromArgb(255, fillRed, fillGreen, fillBlue);
                FillTransparencySlider.Value = fillAlpha;

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

        private void OnStrokeTransparencyChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (StrokeTransparencyText != null)
            {
                double percentage = (e.NewValue / 255.0) * 100.0;
                StrokeTransparencyText.Text = $"{percentage:F0}%";
            }
        }

        private void OnFillTransparencyChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (FillTransparencyText != null)
            {
                double percentage = (e.NewValue / 255.0) * 100.0;
                FillTransparencyText.Text = $"{percentage:F0}%";
            }
        }

        private void OnDeleteButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            IsDeleteRequested = true;
        }
    }
}
