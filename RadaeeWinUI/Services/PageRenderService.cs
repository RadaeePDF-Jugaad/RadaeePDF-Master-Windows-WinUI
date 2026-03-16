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
                byte[]? pixelData = await Task.Run(() =>
                {
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
