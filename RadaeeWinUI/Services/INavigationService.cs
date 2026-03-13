namespace RadaeeWinUI.Services
{
    public interface INavigationService
    {
        int CurrentPageIndex { get; }
        int TotalPages { get; }
        bool CanGoToNextPage { get; }
        bool CanGoToPreviousPage { get; }
        
        void SetTotalPages(int totalPages);
        bool GoToNextPage();
        bool GoToPreviousPage();
        bool GoToPage(int pageIndex);
    }
}
