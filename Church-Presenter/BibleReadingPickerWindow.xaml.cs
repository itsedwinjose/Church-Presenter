using System.Windows;
using System.Windows.Controls;

namespace Church_Presenter;

public partial class BibleReadingPickerWindow : Window
{
    private readonly AppDatabase _database;
    private IReadOnlyList<BibleVerse> _verses = [];
    public string? ReadingTitle { get; private set; }
    public string? ReadingText { get; private set; }
    public BibleReadingPickerWindow(AppDatabase database)
    {
        InitializeComponent(); _database = database; TestamentBox.SelectedIndex = 0;
    }
    private void Testament_Changed(object sender, SelectionChangedEventArgs e)
    {
        var testament = (TestamentBox.SelectedItem as ComboBoxItem)?.Content?.ToString(); BookBox.ItemsSource = _database.GetBooks(testament); if (BookBox.Items.Count > 0) BookBox.SelectedIndex = 0;
    }
    private void Book_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (BookBox.SelectedItem is not BibleBook book) return; ChapterBox.ItemsSource = _database.GetChapters(book.Id); if (ChapterBox.Items.Count > 0) ChapterBox.SelectedIndex = 0;
    }
    private void Chapter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (BookBox.SelectedItem is not BibleBook book || ChapterBox.SelectedItem is not int chapter) return;
        _verses = _database.GetVerses(book.Id, chapter); var numbers = _verses.Select(v => v.Verse).ToList(); FromVerseBox.ItemsSource = numbers; ToVerseBox.ItemsSource = numbers;
        if (numbers.Count > 0) { FromVerseBox.SelectedIndex = 0; ToVerseBox.SelectedIndex = numbers.Count - 1; }
    }
    private void VerseRange_Changed(object sender, SelectionChangedEventArgs e) => UpdatePreview();
    private void UpdatePreview()
    {
        if (FromVerseBox.SelectedItem is not int from || ToVerseBox.SelectedItem is not int to) return; if (to < from) { ToVerseBox.SelectedItem = from; return; }
        var selected = _verses.Where(v => v.Verse >= from && v.Verse <= to).ToList(); PreviewText.Text = string.Join(Environment.NewLine + Environment.NewLine, selected.Select(v => $"{v.Verse}. {v.Text}"));
    }
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (BookBox.SelectedItem is not BibleBook book || ChapterBox.SelectedItem is not int chapter || FromVerseBox.SelectedItem is not int from || ToVerseBox.SelectedItem is not int to) { MessageBox.Show("Choose a complete Bible passage.", "Church Presenter"); return; }
        if (to < from) { MessageBox.Show("The ending verse must be after the starting verse.", "Church Presenter"); return; }
        var selected = _verses.Where(v => v.Verse >= from && v.Verse <= to).ToList(); if (selected.Count == 0) return;
        ReadingTitle = from == to ? $"{book.Name} {chapter}:{from}" : $"{book.Name} {chapter}:{from}-{to}";
        ReadingText = string.Join(Environment.NewLine + Environment.NewLine, selected.Select(v => $"{v.Verse}. {v.Text}")); DialogResult = true;
    }
}
