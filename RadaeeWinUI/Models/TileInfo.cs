using System;

namespace RadaeeWinUI.Models
{
    public enum TilePriority
    {
        High = 0,
        Medium = 1,
        Low = 2
    }

    public class TileInfo
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public TilePriority Priority { get; set; }

        public string GetCacheKey(int pageIndex, float scale, bool showAnnotations)
        {
            return $"tile_{pageIndex}_{Row}_{Col}_{Width}_{Height}_{scale:F2}_{showAnnotations}";
        }

        public bool IntersectsViewport(Windows.Foundation.Rect viewport)
        {
            var tileRect = new Windows.Foundation.Rect(X, Y, Width, Height);
            tileRect.Intersect(viewport);
            return !tileRect.IsEmpty;
        }

        public double GetViewportOverlapRatio(Windows.Foundation.Rect viewport)
        {
            var tileRect = new Windows.Foundation.Rect(X, Y, Width, Height);
            tileRect.Intersect(viewport);
            if (tileRect.IsEmpty)
                return 0.0;

            double overlapArea = tileRect.Width * tileRect.Height;
            double tileArea = Width * Height;
            return tileArea > 0 ? overlapArea / tileArea : 0.0;
        }
    }
}
