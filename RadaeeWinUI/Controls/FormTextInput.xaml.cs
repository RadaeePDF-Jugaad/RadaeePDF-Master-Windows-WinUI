using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RDUILib;
using System;
using Windows.System;

namespace RadaeeWinUI.Controls
{
    public sealed partial class FormTextInput : UserControl
    {
        public event EventHandler<string>? TextSubmitted;
        public event EventHandler? Dismissed;

        private PDFAnnot? _annot;
        private PDFPage? _page;
        private int _editType;

        public FormTextInput()
        {
            this.InitializeComponent();
        }

        public void Initialize(PDFAnnot annot, PDFPage page, string initialText, int editType)
        {
            _annot = annot;
            _page = page;
            _editType = editType;

            // Set input mode based on editType
            // 1: normal single line
            // 2: password
            // 3: MultiLine edit area
            switch (editType)
            {
                case 1:
                    // Normal single line text input
                    InputTextBox.Visibility = Visibility.Visible;
                    InputPasswordBox.Visibility = Visibility.Collapsed;
                    InputTextBox.Text = initialText ?? string.Empty;
                    InputTextBox.AcceptsReturn = false;
                    InputTextBox.TextWrapping = TextWrapping.NoWrap;
                    break;
                case 2:
                    // Password input
                    InputTextBox.Visibility = Visibility.Collapsed;
                    InputPasswordBox.Visibility = Visibility.Visible;
                    InputPasswordBox.Password = initialText ?? string.Empty;
                    break;
                case 3:
                    // MultiLine text area
                    InputTextBox.Visibility = Visibility.Visible;
                    InputPasswordBox.Visibility = Visibility.Collapsed;
                    InputTextBox.Text = initialText ?? string.Empty;
                    InputTextBox.AcceptsReturn = true;
                    InputTextBox.TextWrapping = TextWrapping.Wrap;
                    break;
                default:
                    // Default to single line
                    InputTextBox.Visibility = Visibility.Visible;
                    InputPasswordBox.Visibility = Visibility.Collapsed;
                    InputTextBox.Text = initialText ?? string.Empty;
                    InputTextBox.AcceptsReturn = false;
                    InputTextBox.TextWrapping = TextWrapping.NoWrap;
                    break;
            }
        }

        private void InputTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (_editType != 2)
            {
                InputTextBox.Focus(FocusState.Programmatic);
                InputTextBox.SelectAll();
            }
        }

        private void InputPasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (_editType == 2)
            {
                InputPasswordBox.Focus(FocusState.Programmatic);
            }
        }

        private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SubmitText();
        }

        private void InputPasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SubmitText();
        }

        private void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // For single line mode (editType 1), submit on Enter
            if (_editType == 1 && e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                SubmitText();
            }
            // For all modes, cancel on Escape
            else if (e.Key == VirtualKey.Escape)
            {
                e.Handled = true;
                CancelInput();
            }
        }

        private void InputPasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Password fields always submit on Enter
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                SubmitText();
            }
            // Cancel on Escape
            else if (e.Key == VirtualKey.Escape)
            {
                e.Handled = true;
                CancelInput();
            }
        }

        private void SubmitText()
        {
            if (_annot != null && _page != null)
            {
                try
                {
                    string textToSubmit = _editType == 2 ? InputPasswordBox.Password : InputTextBox.Text;

                    _page.ObjsStart();
                    _annot.EditText = textToSubmit;
                    _page.Close();

                    TextSubmitted?.Invoke(this, textToSubmit);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error submitting form text: {ex.Message}");
                }
            }

            Dismissed?.Invoke(this, EventArgs.Empty);
        }

        private void CancelInput()
        {
            // Don't save changes, just dismiss
            Dismissed?.Invoke(this, EventArgs.Empty);
        }
    }
}
