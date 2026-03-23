namespace RadaeeWinUI.Models
{
    /// <summary>
    /// Represents a single search result in the PDF document
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// Page index where the result is found
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// Index of the first character in the search result
        /// </summary>
        public int FirstCharIndex { get; set; }

        /// <summary>
        /// Index of the last character in the search result
        /// </summary>
        public int LastCharIndex { get; set; }

        /// <summary>
        /// The matched text content
        /// </summary>
        public string MatchedText { get; set; } = string.Empty;

        public SearchResult(int pageIndex, int firstCharIndex, int lastCharIndex, string matchedText)
        {
            PageIndex = pageIndex;
            FirstCharIndex = firstCharIndex;
            LastCharIndex = lastCharIndex;
            MatchedText = matchedText;
        }
    }
}
