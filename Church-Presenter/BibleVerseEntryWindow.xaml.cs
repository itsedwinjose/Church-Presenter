using System.Windows;

namespace Church_Presenter;

public partial class BibleVerseEntryWindow : Window
{
    private readonly AppDatabase _database;
    public BibleVerseEntryWindow(AppDatabase database) { InitializeComponent(); _database = database; }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (TestamentBox.SelectedItem is not System.Windows.Controls.ComboBoxItem item || string.IsNullOrWhiteSpace(BookBox.Text) || !int.TryParse(ChapterBox.Text, out var chapter) || chapter < 1 || !int.TryParse(VerseBox.Text, out var verse) || verse < 1 || string.IsNullOrWhiteSpace(VerseTextBox.Text)) { MessageBox.Show("Select a testament and enter a book, valid chapter, verse, and text.", "Church Presenter"); return; }
        _database.SaveBibleVerse(item.Content?.ToString() ?? "Old", BookBox.Text, chapter, verse, VerseTextBox.Text.Trim()); DialogResult = true;
    }
}
