using Microsoft.UI.Xaml.Controls;
using RDUILib;

namespace RadaeeWinUI.Controls
{
    public sealed partial class TextNoteDialog : ContentDialog
    {
        public string NoteSubject => SubjectTextBox.Text;
        public string NoteContent => ContentTextBox.Text;
        public bool IsDeleteRequested { get; private set; }

        private PDFAnnot? _annot;

        public TextNoteDialog()
        {
            this.InitializeComponent();
            this.XamlRoot = App.MainWindow.Content.XamlRoot;
        }

        public TextNoteDialog(PDFAnnot annot) : this()
        {
            _annot = annot;
            if (annot != null)
            {
                SubjectTextBox.Text = annot.PopupSubject ?? string.Empty;
                ContentTextBox.Text = annot.PopupText ?? string.Empty;
            }
        }

        private void OnDeleteButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            IsDeleteRequested = true;
        }
    }
}
