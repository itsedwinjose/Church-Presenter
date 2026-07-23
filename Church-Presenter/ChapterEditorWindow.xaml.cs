using System.Windows;

namespace Church_Presenter;

public partial class ChapterEditorWindow : Window
{
    private readonly bool _includeFirstVerse;

    public int ChapterNumber { get; private set; }
    public int FirstVerseNumber { get; private set; } = 1;
    public string FirstVerseText { get; private set; } = string.Empty;

    public ChapterEditorWindow(string heading, string actionText, int chapterNumber = 1, bool includeFirstVerse = false, int firstVerseNumber = 1, string firstVerseText = "")
    {
        InitializeComponent();
        _includeFirstVerse = includeFirstVerse;
        HeadingText.Text = heading;
        SubtitleText.Text = includeFirstVerse
            ? "Adding a chapter creates its first verse because empty chapters are not stored separately."
            : "Update the selected chapter number.";
        SaveButton.Content = actionText;
        ChapterNumberBox.Text = chapterNumber.ToString();
        VerseNumberBox.Text = firstVerseNumber.ToString();
        VerseTextBox.Text = firstVerseText;
        if (!_includeFirstVerse)
        {
            VerseNumberPanel.Visibility = Visibility.Collapsed;
            VerseTextLabel.Visibility = Visibility.Collapsed;
            VerseTextBox.Visibility = Visibility.Collapsed;
            Height = 260;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ChapterNumberBox.Text, out var chapterNumber) || chapterNumber < 1)
        {
            MessageBox.Show("Enter a valid chapter number.", "Church Presenter");
            return;
        }

        ChapterNumber = chapterNumber;
        if (_includeFirstVerse)
        {
            if (!int.TryParse(VerseNumberBox.Text, out var firstVerseNumber) || firstVerseNumber < 1 || string.IsNullOrWhiteSpace(VerseTextBox.Text))
            {
                MessageBox.Show("Enter a valid first verse number and verse text.", "Church Presenter");
                return;
            }

            FirstVerseNumber = firstVerseNumber;
            FirstVerseText = VerseTextBox.Text.Trim();
        }

        DialogResult = true;
    }
}
