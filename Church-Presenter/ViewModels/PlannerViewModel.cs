using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Church_Presenter.Models;

namespace Church_Presenter.ViewModels;

public partial class PlannerViewModel : ObservableObject
{
    public PlannerViewModel()
    {
        LoadPlaceholder();
    }

    [ObservableProperty]
    private Planner _currentPlanner = new Planner { Name = "Sunday Morning Worship", Theme = "Faith & Hope" };

    [ObservableProperty]
    private ObservableCollection<PlannerItem> _items = new ObservableCollection<PlannerItem>();

    [ObservableProperty]
    private ObservableCollection<PlannerComponentModel> _toolbox = new ObservableCollection<PlannerComponentModel>();

    [ObservableProperty]
    private PlannerItem? _selectedItem;

    private void LoadPlaceholder()
    {
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Heading, IconGlyph = "H", Title = "Heading", Description = "Section heading" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Paragraph, IconGlyph = "T", Title = "Paragraph", Description = "Simple text block" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Song, IconGlyph = "♪", Title = "Song / Lyrics", Description = "Add a song from the library" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.BibleReading, IconGlyph = "📖", Title = "Bible Reading", Description = "Add scripture reading" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Image, IconGlyph = "🖼", Title = "Image", Description = "Add an image" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Video, IconGlyph = "🎬", Title = "Video", Description = "Add a video" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Audio, IconGlyph = "🔊", Title = "Audio / Music", Description = "Add audio" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.MediaText, IconGlyph = "🧩", Title = "Media + Text", Description = "Media with overlay text" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Announcement, IconGlyph = "📣", Title = "Announcement", Description = "Display an announcement" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Countdown, IconGlyph = "⏱", Title = "Countdown", Description = "Countdown timer" });

        // Placeholder planner items
        Items.Add(new PlannerItem { Title = "Heading", Subtitle = "Welcome", Component = new PlannerComponentModel { Type = PlannerComponentType.Heading, IconGlyph = "H", Title = "Heading" } });
        Items.Add(new PlannerItem { Title = "Song", Subtitle = "Amazing Grace", Component = new PlannerComponentModel { Type = PlannerComponentType.Song, IconGlyph = "♪", Title = "Song / Lyrics" }, Duration = System.TimeSpan.FromMinutes(4) });
        Items.Add(new PlannerItem { Title = "Bible Reading", Subtitle = "John 3:16-21", Component = new PlannerComponentModel { Type = PlannerComponentType.BibleReading, IconGlyph = "📖", Title = "Bible Reading" }, Duration = System.TimeSpan.FromMinutes(2) });
        Items.Add(new PlannerItem { Title = "Image", Subtitle = "Cross Background", Component = new PlannerComponentModel { Type = PlannerComponentType.Image, IconGlyph = "🖼", Title = "Image" }, Duration = System.TimeSpan.FromSeconds(15) });
        Items.Add(new PlannerItem { Title = "Song", Subtitle = "How Great Thou Art", Component = new PlannerComponentModel { Type = PlannerComponentType.Song, IconGlyph = "♪", Title = "Song / Lyrics" }, Duration = System.TimeSpan.FromMinutes(5) });
        Items.Add(new PlannerItem { Title = "Video", Subtitle = "Announcements.mp4", Component = new PlannerComponentModel { Type = PlannerComponentType.Video, IconGlyph = "🎬", Title = "Video" }, Duration = System.TimeSpan.FromMinutes(2) });
        Items.Add(new PlannerItem { Title = "Prayer", Subtitle = "Closing Prayer", Component = new PlannerComponentModel { Type = PlannerComponentType.Prayer, IconGlyph = "🙏", Title = "Prayer" } });
    }

    [RelayCommand]
    private void AddNewItem()
    {
        Items.Add(new PlannerItem { Title = "New Item", Subtitle = "", Component = new PlannerComponentModel { Type = PlannerComponentType.Custom, IconGlyph = "✚", Title = "Custom" } });
    }

    [RelayCommand]
    private void DuplicateSelected()
    {
        if (SelectedItem is null) return;
        var copy = new PlannerItem { Title = SelectedItem.Title, Subtitle = SelectedItem.Subtitle, Component = SelectedItem.Component, Duration = SelectedItem.Duration };
        Items.Add(copy);
    }
}
