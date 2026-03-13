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
        private readonly ConcurrentDictionary<int, CacheEntry> _pageCache = new();
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _renderTasks = new();
        private readonly LinkedList<int> _lruList = new();
        private readonly object _lruLock = new();
        private const int MaxCacheSize = 50;
        private int _cacheAccessCounter = 0;

        private class CacheEntry
        {
            public WriteableBitmap Bitmap { get; set; }
            public int LastAccessTime { get; set; }
            public LinkedListNode<int>? LruNode { get; set; }

            public CacheEntry(WriteableBitmap bitmap, int accessTime)
            {
                Bitmap = bitmap;
                LastAccessTime = accessTime;
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

        public void CacheRenderedPage(int pageIndex, WriteableBitmap bitmap)
        {
            lock (_lruLock)
            {
                if (_pageCache.TryGetValue(pageIndex, out var existingEntry))
                {
                    if (existingEntry.LruNode != null)
                    {
                        _lruList.Remove(existingEntry.LruNode);
                    }
                    existingEntry.Bitmap = bitmap;
                    existingEntry.LastAccessTime = Interlocked.Increment(ref _cacheAccessCounter);
                    existingEntry.LruNode = _lruList.AddFirst(pageIndex);
                }
                else
                {
                    if (_pageCache.Count >= MaxCacheSize)
                    {
                        var lruPageIndex = _lruList.Last?.Value;
                        if (lruPageIndex.HasValue)
                        {
                            _lruList.RemoveLast();
                            _pageCache.TryRemove(lruPageIndex.Value, out _);
                        }
                    }

                    var entry = new CacheEntry(bitmap, Interlocked.Increment(ref _cacheAccessCounter));
                    entry.LruNode = _lruList.AddFirst(pageIndex);
                    _pageCache[pageIndex] = entry;
                }
            }
        }

        public WriteableBitmap? GetCachedPage(int pageIndex)
        {
            if (_pageCache.TryGetValue(pageIndex, out var entry))
            {
                lock (_lruLock)
                {
                    entry.LastAccessTime = Interlocked.Increment(ref _cacheAccessCounter);
                    if (entry.LruNode != null)
                    {
                        _lruList.Remove(entry.LruNode);
                        entry.LruNode = _lruList.AddFirst(pageIndex);
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
                /*foreach (var cts in _renderTasks.Values)
                {
                    cts.Cancel();
                }
                _renderTasks.Clear();*/
                _pageCache.TryRemove(pageIndex, out _);
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
