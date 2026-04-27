using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using RDUILib;
using RadaeeWinUI.Models;
using Windows.Foundation;

namespace RadaeeWinUI.Services
{
    public class PageRenderService : IPageRenderService
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _pageCache = new();
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _renderTasks = new();
        private readonly LinkedList<string> _lruList = new();
        private readonly object _lruLock = new();
        private const int MaxCacheSize = 50;
        private int _cacheAccessCounter = 0;

        private readonly ConcurrentDictionary<string, byte[]> _tileCache = new();
        private const int MaxTileCacheEntries = 200;

        // Serializes all native PDF library calls (PDF_Page_render, RDDIB, RDMatrix, etc.)
        // to prevent concurrent access from multiple Task.Run threads which causes
        // memory corruption crashes in the statically linked native library.
        private readonly SemaphoreSlim _nativeRenderLock = new(1, 1);

        private class CacheEntry
        {
            public WriteableBitmap Bitmap { get; set; }
            public int LastAccessTime { get; set; }
            public LinkedListNode<string>? LruNode { get; set; }
            public string CacheKey { get; set; }
            public int PageIndex { get; set; }

            public CacheEntry(WriteableBitmap bitmap, int accessTime, string cacheKey, int pageIndex)
            {
                Bitmap = bitmap;
                LastAccessTime = accessTime;
                CacheKey = cacheKey;
                PageIndex = pageIndex;
            }
        }

        public async Task<WriteableBitmap?> RenderPageAsync(PDFPage page, int width, int height, RenderOptions options, CancellationToken cancellationToken = default)
        {
            try
            {
                byte[]? pixelData = await Task.Run(async () =>
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return null;

                        await _nativeRenderLock.WaitAsync(cancellationToken);
                        try
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return null;

                            RDDIB dib = new RDDIB(width, height);
                            RDMatrix mat = CreateTransformMatrix(options.Scale, 0, 0, height);

                            if (cancellationToken.IsCancellationRequested)
                                return null;

                            page.RenderPrepare();
                            //bool success = page.Render(dib, mat, options.ShowAnnotations, options.RenderMode);
                            bool success = page.Render(dib, mat, options.ShowAnnotations, options.RenderMode);

                            if (success)
                            {
                                return dib.Data;
                            }

                            return null;
                        }
                        finally
                        {
                            _nativeRenderLock.Release();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return null;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error rendering page: {ex.Message}");
                        return null;
                    }
                }, cancellationToken);

