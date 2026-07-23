using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Church_Presenter;

public partial class ChapterEditorWindow : Window
{
    public BibleBook? SelectedBook { get; private set; }

    public int ChapterNumber { get; private set; }

    public ChapterEditorWindow(string heading, string actionText, IReadOnlyList<BibleBook> books, int? selectedBookId = null, int chapterNumber = 1)
    {
        InitializeComponent();
        HeadingText.Text = heading;
        SubtitleText.Text = "Select a Bible book, enter the chapter number, and save.";
        SaveButton.Content = actionText;
        BookBox.ItemsSource = books;
        if (selectedBookId is int bookId)
            BookBox.SelectedItem = books.FirstOrDefault(book => book.Id == bookId);
        if (BookBox.SelectedItem is null && books.Count > 0)
            BookBox.SelectedIndex = 0;
        ChapterNumberBox.Text = chapterNumber.ToString();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (BookBox.SelectedItem is not BibleBook book)
        {
            MessageBox.Show("Select a Bible book.", "Church Presenter");
            return;
        }

        if (!int.TryParse(ChapterNumberBox.Text, out var chapterNumber) || chapterNumber < 1)
        {
            MessageBox.Show("Enter a valid chapter number.", "Church Presenter");
            return;
        }

        SelectedBook = book;
        ChapterNumber = chapterNumber;
        DialogResult = true;
    }
}
