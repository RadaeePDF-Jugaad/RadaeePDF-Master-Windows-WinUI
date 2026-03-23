using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RadaeeWinUI.Controls
{
    public sealed partial class PageContainer : UserControl
    {
        private int _pageIndex;

        public PageContainer()
        {
            InitializeComponent();
        }

        public Image PageImageControl => PageImage;

        public Canvas AnnotationCanvasControl => AnnotationCanvas;

        public int PageIndex
        {
            get => _pageIndex;
            set => _pageIndex = value;
        }

        public void SetSize(double width, double height)
        {
            Width = width;
            Height = height;
            RootGrid.Width = width;
            RootGrid.Height = height;
            AnnotationCanvas.Width = width;
            AnnotationCanvas.Height = height;
        }

        public void SetPosition(double x, double y)
        {
            Canvas.SetLeft(this, x);
            Canvas.SetTop(this, y);
        }
    }
}
