using Microsoft.Win32;
using System.Windows;
using System.Linq;

namespace Church_Presenter;

public partial class MainWindow : Window
{
    private readonly AppDatabase _database = new();
    private BibleVerse? _selectedVerse;
    private PlannerComponent? _selectedComponent;
    private Song? _selectedSong;
    private MediaAsset? _selectedMedia;
    private System.Windows.Controls.ComboBox? _planningModeBox;
    private int _plannerId;
    public MainWindow()
    {
        InitializeComponent();
        _database.Initialize();
        AddPlanningModeControl();
        var dp = GetControl<System.Windows.Controls.DatePicker>("ServiceDatePicker");
        if (dp != null) dp.SelectedDate = DateTime.Today;
        LoadSettings();
        LoadBooks();
        LoadLibraries();
        LoadPlanner();
        Closing += MainWindow_Closing;
    }
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        foreach (var window in Application.Current.Windows.OfType<Window>().Where(window => window != this).ToArray())
        {
            try { window.Close(); } catch { }
        }
    }
    private void AddPlanningModeControl()
    {
        var compTypeLabel = GetControl<System.Windows.Controls.TextBlock>("ComponentTypeLabel");
        var panel = compTypeLabel?.Parent as System.Windows.Controls.StackPanel; if (panel is null) return;
        var label = new System.Windows.Controls.TextBlock { Text = "Presentation mode", Margin = new Thickness(0, 12, 0, 0) };
        _planningModeBox = new System.Windows.Controls.ComboBox { Margin = new Thickness(4), Width = 150 };
        _planningModeBox.Items.Add("Static"); _planningModeBox.Items.Add("Scrollable"); _planningModeBox.SelectionChanged += (_, _) => { if (_selectedComponent is not null && _planningModeBox.SelectedItem is string mode) _database.SetPlannerPresentationMode(_selectedComponent.Id, mode); };
        panel.Children.Insert(1, label); panel.Children.Insert(2, _planningModeBox);
    }
    private void Show(UIElement panel) { foreach (var item in new UIElement[] { DashboardPanel, BiblePanel, PlannerPanel, SettingsPanel, SongPanel, MediaPanel, OtherPanel }) item.Visibility = Visibility.Collapsed; panel.Visibility = Visibility.Visible; }
    private void Dashboard_Click(object sender, RoutedEventArgs e) => Show(DashboardPanel); private void Planner_Click(object sender, RoutedEventArgs e) { Show(PlannerPanel); LoadPlanner(); } private void Bible_Click(object sender, RoutedEventArgs e) => Show(BiblePanel); private void Settings_Click(object sender, RoutedEventArgs e) => Show(SettingsPanel);
    private void SongLibrary_Click(object sender, RoutedEventArgs e) { LoadLibraries(); Show(SongPanel); }
    private void MediaLibrary_Click(object sender, RoutedEventArgs e) { LoadLibraries(); Show(MediaPanel); }
    private void Theme_Click(object sender, RoutedEventArgs e) { OtherTitle.Text = "Theme Manager"; OtherSubtitle.Text = "Theme editing will be added next."; Show(OtherPanel); }
    private void LoadBooks() { BookBox.ItemsSource = _database.GetBooks(); if (BookBox.Items.Count > 0) BookBox.SelectedIndex = 0; }
    private void Book_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (BookBox.SelectedItem is BibleBook book) { ChapterBox.ItemsSource = _database.GetChapters(book.Id); if (ChapterBox.Items.Count > 0) ChapterBox.SelectedIndex = 0; } }
    private void Chapter_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (BookBox.SelectedItem is BibleBook book && ChapterBox.SelectedItem is int chapter) VerseList.ItemsSource = _database.GetVerses(book.Id, chapter); }
    private void Search_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) { if (SearchBox.Text.Length >= 2) VerseList.ItemsSource = _database.Search(SearchBox.Text); else if (BookBox.SelectedItem is BibleBook book && ChapterBox.SelectedItem is int chapter) VerseList.ItemsSource = _database.GetVerses(book.Id, chapter); }
    private void Verse_Selected(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => _selectedVerse = VerseList.SelectedItem as BibleVerse;
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
    private void PlannerIdentity_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => LoadPlanner();
    private void LoadPlanner_Click(object sender, RoutedEventArgs e) => LoadPlanner();
    private void LoadPlanner()
    {
        var dp = GetControl<System.Windows.Controls.DatePicker>("ServiceDatePicker");
        var sn = GetControl<System.Windows.Controls.TextBox>("ServiceNameBox");
        var plannerList = GetControl<System.Windows.Controls.ListBox>("PlannerList");
        if (dp is null || sn is null || plannerList is null) return;
        var name = string.IsNullOrWhiteSpace(sn.Text) ? "Sunday Service" : sn.Text.Trim();
        _plannerId = _database.GetOrCreatePlanner(dp.SelectedDate ?? DateTime.Today, name);
        plannerList.ItemsSource = _database.GetPlannerComponents(_plannerId);
        ClearComponentEditor();
    }
    private void AddComponent_Click(object sender, RoutedEventArgs e)
    {
        if (_plannerId == 0) LoadPlanner();
        var type = (sender as System.Windows.Controls.Button)?.Tag?.ToString() ?? "Paragraph";
        if (type == "Song")
        {
            var picker = new SongPickerWindow(_database) { Owner = this };
            if (picker.ShowDialog() != true || picker.SelectedSong is null) return;
            _database.AddPlannerComponent(_plannerId, "Song", picker.SelectedSong.Title, picker.SelectedSong.Lyrics);
            RefreshPlanner();
            return;
        }
        if (type == "Bible Reading")
        {
            var picker = new BibleReadingPickerWindow(_database) { Owner = this };
            if (picker.ShowDialog() != true || picker.ReadingTitle is null || picker.ReadingText is null) return;
            _database.AddPlannerComponent(_plannerId, "Bible Reading", picker.ReadingTitle, picker.ReadingText);
            RefreshPlanner();
            return;
        }
        var (title, content) = type switch
        {
            "Heading" => ("New heading", ""), "Paragraph" => ("Paragraph", "Enter text here."),
            "Song" => ("New song", "Paste lyrics here."), "Bible Reading" => ("Bible reading", "Enter a Bible reference, e.g. John 3:16."),
            "Image" => ("Image", "Choose or enter an image file path."), "Image + Text" => ("Image and text", "Image path and caption."),
            "Video" => ("Video", "Choose or enter a video file path."), "Music" => ("Music", "Choose or enter a music file path."),
            "Announcement" => ("Announcement", "Enter announcement text."), "Blank Screen" => ("Blank screen", ""),
            "Countdown" => ("Countdown", "05:00"), _ => (type, "")
        };
        if ((type == "Image" || type == "Video" || type == "Music") && _selectedMedia is not null && _selectedMedia.Type == type) (title, content) = (_selectedMedia.Title, _selectedMedia.FilePath);
        _database.AddPlannerComponent(_plannerId, type, title, content); RefreshPlanner(true);
    }
    private void PlannerComponent_Selected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var plannerList = GetControl<System.Windows.Controls.ListBox>("PlannerList");
        _selectedComponent = plannerList?.SelectedItem as PlannerComponent; if (_selectedComponent is null) { ClearComponentEditor(); return; }
        var compTypeLabel = GetControl<System.Windows.Controls.TextBlock>("ComponentTypeLabel");
        var compTitle = GetControl<System.Windows.Controls.TextBox>("ComponentTitleBox");
        var compContent = GetControl<System.Windows.Controls.TextBox>("ComponentContentBox");
        if (compTypeLabel != null) compTypeLabel.Text = _selectedComponent.Type == "Song" ? "Song — edits apply only to this planner" : _selectedComponent.Type;
        if (compTitle != null) compTitle.Text = _selectedComponent.Title;
        if (compContent != null) compContent.Text = _selectedComponent.Content;
        if (_planningModeBox is not null) _planningModeBox.SelectedItem = _selectedComponent.PresentationMode;
    }
    private void SaveComponent_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedComponent is null) return;
        var compTitleBox = GetControl<System.Windows.Controls.TextBox>("ComponentTitleBox");
        var compContentBox = GetControl<System.Windows.Controls.TextBox>("ComponentContentBox");
        if (_selectedComponent is not null && compTitleBox is not null && compContentBox is not null)
        {
            _database.UpdatePlannerComponent(_selectedComponent.Id, compTitleBox.Text.Trim(), compContentBox.Text);
            RefreshPlanner(true);
        }
    }
    private void MoveUp_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);
    private void MoveDown_Click(object sender, RoutedEventArgs e) => MoveSelected(1);
    private void MoveSelected(int direction) { if (_selectedComponent is null) return; _database.MovePlannerComponent(_plannerId, _selectedComponent.Id, direction); RefreshPlanner(true); }
    private void RemoveComponent_Click(object sender, RoutedEventArgs e) { if (_selectedComponent is null) return; if (MessageBox.Show($"Remove '{_selectedComponent.Title}'?", "Church Presenter", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) { _database.DeletePlannerComponent(_selectedComponent.Id); RefreshPlanner(); } }
    private void RefreshPlanner(bool reselect = false)
    {
        var selectedId = reselect ? _selectedComponent?.Id : null;
        var components = _database.GetPlannerComponents(_plannerId);
        var plannerList = GetControl<System.Windows.Controls.ListBox>("PlannerList");
        if (plannerList is null) return;
        plannerList.ItemsSource = components;
        if (selectedId is int id) plannerList.SelectedItem = components.FirstOrDefault(x => x.Id == id); else ClearComponentEditor();
    }
    private void ClearComponentEditor()
    {
        _selectedComponent = null;
        var compType = GetControl<System.Windows.Controls.TextBlock>("ComponentTypeLabel");
        if (compType is null) return;
        compType.Text = "Select a component";
        var compTitle = GetControl<System.Windows.Controls.TextBox>("ComponentTitleBox");
        var compContent = GetControl<System.Windows.Controls.TextBox>("ComponentContentBox");
        if (compTitle != null) compTitle.Text = "";
        if (compContent != null) compContent.Text = "";
        if (_planningModeBox is not null) _planningModeBox.SelectedIndex = -1;
    }

    private T? GetControl<T>(string name) where T : class => FindName(name) as T;
    private void PresentComponent_Click(object sender, RoutedEventArgs e)
    {
        if (_plannerId == 0) LoadPlanner();
        var components = _database.GetPlannerComponents(_plannerId);
        if (components.Count == 0) { MessageBox.Show("Add a planner component before starting presentation.", "Church Presenter"); return; }
        new OperatorConsoleWindow(_database, components) { Owner = this }.Show();
    }
    private void LoadLibraries() { SongList.ItemsSource = _database.GetSongs(); }
    private void SaveSong_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SongTitleBox.Text) || string.IsNullOrWhiteSpace(SongLyricsBox.Text)) { MessageBox.Show("Enter both a song title and lyrics.", "Church Presenter"); return; }
        _database.SaveSong(SongTitleBox.Text.Trim(), SongLyricsBox.Text); LoadLibraries(); SongTitleBox.Clear(); SongLyricsBox.Clear();
    }
    private void DeleteAllSongs_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Delete every song in the song library? Existing planner items will be kept. This cannot be undone.", "Delete songs", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _database.DeleteAllSongs();
        _selectedSong = null;
        SongTitleBox.Clear();
        SongLyricsBox.Clear();
        LoadLibraries();
    }
    private void Song_Selected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _selectedSong = SongList.SelectedItem as Song; if (_selectedSong is null) return; SongTitleBox.Text = _selectedSong.Title; SongLyricsBox.Text = _selectedSong.Lyrics;
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
