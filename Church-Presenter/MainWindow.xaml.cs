using Microsoft.Win32;
using System.Windows;

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
    public MainWindow() { InitializeComponent(); _database.Initialize(); AddPlanningModeControl(); ServiceDatePicker.SelectedDate = DateTime.Today; LoadSettings(); LoadBooks(); LoadPlanner(); }
    private void AddPlanningModeControl()
    {
        var panel = ComponentTypeLabel.Parent as System.Windows.Controls.StackPanel; if (panel is null) return;
        var label = new System.Windows.Controls.TextBlock { Text = "Presentation mode", Margin = new Thickness(0, 12, 0, 0) };
        _planningModeBox = new System.Windows.Controls.ComboBox { Margin = new Thickness(4), Width = 150 };
        _planningModeBox.Items.Add("Static"); _planningModeBox.Items.Add("Scrollable"); _planningModeBox.SelectionChanged += (_, _) => { if (_selectedComponent is not null && _planningModeBox.SelectedItem is string mode) _database.SetPlannerPresentationMode(_selectedComponent.Id, mode); };
        panel.Children.Insert(1, label); panel.Children.Insert(2, _planningModeBox);
    }
    private void Show(UIElement panel) { foreach (var item in new UIElement[] { DashboardPanel, BiblePanel, PlannerPanel, SettingsPanel, OtherPanel }) item.Visibility = Visibility.Collapsed; panel.Visibility = Visibility.Visible; }
    private void Dashboard_Click(object sender, RoutedEventArgs e) => Show(DashboardPanel); private void Planner_Click(object sender, RoutedEventArgs e) { Show(PlannerPanel); LoadPlanner(); } private void Bible_Click(object sender, RoutedEventArgs e) => Show(BiblePanel); private void Settings_Click(object sender, RoutedEventArgs e) => Show(SettingsPanel);
    private void Library_Click(object sender, RoutedEventArgs e) { OtherTitle.Text = "Song + Media Library"; OtherSubtitle.Text = "Add songs and media once, then reuse them in any planner."; SongLibraryCard.Visibility = MediaLibraryCard.Visibility = Visibility.Visible; LoadLibraries(); Show(OtherPanel); }
    private void Theme_Click(object sender, RoutedEventArgs e) { OtherTitle.Text = "Theme Manager"; OtherSubtitle.Text = "Theme editing will be added next."; SongLibraryCard.Visibility = MediaLibraryCard.Visibility = Visibility.Collapsed; Show(OtherPanel); }
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
    private void LoadSettings() { BackgroundColorBox.Text = _database.Get("BackgroundColor", "#101828"); FontColorBox.Text = _database.Get("FontColor", "#FFFFFF"); FontSizeSlider.Value = double.Parse(_database.Get("FontSize", "48")); FontSizeLabel.Text = $"{FontSizeSlider.Value:0} px"; FontSizeSlider.ValueChanged += (_, _) => FontSizeLabel.Text = $"{FontSizeSlider.Value:0} px"; }
    private void SaveSettings_Click(object sender, RoutedEventArgs e) { _database.Set("BackgroundColor", BackgroundColorBox.Text); _database.Set("FontColor", FontColorBox.Text); _database.Set("FontSize", FontSizeSlider.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)); MessageBox.Show("Display settings saved.", "Church Presenter"); }
    private void PresentVerse_Click(object sender, RoutedEventArgs e) { if (_selectedVerse is null) { MessageBox.Show("Choose a verse first.", "Church Presenter"); return; } Present(_selectedVerse.Reference, _selectedVerse.Text); }
    private void PlannerIdentity_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => LoadPlanner();
    private void LoadPlanner_Click(object sender, RoutedEventArgs e) => LoadPlanner();
    private void LoadPlanner()
    {
        if (ServiceDatePicker is null || ServiceNameBox is null) return;
        var name = string.IsNullOrWhiteSpace(ServiceNameBox.Text) ? "Sunday Service" : ServiceNameBox.Text.Trim();
        _plannerId = _database.GetOrCreatePlanner(ServiceDatePicker.SelectedDate ?? DateTime.Today, name);
        PlannerList.ItemsSource = _database.GetPlannerComponents(_plannerId); ClearComponentEditor();
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
        _selectedComponent = PlannerList.SelectedItem as PlannerComponent; if (_selectedComponent is null) { ClearComponentEditor(); return; }
        ComponentTypeLabel.Text = _selectedComponent.Type == "Song" ? "Song — edits apply only to this planner" : _selectedComponent.Type; ComponentTitleBox.Text = _selectedComponent.Title; ComponentContentBox.Text = _selectedComponent.Content; if (_planningModeBox is not null) _planningModeBox.SelectedItem = _selectedComponent.PresentationMode;
    }
    private void SaveComponent_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedComponent is null) return;
        _database.UpdatePlannerComponent(_selectedComponent.Id, ComponentTitleBox.Text.Trim(), ComponentContentBox.Text); RefreshPlanner(true);
    }
    private void MoveUp_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);
    private void MoveDown_Click(object sender, RoutedEventArgs e) => MoveSelected(1);
    private void MoveSelected(int direction) { if (_selectedComponent is null) return; _database.MovePlannerComponent(_plannerId, _selectedComponent.Id, direction); RefreshPlanner(true); }
    private void RemoveComponent_Click(object sender, RoutedEventArgs e) { if (_selectedComponent is null) return; if (MessageBox.Show($"Remove '{_selectedComponent.Title}'?", "Church Presenter", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) { _database.DeletePlannerComponent(_selectedComponent.Id); RefreshPlanner(); } }
    private void RefreshPlanner(bool reselect = false) { var selectedId = reselect ? _selectedComponent?.Id : null; var components = _database.GetPlannerComponents(_plannerId); PlannerList.ItemsSource = components; if (selectedId is int id) PlannerList.SelectedItem = components.FirstOrDefault(x => x.Id == id); else ClearComponentEditor(); }
    private void ClearComponentEditor() { _selectedComponent = null; if (ComponentTypeLabel is null) return; ComponentTypeLabel.Text = "Select a component"; ComponentTitleBox.Text = ""; ComponentContentBox.Text = ""; if (_planningModeBox is not null) _planningModeBox.SelectedIndex = -1; }
    private void PresentComponent_Click(object sender, RoutedEventArgs e)
    {
        if (_plannerId == 0) LoadPlanner();
        var components = _database.GetPlannerComponents(_plannerId);
        if (components.Count == 0) { MessageBox.Show("Add a planner component before starting presentation.", "Church Presenter"); return; }
        new OperatorConsoleWindow(_database, components) { Owner = this }.Show();
    }
    private void LoadLibraries() { SongList.ItemsSource = _database.GetSongs(); MediaList.ItemsSource = _database.GetMediaAssets(); }
    private void SaveSong_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SongTitleBox.Text) || string.IsNullOrWhiteSpace(SongLyricsBox.Text)) { MessageBox.Show("Enter both a song title and lyrics.", "Church Presenter"); return; }
        _database.SaveSong(SongTitleBox.Text.Trim(), SongLyricsBox.Text); LoadLibraries(); SongTitleBox.Clear(); SongLyricsBox.Clear();
    }
    private void Song_Selected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _selectedSong = SongList.SelectedItem as Song; if (_selectedSong is null) return; SongTitleBox.Text = _selectedSong.Title; SongLyricsBox.Text = _selectedSong.Lyrics;
    }
    private void SaveMedia_Click(object sender, RoutedEventArgs e)
    {
        var selected = MediaTypeBox.SelectedItem as System.Windows.Controls.ComboBoxItem; var type = selected?.Content?.ToString() ?? "Image";
        var filter = type == "Image" ? "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*" : type == "Video" ? "Video files|*.mp4;*.avi;*.wmv;*.mov|All files|*.*" : "Audio files|*.mp3;*.wav;*.wma;*.m4a|All files|*.*";
        var picker = new OpenFileDialog { Filter = filter, Title = $"Choose {type.ToLowerInvariant()} file" };
        if (picker.ShowDialog() != true) return;
        var title = string.IsNullOrWhiteSpace(MediaTitleBox.Text) ? System.IO.Path.GetFileNameWithoutExtension(picker.FileName) : MediaTitleBox.Text.Trim();
        _database.SaveMediaAsset(type, title, picker.FileName); LoadLibraries(); MediaTitleBox.Clear();
    }
    private void Media_Selected(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { _selectedMedia = MediaList.SelectedItem as MediaAsset; if (_selectedMedia is not null) MediaTitleBox.Text = _selectedMedia.Title; }
    private void Preview_Click(object sender, RoutedEventArgs e) => Present("Church Presenter", "Your presentation preview appears here.");
    private void Present(string title, string content) { try { new PresentationWindow(title, content, BackgroundColorBox.Text, FontColorBox.Text, FontSizeSlider.Value).Show(); } catch { MessageBox.Show("Use valid hex colors, for example #101828 and #FFFFFF.", "Church Presenter"); } }
}
