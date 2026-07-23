using System.Windows;
using System.Windows.Controls;

namespace Church_Presenter;

public partial class BookEditorWindow : Window
{
    public string Testament { get; private set; } = "Old";
    public string BookName { get; private set; } = string.Empty;

    public BookEditorWindow(string heading, string actionText, string testament = "Old", string bookName = "")
    {
        InitializeComponent();
        HeadingText.Text = heading;
        SubtitleText.Text = actionText == "Add book" ? "Create a Bible book entry before adding chapters and verses." : "Update the selected Bible book.";
        SaveButton.Content = actionText;
        BookNameBox.Text = bookName;
        TestamentBox.SelectedIndex = string.Equals(testament, "New", System.StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (TestamentBox.SelectedItem is not ComboBoxItem item || string.IsNullOrWhiteSpace(BookNameBox.Text))
        {
            MessageBox.Show("Select a testament and enter a book name.", "Church Presenter");
            return;
        }

        Testament = item.Content?.ToString() ?? "Old";
        BookName = BookNameBox.Text.Trim();
        DialogResult = true;
    }
}
