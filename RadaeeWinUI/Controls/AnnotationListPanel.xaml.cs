using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RadaeeWinUI.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace RadaeeWinUI.Controls
{
    public sealed partial class AnnotationListPanel : UserControl
    {
        private ObservableCollection<AnnotationListItem> _allAnnotations = new();
        private ObservableCollection<AnnotationListItem> _filteredAnnotations = new();

        public event EventHandler<AnnotationData>? AnnotationSelected;

        public AnnotationListPanel()
        {
            this.InitializeComponent();
            AnnotationListView.ItemsSource = _filteredAnnotations;
        }

        public void SetAnnotations(ObservableCollection<AnnotationData> annotations)
        {
            _allAnnotations.Clear();
            foreach (var annot in annotations)
            {
                _allAnnotations.Add(new AnnotationListItem(annot));
            }
            ApplyFilter(SearchBox.Text);
        }

        public void AddAnnotation(AnnotationData annotation)
        {
            var item = new AnnotationListItem(annotation);
            _allAnnotations.Add(item);
            if (string.IsNullOrWhiteSpace(SearchBox.Text) || MatchesFilter(item, SearchBox.Text))
            {
                _filteredAnnotations.Add(item);
            }
        }

        public void RemoveAnnotation(AnnotationData annotation)
        {
            var item = _allAnnotations.FirstOrDefault(a => a.Data == annotation);
            if (item != null)
            {
                _allAnnotations.Remove(item);
                _filteredAnnotations.Remove(item);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(SearchBox.Text);
        }

        private void ApplyFilter(string searchText)
        {
            _filteredAnnotations.Clear();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                foreach (var item in _allAnnotations)
                {
                    _filteredAnnotations.Add(item);
                }
            }
            else
            {
                foreach (var item in _allAnnotations.Where(a => MatchesFilter(a, searchText)))
                {
                    _filteredAnnotations.Add(item);
                }
            }
        }

        private bool MatchesFilter(AnnotationListItem item, string searchText)
        {
            searchText = searchText.ToLower();
            return item.TypeName.ToLower().Contains(searchText) ||
                   item.Content.ToLower().Contains(searchText) ||
                   item.PageInfo.ToLower().Contains(searchText);
        }

        private void AnnotationListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnnotationListView.SelectedItem is AnnotationListItem item)
            {
                AnnotationSelected?.Invoke(this, item.Data);
            }
        }
    }

    public class AnnotationListItem
    {
        public AnnotationData Data { get; }

        public AnnotationListItem(AnnotationData data)
        {
            Data = data;
        }

        public string TypeName => Data.Type.ToString();
        
        public string TypeIcon => Data.Type switch
        {
            AnnotationType.TextNote => "📝",
            AnnotationType.Highlight => "🖍",
            AnnotationType.Underline => "▁",
            AnnotationType.Strikeout => "̶",
            AnnotationType.Rectangle => "▭",
            AnnotationType.Ellipse => "○",
            AnnotationType.Line => "─",
            AnnotationType.Ink => "✏",
            _ => "📄"
        };

        public string PageInfo => $"Page {Data.PageIndex + 1}";
        
        public string Content => string.IsNullOrWhiteSpace(Data.Content) ? "" : Data.Content;
        
        public Visibility HasContent => string.IsNullOrWhiteSpace(Data.Content) ? Visibility.Collapsed : Visibility.Visible;
    }
}
