using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace RadaeeWinUI.Controls
{
    public sealed partial class PDFPageControl : UserControl
    {
        public PDFPageControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty PageBitmapProperty =
            DependencyProperty.Register(
                nameof(PageBitmap),
                typeof(WriteableBitmap),
                typeof(PDFPageControl),
                new PropertyMetadata(null, OnPageBitmapChanged));

        public WriteableBitmap? PageBitmap
        {
            get => (WriteableBitmap?)GetValue(PageBitmapProperty);
            set => SetValue(PageBitmapProperty, value);
        }

        private static void OnPageBitmapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PDFPageControl)d;
            if (e.NewValue is WriteableBitmap bitmap)
            {
                control.PageImage.Source = bitmap;
                control.PageImage.Visibility = Visibility.Visible;
                control.LoadingRing.IsActive = false;
                control.LoadingRing.Visibility = Visibility.Collapsed;
            }
            else
            {
                control.PageImage.Visibility = Visibility.Collapsed;
                control.LoadingRing.IsActive = true;
                control.LoadingRing.Visibility = Visibility.Visible;
            }
        }

        public void ShowLoading()
        {
            PageImage.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
        }
    }
}
