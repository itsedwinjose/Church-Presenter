using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Church_Presenter.Models;

namespace Church_Presenter.ViewModels;

public partial class MediaLibraryViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<MediaItem> displayedMedia = new();

    [ObservableProperty]
    private ObservableCollection<MediaFolder> folders = new();

    [ObservableProperty]
    private ObservableCollection<MediaTag> filterTags = new();

    [ObservableProperty]
    private ObservableCollection<string> fileFormats = new();

    [ObservableProperty]
    private ObservableCollection<string> aspectRatios = new();

    [ObservableProperty]
    private MediaItem? selectedMedia;

    [ObservableProperty]
    private MediaFolder? selectedFolder;

    [ObservableProperty]
    private string searchText = "Search media by filename, tags or description";

    [ObservableProperty]
    private string selectedMediaType = "All";

    [ObservableProperty]
    private string selectedFileFormat = "All Formats";

    [ObservableProperty]
    private string selectedAspectRatio = "All Ratios";

    [ObservableProperty]
    private string selectedTag = "All Tags";

    [ObservableProperty]
    private string selectedSort = "Date Added (Newest)";

    [ObservableProperty]
    private int totalFiles = 439;

    [ObservableProperty]
    private int totalImages = 245;

    [ObservableProperty]
    private int totalVideos = 56;

    [ObservableProperty]
    private int totalAudio = 138;

    [ObservableProperty]
    private int favoriteCount = 42;

    [ObservableProperty]
    private int recentlyAddedCount = 16;

    [ObservableProperty]
    private string storageUsed = "8.6 GB";

    [ObservableProperty]
    private string storageCapacity = "10 GB";

    [ObservableProperty]
    private string pageSummary = "Showing 1 to 16 of 156 items";

    [ObservableProperty]
    private string selectedCategory = "Bible Illustrations";

    [ObservableProperty]
    private string selectedTheme = "Sunrise, faith";

    public ObservableCollection<string> SortOptions { get; } = new()
    {
        "Date Added (Newest)",
        "Date Added (Oldest)",
        "Alphabetical",
        "Most Used"
    };

    public ObservableCollection<string> MediaTypeTabs { get; } = new()
    {
        "All Media",
        "Images",
        "Videos",
        "Audio"
    };

    public ObservableCollection<string> RepeatOptions { get; } = new()
    {
        "None",
        "Loop Once",
        "Loop Forever"
    };

    public ObservableCollection<string> StretchModes { get; } = new()
    {
        "Fill",
        "Uniform",
        "Center"
    };

    public MediaLibraryViewModel()
    {
        BuildFolders();
        BuildFilters();
        BuildMedia();

        SelectedFolder = Folders[0];
        SelectedMedia = DisplayedMedia[0];
        SelectedMedia.IsSelected = true;
    }

    private void BuildFolders()
    {
        Folders = new ObservableCollection<MediaFolder>
        {
            new() { Name = "All Media", MediaCount = 156, Icon = "🗂", IsSelected = true },
            new() { Name = "Images", MediaCount = 86, Icon = "🖼" },
            new() { Name = "Videos", MediaCount = 34, Icon = "🎬" },
            new() { Name = "Audio", MediaCount = 36, Icon = "♫" },
            new() { Name = "Background Images", MediaCount = 28, Icon = "🌄" },
            new() { Name = "Background Videos", MediaCount = 14, Icon = "📹" },
            new() { Name = "Worship", MediaCount = 18, Icon = "✨" },
            new() { Name = "Christmas", MediaCount = 12, Icon = "🎄" },
            new() { Name = "Good Friday", MediaCount = 9, Icon = "✝" },
            new() { Name = "Easter", MediaCount = 11, Icon = "🌅" },
            new() { Name = "Youth", MediaCount = 17, Icon = "⚡" },
            new() { Name = "Announcements", MediaCount = 12, Icon = "📣" },
            new() { Name = "Logos", MediaCount = 6, Icon = "🏷" },
            new() { Name = "Downloads", MediaCount = 8, Icon = "⬇" },
            new() { Name = "Favorites", MediaCount = 5, Icon = "★" }
        };
    }

    private void BuildFilters()
    {
        FilterTags = new ObservableCollection<MediaTag>
        {
            new() { Name = "Christmas", Count = 12 },
            new() { Name = "Youth", Count = 8 },
            new() { Name = "Bible", Count = 20 },
            new() { Name = "Communion", Count = 6 },
            new() { Name = "Praise", Count = 15 },
            new() { Name = "Welcome", Count = 9 },
            new() { Name = "Announcement", Count = 4 }
        };

        FileFormats = new ObservableCollection<string>
        {
            "All Formats",
            "PNG",
            "JPG",
            "JPEG",
            "WEBP",
            "SVG",
            "MP4",
            "MOV",
            "AVI",
            "WMV",
            "MP3",
            "WAV",
            "OGG"
        };

        AspectRatios = new ObservableCollection<string>
        {
            "All Ratios",
            "16:9",
            "4:3",
            "Portrait",
            "Square"
        };
    }

    private void BuildMedia()
    {
        DisplayedMedia = new ObservableCollection<MediaItem>
        {
            new ImageItem
            {
                FileName = "cross-sunset.jpg",
                Description = "Cross at sunset",
                Category = "Bible Illustrations",
                FileFormat = "JPG",
                Width = 1920,
                Height = 1080,
                FileSizeBytes = ToBytesInMb(2.4),
                CreatedDate = new DateTime(2024, 5, 20, 10, 30, 0),
                ModifiedDate = new DateTime(2024, 5, 20, 10, 30, 0),
                IsFavorite = true,
                Theme = "Sunrise, faith",
                StretchMode = "Fill",
                BackgroundOpacity = 100,
                Rotation = 0,
                Tags = new ObservableCollection<string> { "cross", "jesus", "sunrise", "faith" },
                UsedInPlanners = new ObservableCollection<string>
                {
                    "Sunday Service - 19 May 2024",
                    "Youth Worship - 12 May 2024",
                    "Good Friday - 29 Mar 2024"
                }
            },
            new ImageItem
            {
                FileName = "mountains.jpg",
                Description = "Landscape background",
                Category = "Background Images",
                FileFormat = "JPG",
                Width = 1920,
                Height = 1080,
                FileSizeBytes = ToBytesInMb(1.8),
                CreatedDate = new DateTime(2024, 5, 18, 8, 15, 0),
                ModifiedDate = new DateTime(2024, 5, 18, 9, 45, 0),
                Theme = "Creation",
                StretchMode = "Fill",
                Tags = new ObservableCollection<string> { "nature", "mountains", "background" }
            },
            new VideoItem
            {
                FileName = "worship-night.mp4",
                Description = "Worship crowd silhouette",
                Category = "Worship Videos",
                FileFormat = "MP4",
                Width = 1920,
                Height = 1080,
                Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(45),
                FrameRate = 30,
                Codec = "H.264",
                FileSizeBytes = ToBytesInMb(128),
                CreatedDate = new DateTime(2024, 5, 16, 7, 30, 0),
                ModifiedDate = new DateTime(2024, 5, 16, 8, 10, 0),
                Theme = "Worship Night",
                Loop = false,
                Mute = false,
                Volume = 80,
                Tags = new ObservableCollection<string> { "worship", "night", "crowd" }
            },
            new VideoItem
            {
                FileName = "god-is-faithful.mp4",
                Description = "Lyric motion graphic",
                Category = "Announcements",
                FileFormat = "MP4",
                Width = 1920,
                Height = 1080,
                Duration = TimeSpan.FromSeconds(15),
                FrameRate = 60,
                Codec = "H.265",
                FileSizeBytes = ToBytesInMb(68),
                CreatedDate = new DateTime(2024, 5, 14, 9, 0, 0),
                ModifiedDate = new DateTime(2024, 5, 14, 11, 40, 0),
                Theme = "Faithful",
                Loop = true,
                Mute = true,
                Volume = 0,
                Tags = new ObservableCollection<string> { "lyrics", "faith", "announcement" }
            },
            new AudioItem
            {
                FileName = "jesus-loves-me.mp3",
                Description = "Piano arrangement",
                Category = "Worship Audio",
                FileFormat = "MP3",
                Duration = TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(32),
                Bitrate = 192000,
                SampleRate = 44100,
                Channels = 2,
                FileSizeBytes = ToBytesInMb(4.2),
                CreatedDate = new DateTime(2024, 5, 12, 18, 10, 0),
                ModifiedDate = new DateTime(2024, 5, 12, 18, 25, 0),
                Theme = "Children",
                Tags = new ObservableCollection<string> { "piano", "children", "worship" }
            },
            new AudioItem
            {
                FileName = "amazing-grace.mp3",
                Description = "Soft worship instrumental",
                Category = "Worship Audio",
                FileFormat = "MP3",
                Duration = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(18),
                Bitrate = 256000,
                SampleRate = 48000,
                Channels = 2,
                FileSizeBytes = ToBytesInMb(5.1),
                CreatedDate = new DateTime(2024, 5, 11, 13, 0, 0),
                ModifiedDate = new DateTime(2024, 5, 11, 15, 50, 0),
                IsFavorite = true,
                Theme = "Grace",
                Tags = new ObservableCollection<string> { "grace", "instrumental", "worship" }
            },
            new ImageItem
            {
                FileName = "holy-spirit.jpg",
                Description = "Dove and clouds",
                Category = "Bible Illustrations",
                FileFormat = "JPG",
                Width = 1600,
                Height = 900,
                FileSizeBytes = ToBytesInMb(2.1),
                CreatedDate = new DateTime(2024, 5, 10, 14, 20, 0),
                ModifiedDate = new DateTime(2024, 5, 10, 16, 5, 0),
                IsFavorite = true,
                Theme = "Holy Spirit",
                Tags = new ObservableCollection<string> { "dove", "spirit", "clouds" }
            },
            new ImageItem
            {
                FileName = "bible-open.jpg",
                Description = "Open Bible on table",
                Category = "Bible Illustrations",
                FileFormat = "JPG",
                Width = 1920,
                Height = 1080,
                FileSizeBytes = ToBytesInMb(1.3),
                CreatedDate = new DateTime(2024, 5, 9, 11, 45, 0),
                ModifiedDate = new DateTime(2024, 5, 9, 12, 5, 0),
                Theme = "Scripture",
                Tags = new ObservableCollection<string> { "bible", "scripture", "reading" }
            },
            new VideoItem
            {
                FileName = "nature-4k.mp4",
                Description = "4K valley motion loop",
                Category = "Background Videos",
                FileFormat = "MP4",
                Width = 3840,
                Height = 2160,
                Duration = TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(10),
                FrameRate = 60,
                Codec = "H.265",
                FileSizeBytes = ToBytesInMb(96),
                CreatedDate = new DateTime(2024, 5, 7, 17, 30, 0),
                ModifiedDate = new DateTime(2024, 5, 7, 17, 55, 0),
                Theme = "Nature",
                Loop = true,
                Mute = true,
                Volume = 0,
                Tags = new ObservableCollection<string> { "background", "nature", "loop" }
            },
            new ImageItem
            {
                FileName = "green-leaves.jpg",
                Description = "Fresh greenery texture",
                Category = "Background Images",
                FileFormat = "JPG",
                Width = 1920,
                Height = 1080,
                FileSizeBytes = ToBytesInMb(1.1),
                CreatedDate = new DateTime(2024, 5, 6, 9, 5, 0),
                ModifiedDate = new DateTime(2024, 5, 6, 9, 18, 0),
                Theme = "Creation",
                Tags = new ObservableCollection<string> { "green", "nature", "fresh" }
            },
            new AudioItem
            {
                FileName = "praise-and-worship.mp3",
                Description = "Ambient pad loop",
                Category = "Worship Audio",
                FileFormat = "MP3",
                Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(30),
                Bitrate = 192000,
                SampleRate = 44100,
                Channels = 2,
                FileSizeBytes = ToBytesInMb(3.5),
                CreatedDate = new DateTime(2024, 5, 3, 19, 40, 0),
                ModifiedDate = new DateTime(2024, 5, 3, 20, 10, 0),
                IsFavorite = true,
                Theme = "Praise",
                Tags = new ObservableCollection<string> { "ambient", "praise", "loop" }
            },
            new ImageItem
            {
                FileName = "sunrise.jpg",
                Description = "Beach sunrise backdrop",
                Category = "Background Images",
                FileFormat = "JPG",
                Width = 1920,
                Height = 1080,
                FileSizeBytes = ToBytesInMb(1.7),
                CreatedDate = new DateTime(2024, 5, 1, 6, 55, 0),
                ModifiedDate = new DateTime(2024, 5, 1, 7, 10, 0),
                Theme = "Welcome",
                Tags = new ObservableCollection<string> { "sunrise", "welcome", "background" }
            }
        };
    }

    private static long ToBytesInMb(double value) => (long)(value * 1024 * 1024);
}
