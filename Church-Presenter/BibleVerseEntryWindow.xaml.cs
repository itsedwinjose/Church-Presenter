using System.Windows;
using System.Windows.Controls;

namespace Church_Presenter;

public partial class BibleVerseEntryWindow : Window
{
    private readonly AppDatabase _database;
    private readonly BibleBook? _book;
    private readonly BibleVerse? _verse;

    public BibleVerseEntryWindow(AppDatabase database, BibleBook? book = null, int? chapter = null, BibleVerse? verse = null)
    {
        InitializeComponent();
        _database = database;
        _book = book;
        _verse = verse;

        if (verse is not null && book is not null)
        {
            HeadingText.Text = "Edit Bible verse";
            SubtitleText.Text = "Update the selected verse number, chapter, or text.";
            SaveButton.Content = "Save changes";
            TestamentBox.SelectedIndex = string.Equals(book.Testament, "New", System.StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            BookBox.Text = book.Name;
            ChapterBox.Text = verse.Chapter.ToString();
            VerseBox.Text = verse.Verse.ToString();
            VerseTextBox.Text = verse.Text;
            TestamentBox.IsEnabled = false;
            BookBox.IsReadOnly = true;
            return;
        }

        if (book is not null)
        {
            HeadingText.Text = "Add Bible verse";
            SubtitleText.Text = "Add a verse to the selected Bible book.";
            SaveButton.Content = "Add verse";
            TestamentBox.SelectedIndex = string.Equals(book.Testament, "New", System.StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            BookBox.Text = book.Name;
            TestamentBox.IsEnabled = false;
            BookBox.IsReadOnly = true;
            if (chapter is int chapterNumber && chapterNumber > 0) ChapterBox.Text = chapterNumber.ToString();
            return;
        }

        HeadingText.Text = "Add Bible data";
        SubtitleText.Text = "Create a verse entry or a new book if it does not exist yet.";
        SaveButton.Content = "Save verse";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (TestamentBox.SelectedItem is not ComboBoxItem item || string.IsNullOrWhiteSpace(BookBox.Text) || !int.TryParse(ChapterBox.Text, out var chapter) || chapter < 1 || !int.TryParse(VerseBox.Text, out var verseNumber) || verseNumber < 1 || string.IsNullOrWhiteSpace(VerseTextBox.Text)) { MessageBox.Show("Select a testament and enter a book, valid chapter, verse, and text.", "Church Presenter"); return; }
        try
        {
            if (_verse is not null)
            {
                _database.UpdateBibleVerse(_verse.Id, chapter, verseNumber, VerseTextBox.Text.Trim());
            }
            else if (_book is not null)
            {
                _database.AddBibleVerse(_book.Id, chapter, verseNumber, VerseTextBox.Text.Trim());
            }
            else
            {
                _database.SaveBibleVerse(item.Content?.ToString() ?? "Old", BookBox.Text, chapter, verseNumber, VerseTextBox.Text.Trim());
            }

            DialogResult = true;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Church Presenter");
        }
    }
}
