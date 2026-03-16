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
        string GenerateCacheKey(int pageIndex, int width, int height, RenderOptions options);
        void CacheRenderedPage(string cacheKey, WriteableBitmap bitmap);
        WriteableBitmap? GetCachedPage(string cacheKey);
        void ClearCache();
        void ClearCache(int pageIndex);
        Task<WriteableBitmap?> RefreshPageCacheAsync(int pageIndex, PDFPage page, int width, int height, RenderOptions options, CancellationToken cancellationToken = default);
        RDMatrix CreateTransformMatrix(float scale, float offsetX, float offsetY, float pageHeight);
    }
}
