using System.Windows;
using System.Windows.Controls;

namespace Church_Presenter;

public partial class SongPickerWindow : Window
{
    private readonly IReadOnlyList<Song> _songs;
    public Song? SelectedSong { get; private set; }
    public SongPickerWindow(AppDatabase database)
    {
        InitializeComponent(); _songs = database.GetSongs(); FilterSongs();
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) => FilterSongs();
    private void FilterSongs()
    {
        var query = SearchBox?.Text?.Trim() ?? string.Empty;
        SongList.ItemsSource = string.IsNullOrWhiteSpace(query) ? _songs : _songs.Where(song => song.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) || song.Lyrics.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();
    }
    private void Song_Selected(object sender, SelectionChangedEventArgs e)
    {
        var song = SongList.SelectedItem as Song; TitleText.Text = song?.Title ?? "Select a song"; LyricsBox.Text = song?.Lyrics ?? string.Empty;
    }
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        SelectedSong = SongList.SelectedItem as Song;
        if (SelectedSong is null) { MessageBox.Show("Select a song first.", "Church Presenter"); return; }
        DialogResult = true;
    }
}
