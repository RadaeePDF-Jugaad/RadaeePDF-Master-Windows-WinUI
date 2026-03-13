using System.Collections.Generic;
using RadaeeWinUI.Models;

namespace RadaeeWinUI.Services
{
    public interface ILayoutManager
    {
        ViewMode CurrentViewMode { get; set; }
        double PageSpacing { get; set; }
        
        void Initialize(int totalPages, double containerWidth, double containerHeight);
        List<PageLayoutInfo> CalculateLayout(double scrollOffsetX, double scrollOffsetY, double viewportWidth, double viewportHeight);
        (double width, double height) GetTotalSize();
        (double x, double y) GetPagePosition(int pageIndex);
        void UpdatePageSize(int pageIndex, double width, double height);
    }
}
