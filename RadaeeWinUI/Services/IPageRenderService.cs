using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using RDUILib;
using RadaeeWinUI.Models;

namespace RadaeeWinUI.Services
{
    public interface IPageRenderService
    {
        Task<WriteableBitmap?> RenderPageAsync(PDFPage page, int width, int height, RenderOptions options, CancellationToken cancellationToken = default);
        void CacheRenderedPage(int pageIndex, WriteableBitmap bitmap);
        WriteableBitmap? GetCachedPage(int pageIndex);
        void ClearCache();
        void ClearCache(int pageIndex);
        RDMatrix CreateTransformMatrix(float scale, float offsetX, float offsetY, float pageHeight);
    }
}