                if (pixelData != null && !cancellationToken.IsCancellationRequested)
                {
                    WriteableBitmap bitmap = new WriteableBitmap(width, height);
                    using (var stream = bitmap.PixelBuffer.AsStream())
                    {
                        stream.Write(pixelData, 0, pixelData.Length);
                    }
                    bitmap.Invalidate();
                    return bitmap;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating bitmap: {ex.Message}");
                return null;
            }
        }

        public string GenerateCacheKey(int pageIndex, int width, int height, RenderOptions options)
        {
            return $"{pageIndex}_{width}_{height}_{(int)options.RenderMode}_{options.ShowAnnotations}";
        }

        public void CacheRenderedPage(string cacheKey, WriteableBitmap bitmap)
        {
            // Extract page index from cache key for page-level operations
            int pageIndex = ExtractPageIndexFromCacheKey(cacheKey);
            
            lock (_lruLock)
            {
                if (_pageCache.TryGetValue(cacheKey, out var existingEntry))
                {
                    if (existingEntry.LruNode != null)
                    {
                        _lruList.Remove(existingEntry.LruNode);
                    }
                    existingEntry.Bitmap = bitmap;
                    existingEntry.LastAccessTime = Interlocked.Increment(ref _cacheAccessCounter);
                    existingEntry.LruNode = _lruList.AddFirst(cacheKey);
                }
                else
                {
                    if (_pageCache.Count >= MaxCacheSize)
                    {
                        var lruCacheKey = _lruList.Last?.Value;
                        if (lruCacheKey != null)
                        {
                            _lruList.RemoveLast();
                            _pageCache.TryRemove(lruCacheKey, out _);
                        }
                    }

                    var entry = new CacheEntry(bitmap, Interlocked.Increment(ref _cacheAccessCounter), cacheKey, pageIndex);
                    entry.LruNode = _lruList.AddFirst(cacheKey);
                    _pageCache[cacheKey] = entry;
                }
            }
        }

        public WriteableBitmap? GetCachedPage(string cacheKey)
        {
            if (_pageCache.TryGetValue(cacheKey, out var entry))
            {
                lock (_lruLock)
                {
                    entry.LastAccessTime = Interlocked.Increment(ref _cacheAccessCounter);
                    if (entry.LruNode != null)
                    {
                        _lruList.Remove(entry.LruNode);
                        entry.LruNode = _lruList.AddFirst(cacheKey);
                    }
                }
                return entry.Bitmap;
            }
            return null;
        }

        public void ClearCache()
        {
            lock (_lruLock)
            {
                foreach (var cts in _renderTasks.Values)
                {
                    cts.Cancel();
                }
                _renderTasks.Clear();
                _pageCache.Clear();
                _lruList.Clear();
            }
        }

        public void ClearCache(int pageIndex)
        {
            lock (_lruLock)
            {
                // Remove all cache entries for this page index
                var keysToRemove = _pageCache.Where(kvp => kvp.Value.PageIndex == pageIndex)
                                             .Select(kvp => kvp.Key)
                                             .ToList();
                
                foreach (var key in keysToRemove)
                {
                    if (_pageCache.TryRemove(key, out var entry))
                    {
                        if (entry.LruNode != null)
                        {
                            _lruList.Remove(entry.LruNode);
                        }
                    }
                }
            }
        }

        public async Task<WriteableBitmap?> RefreshPageCacheAsync(int pageIndex, PDFPage page, int width, int height, RenderOptions options, CancellationToken cancellationToken = default)
        {
            try
            {
                string cacheKey = GenerateCacheKey(pageIndex, width, height, options);
                
                // 1. Remove old cache entry
                ClearCache(pageIndex);
                
                // 2. Render immediately
                var bitmap = await RenderPageAsync(page, width, height, options, cancellationToken);
                
                // 3. Store in cache
                if (bitmap != null && !cancellationToken.IsCancellationRequested)
                {
                    CacheRenderedPage(cacheKey, bitmap);
                }
                
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache refresh failed for page {pageIndex}: {ex.Message}");
                return null;
            }
        }

        private int ExtractPageIndexFromCacheKey(string cacheKey)
        {
            // Cache key format: {pageIndex}_{width}_{height}_{renderMode}_{showAnnotations}
            var parts = cacheKey.Split('_');
            if (parts.Length > 0 && int.TryParse(parts[0], out int pageIndex))
            {
                return pageIndex;
            }
            return -1;
        }

        public async Task RenderPageTiledAsync(int pageIndex, PDFPage page, int width, int height, RenderOptions options, Action<TileRenderResult> tileCallback, CancellationToken cancellationToken = default)
        {
            if (width <= 0 || height <= 0)
                return;

            try
            {
                int tileSize = options.TileSize > 0 ? options.TileSize : 512;
                var tiles = GenerateTileGrid(width, height, tileSize);
                AssignTilePriorities(tiles, options.ViewportRect);

                var sortedTiles = tiles.OrderBy(t => (int)t.Priority)
                                       .ThenBy(t => t.Row)
                                       .ThenBy(t => t.Col)
                                       .ToList();

                // Render tiles sequentially in a single Task.Run (COM thread safety).
                // After each tile, invoke tileCallback so the caller can write
                // the tile data into a WriteableBitmap on the UI thread.
                // The _nativeRenderLock is acquired per-tile so that cancellation
                // can be checked between tiles and other page renders can interleave.
                await Task.Run(async () =>
                {
                    try
                    {
                        foreach (var tile in sortedTiles)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            string tileCacheKey = tile.GetCacheKey(pageIndex, options.Scale, options.ShowAnnotations);

                            // Check tile cache
                            if (_tileCache.TryGetValue(tileCacheKey, out var cachedData))
                            {
                                tileCallback(new TileRenderResult(tile, cachedData, true));
                                continue;
                            }

                            // Acquire lock before calling native render code
                            await _nativeRenderLock.WaitAsync(cancellationToken);
                            byte[]? tileData;
                            try
                            {
                                tileData = RenderSingleTile(page, tile, options, height, cancellationToken);
                            }
                            finally
                            {
                                _nativeRenderLock.Release();
                            }

                            if (tileData != null && !cancellationToken.IsCancellationRequested)
                            {
                                CacheTileData(tileCacheKey, tileData);
                                tileCallback(new TileRenderResult(tile, tileData, true));
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected during cancellation
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in tiled rendering: {ex.Message}");
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during cancellation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in tiled rendering outer: {ex.Message}");
            }
        }

        private List<TileInfo> GenerateTileGrid(int pageWidth, int pageHeight, int tileSize)
        {
            var tiles = new List<TileInfo>();
            int cols = (int)Math.Ceiling((double)pageWidth / tileSize);
            int rows = (int)Math.Ceiling((double)pageHeight / tileSize);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int x = col * tileSize;
                    int y = row * tileSize;
                    int w = Math.Min(tileSize, pageWidth - x);
                    int h = Math.Min(tileSize, pageHeight - y);

                    tiles.Add(new TileInfo
                    {
                        Row = row,
                        Col = col,
                        X = x,
                        Y = y,
                        Width = w,
                        Height = h,
                        Priority = TilePriority.Low
                    });
                }
            }

            return tiles;
        }

        private void AssignTilePriorities(List<TileInfo> tiles, Rect? viewportRect)
        {
            if (viewportRect == null || viewportRect.Value.IsEmpty)
            {
                // No viewport info: all tiles are high priority
                foreach (var tile in tiles)
                    tile.Priority = TilePriority.High;
                return;
            }

            var vp = viewportRect.Value;

            foreach (var tile in tiles)
            {
                double overlapRatio = tile.GetViewportOverlapRatio(vp);

                if (overlapRatio >= 0.5)
                    tile.Priority = TilePriority.High;
                else if (overlapRatio > 0)
                    tile.Priority = TilePriority.Medium;
                else
                    tile.Priority = TilePriority.Low;
            }
        }

        private byte[]? RenderSingleTile(PDFPage page, TileInfo tile, RenderOptions options, int pageHeightPixels, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                RDDIB tileDib = new RDDIB(tile.Width, tile.Height);

                // Reset tile to background color
                tileDib.Reset(options.BackgroundColor);

                // Create transform matrix for this tile:
                // The matrix maps PDF coordinates to pixel coordinates.
                // For the full page: mat = (scale, 0, 0, -scale, 0, pageHeight)
                // For a tile at pixel offset (tileX, tileY): shift by (-tileX, -tileY)
                // So: mat = (scale, 0, 0, -scale, -tileX, pageHeight - tileY)
                RDMatrix tileMat = new RDMatrix(
                    options.Scale, 0, 0, -options.Scale,
                    -tile.X, pageHeightPixels - tile.Y
                );

                if (cancellationToken.IsCancellationRequested)
                    return null;

                page.RenderPrepare();
                bool success = page.Render(tileDib, tileMat, options.ShowAnnotations, options.RenderMode);
                if (success)
                {
                    return tileDib.Data;
                }

                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering single tile: {ex.Message}");
                return null;
            }
        }

        private void ComposeTileIntoBuffer(byte[] tileData, TileInfo tile, byte[] composedBuffer, int pageWidth)
        {
            int bytesPerPixel = 4;
            int tileStride = tile.Width * bytesPerPixel;
            int pageStride = pageWidth * bytesPerPixel;

            for (int row = 0; row < tile.Height; row++)
            {
                int srcOffset = row * tileStride;
                int dstOffset = (tile.Y + row) * pageStride + tile.X * bytesPerPixel;

                if (srcOffset + tileStride <= tileData.Length && dstOffset + tileStride <= composedBuffer.Length)
                {
                    Buffer.BlockCopy(tileData, srcOffset, composedBuffer, dstOffset, tileStride);
                }
            }
        }

        private WriteableBitmap? CreateBitmapFromBuffer(byte[] buffer, int width, int height)
        {
            try
            {
                WriteableBitmap bitmap = new WriteableBitmap(width, height);
                using (var stream = bitmap.PixelBuffer.AsStream())
                {
                    stream.Write(buffer, 0, Math.Min(buffer.Length, width * height * 4));
                }
                bitmap.Invalidate();
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating bitmap from buffer: {ex.Message}");
                return null;
            }
        }

        private void CacheTileData(string tileCacheKey, byte[] tileData)
        {
            // Evict oldest entries if cache is full
            if (_tileCache.Count >= MaxTileCacheEntries)
            {
                // Simple eviction: remove first 10% of entries
                var keysToRemove = _tileCache.Keys.Take(MaxTileCacheEntries / 10).ToList();
                foreach (var key in keysToRemove)
                {
                    _tileCache.TryRemove(key, out _);
                }
            }

            _tileCache[tileCacheKey] = tileData;
        }

        private int ExtractPageIndexFromOptions(RenderOptions options)
        {
            // Fallback: return -1 if page index cannot be determined from options alone
            // The caller should provide the proper page index via the cache key
            return -1;
        }

        public void ClearTileCache()
        {
            _tileCache.Clear();
        }

        public void ClearTileCache(int pageIndex)
        {
            var keysToRemove = _tileCache.Keys
                .Where(k => k.StartsWith($"tile_{pageIndex}_"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _tileCache.TryRemove(key, out _);
            }
        }

        public void CancelRender(int pageIndex)
        {
            if (_renderTasks.TryRemove(pageIndex, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        public int GetCacheSize()
        {
            return _pageCache.Count;
        }

        public RDMatrix CreateTransformMatrix(float scale, float offsetX, float offsetY, float pageHeight)
        {
            return new RDMatrix(scale, 0, 0, -scale, offsetX, pageHeight);
        }
    }
}
