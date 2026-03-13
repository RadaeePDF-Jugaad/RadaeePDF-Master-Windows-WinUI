using System;

namespace RadaeeWinUI.Services
{
    public class NavigationService : INavigationService
    {
        private int _currentPageIndex = 0;
        private int _totalPages = 0;

        public int CurrentPageIndex => _currentPageIndex;
        public int TotalPages => _totalPages;
        public bool CanGoToNextPage => _currentPageIndex < _totalPages - 1;
        public bool CanGoToPreviousPage => _currentPageIndex > 0;

        public void SetTotalPages(int totalPages)
        {
            _totalPages = totalPages;
            if (_currentPageIndex >= _totalPages)
            {
                _currentPageIndex = Math.Max(0, _totalPages - 1);
            }
        }

        public bool GoToNextPage()
        {
            if (CanGoToNextPage)
            {
                _currentPageIndex++;
                return true;
            }
            return false;
        }

        public bool GoToPreviousPage()
        {
            if (CanGoToPreviousPage)
            {
                _currentPageIndex--;
                return true;
            }
            return false;
        }

        public bool GoToPage(int pageIndex)
        {
            if (pageIndex >= 0 && pageIndex < _totalPages)
            {
                _currentPageIndex = pageIndex;
                return true;
            }
            return false;
        }
    }
}
