using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace Church_Presenter;

public partial class MainWindow : Window
{
    private readonly AppDatabase _database = new();
    private readonly Dictionary<int, HashSet<int>> _pendingBibleChapters = [];
    private IReadOnlyList<Song> _songs = Array.Empty<Song>();
    private BibleVerse? _selectedVerse;
    private Song? _selectedSong;
    private MediaAsset? _selectedMedia;
    private bool _updatingSongEditor;
    public MainWindow()
    {
        InitializeComponent();
        _database.Initialize();
        LoadSettings();
        LoadBooks();
        LoadLibraries();
        Show(SongPanel, SongLibraryNavButton);
        Closing += MainWindow_Closing;
    }
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        foreach (var window in Application.Current.Windows.OfType<Window>().Where(window => window != this).ToArray())
        {
            try { window.Close(); } catch { }
        }
    }
    private void Show(UIElement panel, Button activeButton)
    {
        foreach (var item in new UIElement[] { DashboardPanel, BiblePanel, PlannerPanel, SettingsPanel, SongPanel, MediaPanel, OtherPanel })
            item.Visibility = Visibility.Collapsed;

        panel.Visibility = Visibility.Visible;
        SetActiveMenu(activeButton);
    }

    private void SetActiveMenu(Button activeButton)
    {
        foreach (var button in new[] { DashboardNavButton, PlannerNavButton, SongLibraryNavButton, BibleNavButton, MediaLibraryNavButton, ThemesNavButton, SettingsNavButton })
        {
            var isActive = ReferenceEquals(button, activeButton);
            button.Background = isActive ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#463493")) : System.Windows.Media.Brushes.Transparent;
            button.Foreground = isActive ? System.Windows.Media.Brushes.White : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CBD5E1"));
        }
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e) => Show(DashboardPanel, DashboardNavButton);
    private void Planner_Click(object sender, RoutedEventArgs e) => Show(PlannerPanel, PlannerNavButton);
    private void Bible_Click(object sender, RoutedEventArgs e) => Show(BiblePanel, BibleNavButton);
    private void Settings_Click(object sender, RoutedEventArgs e) => Show(SettingsPanel, SettingsNavButton);
    private void SongLibrary_Click(object sender, RoutedEventArgs e) { LoadLibraries(); Show(SongPanel, SongLibraryNavButton); }
    private void MediaLibrary_Click(object sender, RoutedEventArgs e) { LoadLibraries(); Show(MediaPanel, MediaLibraryNavButton); }
    private void Theme_Click(object sender, RoutedEventArgs e) { OtherTitle.Text = "Theme Manager"; OtherSubtitle.Text = "Theme editing will be added next."; Show(OtherPanel, ThemesNavButton); }
    private void LoadBooks(int? selectedBookId = null)
    {
        var books = _database.GetBooks();
        BookBox.ItemsSource = books;

        if (books.Count == 0)
        {
            ChapterBox.ItemsSource = null;
            VerseList.ItemsSource = null;
            return;
        }

        if (selectedBookId is int bookId)
        {
            var selectedBook = books.FirstOrDefault(book => book.Id == bookId);
            if (selectedBook is not null)
            {
                BookBox.SelectedItem = selectedBook;
                return;
            }
        }

        BookBox.SelectedIndex = 0;
    }

    private IReadOnlyList<int> GetVisibleChapters(int bookId)
    {
        var chapters = _database.GetChapters(bookId).ToHashSet();
        if (_pendingBibleChapters.TryGetValue(bookId, out var pending))
            chapters.UnionWith(pending);
        return chapters.OrderBy(chapter => chapter).ToList();
    }

    private void AddPendingChapter(int bookId, int chapter)
    {
        if (!_pendingBibleChapters.TryGetValue(bookId, out var pending))
        {
            pending = [];
            _pendingBibleChapters[bookId] = pending;
        }

        pending.Add(chapter);
    }

    private void RemovePendingChapter(int bookId, int chapter)
    {
        if (!_pendingBibleChapters.TryGetValue(bookId, out var pending))
            return;

        pending.Remove(chapter);
        if (pending.Count == 0)
            _pendingBibleChapters.Remove(bookId);
    }

    private void RefreshChapters(BibleBook book, int? selectedChapter = null)
    {
        var chapters = GetVisibleChapters(book.Id);
        ChapterBox.ItemsSource = chapters;

        if (chapters.Count == 0)
        {
            VerseList.ItemsSource = null;
            return;
        }

        if (selectedChapter is int chapter && chapters.Contains(chapter))
        {
            ChapterBox.SelectedItem = chapter;
            return;
        }

        ChapterBox.SelectedIndex = 0;
    }

    private void Book_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (BookBox.SelectedItem is BibleBook book) RefreshChapters(book); }
    private void Chapter_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (BookBox.SelectedItem is BibleBook book && ChapterBox.SelectedItem is int chapter) VerseList.ItemsSource = _database.GetVerses(book.Id, chapter); else VerseList.ItemsSource = null; }
    private void Search_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) { if (SearchBox.Text.Length >= 2) VerseList.ItemsSource = _database.Search(SearchBox.Text); else if (BookBox.SelectedItem is BibleBook book && ChapterBox.SelectedItem is int chapter) VerseList.ItemsSource = _database.GetVerses(book.Id, chapter); }
    private void Verse_Selected(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => _selectedVerse = VerseList.SelectedItem as BibleVerse;
    private void AddBibleBook_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new BookEditorWindow("Add Bible book", "Add book") { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _database.AddBibleBook(dialog.Testament, dialog.BookName);
            var addedBook = _database.GetBooks().FirstOrDefault(book => string.Equals(book.Name, dialog.BookName, StringComparison.CurrentCultureIgnoreCase));
            LoadBooks(addedBook?.Id);
            MessageBox.Show($"'{dialog.BookName}' saved to the Bible library.", "Church Presenter");
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Church Presenter");
        }
    }

    private void AddBibleChapter_Click(object sender, RoutedEventArgs e)
    {
        var books = _database.GetBooks();
        if (books.Count == 0)
        {
            MessageBox.Show("Add a Bible book first.", "Church Presenter");
            return;
        }

        var selectedBook = BookBox.SelectedItem as BibleBook;
        var selectedBookId = selectedBook?.Id ?? books[0].Id;
        var nextChapter = GetVisibleChapters(selectedBookId).DefaultIfEmpty(0).Max() + 1;
        var dialog = new ChapterEditorWindow("Add Bible chapter", "Save chapter", books, selectedBookId, nextChapter) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            if (dialog.SelectedBook is null)
                return;

            if (GetVisibleChapters(dialog.SelectedBook.Id).Contains(dialog.ChapterNumber))
            {
                MessageBox.Show($"Chapter {dialog.ChapterNumber} already exists for {dialog.SelectedBook.Name}.", "Church Presenter");
                return;
            }

            AddPendingChapter(dialog.SelectedBook.Id, dialog.ChapterNumber);
            LoadBooks(dialog.SelectedBook.Id);
            RefreshChapters(dialog.SelectedBook, dialog.ChapterNumber);
            MessageBox.Show($"Chapter {dialog.ChapterNumber} added for {dialog.SelectedBook.Name}. Add verses later to save its content.", "Church Presenter");
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Church Presenter");
        }
    }

    private void AddBibleVerse_Click(object sender, RoutedEventArgs e)
    {
        if (BookBox.SelectedItem is not BibleBook book)
        {
            MessageBox.Show("Select a Bible book first.", "Church Presenter");
            return;
        }

        var selectedChapter = ChapterBox.SelectedItem as int?;
        var dialog = new BibleVerseEntryWindow(_database, book, selectedChapter) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (selectedChapter is int chapterNumber)
            RemovePendingChapter(book.Id, chapterNumber);

        LoadBooks(book.Id);
        if (selectedChapter is int chapter)
            RefreshChapters(book, chapter);

        MessageBox.Show("Bible verse saved.", "Church Presenter");
    }

    private void ImportBible_Click(object sender, RoutedEventArgs e)
    {
        var choice = MessageBox.Show("Choose Yes to import a Bible CSV file. Choose No to add one Bible verse manually, including its Old or New Testament selection.", "Add Bible data", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (choice == MessageBoxResult.No) { if (new BibleVerseEntryWindow(_database) { Owner = this }.ShowDialog() == true) { LoadBooks(); MessageBox.Show("Bible verse saved.", "Church Presenter"); } return; }
        if (choice != MessageBoxResult.Yes) return;
        var picker = new OpenFileDialog { Filter = "Bible CSV|*.csv", Title = "Import UTF-8 Bible CSV (Book,Testament,Chapter,Verse,Text)" }; if (picker.ShowDialog() == true) { _database.ImportBibleCsv(picker.FileName); LoadBooks(); MessageBox.Show("Bible import completed.", "Church Presenter"); }
    }
    private void DeleteAllBible_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Delete every imported Bible book and verse? This cannot be undone.", "Delete Bible data", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _database.DeleteAllBibleData();
        _pendingBibleChapters.Clear();
        _selectedVerse = null;
        BookBox.ItemsSource = null;
        ChapterBox.ItemsSource = null;
        VerseList.ItemsSource = null;
    }
    private void LoadSettings()
    {
        BackgroundColorBox.Text = _database.Get("BackgroundColor", "#FFFFFF");
        FontColorBox.Text = _database.Get("FontColor", "#000000");
        FontSizeSlider.Value = double.Parse(_database.Get("FontSize", "48"));
        FontSizeLabel.Text = $"{FontSizeSlider.Value:0} px";
        FontSizeSlider.ValueChanged += (_, _) => FontSizeLabel.Text = $"{FontSizeSlider.Value:0} px";
        HeadingFontColorBox.Text = _database.Get("HeadingFontColor", "#000000");
        HeadingFontSizeBox.Text = _database.Get("HeadingFontSize", "48");
        ParagraphFontColorBox.Text = _database.Get("ParagraphFontColor", "#000000");
        ParagraphFontSizeBox.Text = _database.Get("ParagraphFontSize", "48");
        BibleReadingFontColorBox.Text = _database.Get("BibleReadingFontColor", "#000000");
        BibleReadingFontSizeBox.Text = _database.Get("BibleReadingFontSize", "48");
        SongLyricsFontColorBox.Text = _database.Get("SongLyricsFontColor", "#000000");
        SongLyricsFontSizeBox.Text = _database.Get("SongLyricsFontSize", "48");

        // Scroll speed
        var speed = int.Parse(_database.Get("ScrollSpeed", "2"));
        ScrollSpeedSlider.Value = speed;
        ScrollSpeedLabel.Text = $"{ScrollSpeedSlider.Value:0} px / tick";
        ScrollSpeedSlider.ValueChanged += (_, _) => ScrollSpeedLabel.Text = $"{ScrollSpeedSlider.Value:0} px / tick";
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _database.Set("BackgroundColor", BackgroundColorBox.Text);
        _database.Set("FontColor", FontColorBox.Text);
        _database.Set("FontSize", FontSizeSlider.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _database.Set("HeadingFontColor", HeadingFontColorBox.Text);
        _database.Set("HeadingFontSize", HeadingFontSizeBox.Text);
        _database.Set("ParagraphFontColor", ParagraphFontColorBox.Text);
        _database.Set("ParagraphFontSize", ParagraphFontSizeBox.Text);
        _database.Set("BibleReadingFontColor", BibleReadingFontColorBox.Text);
        _database.Set("BibleReadingFontSize", BibleReadingFontSizeBox.Text);
        _database.Set("SongLyricsFontColor", SongLyricsFontColorBox.Text);
        _database.Set("SongLyricsFontSize", SongLyricsFontSizeBox.Text);
        _database.Set("ScrollSpeed", ((int)ScrollSpeedSlider.Value).ToString(System.Globalization.CultureInfo.InvariantCulture));
        MessageBox.Show("Display settings saved.", "Church Presenter");
    }
    private void PresentVerse_Click(object sender, RoutedEventArgs e) { if (_selectedVerse is null) { MessageBox.Show("Choose a verse first.", "Church Presenter"); return; } Present(_selectedVerse.Reference, _selectedVerse.Text, "Bible Reading"); }
    private void LoadLibraries(int? songIdToSelect = null, string? songTitleToSelect = null)
    {
        _songs = _database.GetSongs();
        ApplySongFilter(songIdToSelect ?? _selectedSong?.Id, songTitleToSelect);
    }

    private void SongSearch_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) => ApplySongFilter(_selectedSong?.Id, _selectedSong?.Title);

    private void ApplySongFilter(int? songIdToSelect = null, string? songTitleToSelect = null)
    {
        var query = SongSearchBox?.Text?.Trim() ?? string.Empty;
        var filteredSongs = string.IsNullOrWhiteSpace(query)
            ? _songs.ToList()
            : _songs.Where(song => song.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) || song.Lyrics.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();

        SongList.ItemsSource = filteredSongs;
        SongCountText.Text = $"Songs    {_songs.Count}";
        SongSearchStatusText.Text = string.IsNullOrWhiteSpace(query)
            ? $"{filteredSongs.Count} songs available"
            : $"{filteredSongs.Count} of {_songs.Count} songs match \"{query}\"";

        var songToSelect = songIdToSelect is int id
            ? filteredSongs.FirstOrDefault(song => song.Id == id)
            : null;

        if (songToSelect is null && !string.IsNullOrWhiteSpace(songTitleToSelect))
            songToSelect = filteredSongs.FirstOrDefault(song => string.Equals(song.Title, songTitleToSelect, StringComparison.CurrentCultureIgnoreCase));

        if (songToSelect is not null)
        {
            SongList.SelectedItem = songToSelect;
            return;
        }

        if (_selectedSong is not null && filteredSongs.All(song => song.Id != _selectedSong.Id))
            SongList.SelectedItem = null;

        if (_selectedSong is null)
            UpdateSongEditorState();
    }

    private void NewSong_Click(object sender, RoutedEventArgs e)
    {
        SongList.SelectedItem = null;
        SetSelectedSong(null);
        SongTitleBox.Focus();
    }

    private void SongEditor_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_updatingSongEditor) return;
        UpdateSongEditorState();
    }

    private void SaveSong_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SongTitleBox.Text) || string.IsNullOrWhiteSpace(SongLyricsBox.Text)) { MessageBox.Show("Enter both a song title and lyrics.", "Church Presenter"); return; }

        var title = SongTitleBox.Text.Trim();
        var lyrics = SongLyricsBox.Text.TrimEnd();

        try
        {
            if (_selectedSong is null)
            {
                _database.AddSong(title, lyrics);
                LoadLibraries(songTitleToSelect: title);
            }
            else
            {
                _database.UpdateSong(_selectedSong.Id, title, lyrics);
                LoadLibraries(songIdToSelect: _selectedSong.Id);
            }
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            MessageBox.Show("A song with this title already exists. Choose a different title or edit the existing song.", "Church Presenter");
        }
    }
    private void DeleteSong_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSong is null)
        {
            MessageBox.Show("Select a song to delete.", "Church Presenter");
            return;
        }

        if (MessageBox.Show($"Delete '{_selectedSong.Title}' from the song library? Existing planner items will be kept.", "Delete song", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        _database.DeleteSong(_selectedSong.Id);
        SongList.SelectedItem = null;
        SetSelectedSong(null);
        LoadLibraries();
    }

    private void DeleteAllSongs_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Delete every song in the song library? Existing planner items will be kept. This cannot be undone.", "Delete songs", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _database.DeleteAllSongs();
        SongSearchBox.Clear();
        SongList.SelectedItem = null;
        SetSelectedSong(null);
        LoadLibraries();
    }
    private void Song_Selected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        SetSelectedSong(SongList.SelectedItem as Song);
    }

    private void SetSelectedSong(Song? song)
    {
        _selectedSong = song;
        _updatingSongEditor = true;
        SongIdBox.Text = song?.Id.ToString("000") ?? "New";
        SongTitleBox.Text = song?.Title ?? string.Empty;
        SongLyricsBox.Text = song?.Lyrics ?? string.Empty;
        _updatingSongEditor = false;
        UpdateSongEditorState();
    }

    private void UpdateSongEditorState()
    {
        var title = SongTitleBox?.Text?.Trim() ?? string.Empty;
        var lyrics = SongLyricsBox?.Text ?? string.Empty;

        SongStatusText.Text = _selectedSong is null
            ? "No song selected"
            : $"{_selectedSong.Title} (ID {_selectedSong.Id:000})";
        SongEditorModeText.Text = _selectedSong is null ? "Adding a new song" : "Editing selected song";
        SongPreviewTitleText.Text = string.IsNullOrWhiteSpace(title) ? "Song preview" : title;
        SongPreviewLyricsText.Text = string.IsNullOrWhiteSpace(lyrics)
            ? "Select a song or start a new one to see its lyrics here."
            : lyrics;
        SaveSongButton.Content = _selectedSong is null ? "▣  Save Song" : "▣  Update Song";
        DeleteSongButton.IsEnabled = _selectedSong is not null;
    }
    private void SaveMedia_Click(object sender, RoutedEventArgs e)
    {
        // Media handling is now managed by MediaLibraryView
    }
    private void Media_Selected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Media selection is now managed by MediaLibraryView
    }
    private void Preview_Click(object sender, RoutedEventArgs e) => Present("Church Presenter", "Your presentation preview appears here.", "Paragraph");
    private void Present(string title, string content, string componentType)
    {
        try
        {
            var speed = int.Parse(_database.Get("ScrollSpeed", "2"));
            var (foreground, fontSize) = _database.GetPresentationStyle(componentType);
            new PresentationWindow(title, content, BackgroundColorBox.Text, foreground, fontSize, false, speed).Show();
        }
        catch
        {
            MessageBox.Show("Use valid hex colors, for example #FFFFFF and #000000.", "Church Presenter");
        }
    }
}
