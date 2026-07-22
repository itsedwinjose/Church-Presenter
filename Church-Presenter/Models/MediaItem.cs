using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Church_Presenter.Models;

public abstract class MediaItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ObservableCollection<string> Tags { get; set; } = new();
    public bool IsFavorite { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Today;
    public DateTime ModifiedDate { get; set; } = DateTime.Today;
    public string FilePath { get; set; } = string.Empty;
    public string AddedBy { get; set; } = "Admin";
    public ObservableCollection<string> UsedInPlanners { get; set; } = new();
    public string Theme { get; set; } = "Default";
    public string StretchMode { get; set; } = "Fill";
    public int BackgroundOpacity { get; set; } = 100;
    public int Rotation { get; set; }
    public bool IsSelected { get; set; }

    public abstract string MediaType { get; }
    public abstract string FileTypeDisplay { get; }
    public abstract string ThumbnailGlyph { get; }
    public abstract string AccentColor { get; }
    public virtual string ThumbnailStartColor => AccentColor;
    public virtual string ThumbnailEndColor => "#F7F9FF";
    public virtual string ResolutionDisplay => "—";
    public virtual string AspectRatioDisplay => "—";
    public virtual string DurationDisplay => "—";
    public virtual string DetailOneLabel => "Category";
    public virtual string DetailOneValue => Category;
    public virtual string DetailTwoLabel => "Tags";
    public virtual string DetailTwoValue => TagsDisplay;
    public virtual string DetailThreeLabel => "Dimensions";
    public virtual string DetailThreeValue => ResolutionDisplay;
    public virtual string DetailFourLabel => "Modified";
    public virtual string DetailFourValue => ModifiedDisplay;
    public virtual string MetaLine => $"{MediaType}  •  {FileTypeDisplay}  •  {FileSizeDisplay}";
    public string FileSizeDisplay => FormatFileSize(FileSizeBytes);
    public string CreatedDisplay => CreatedDate.ToString("dd MMM yyyy hh:mm tt");
    public string ModifiedDisplay => ModifiedDate.ToString("dd MMM yyyy hh:mm tt");
    public string TagsDisplay => Tags.Count == 0 ? "—" : string.Join(", ", Tags);
    public string FavoriteGlyph => IsFavorite ? "★" : "☆";
    public int UsageCount => UsedInPlanners.Count;
    public string UsedInDisplay => UsageCount == 0 ? "Not yet used" : string.Join(Environment.NewLine, UsedInPlanners);

    protected static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.#} {sizes[order]}";
    }

    protected static string FormatDuration(TimeSpan duration)
        => duration.Hours > 0
            ? $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}"
            : $"{duration.Minutes:D2}:{duration.Seconds:D2}";

    protected static string FormatAspectRatio(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return "—";
        }

        var gcd = Gcd(width, height);
        return $"{width / gcd}:{height / gcd}";
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            var temp = b;
            b = a % b;
            a = temp;
        }

        return Math.Abs(a);
    }
}

public class ImageItem : MediaItem
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string FileFormat { get; set; } = "JPG";

    public override string MediaType => "Image";
    public override string FileTypeDisplay => FileFormat.ToUpperInvariant();
    public override string ThumbnailGlyph => "🖼";
    public override string AccentColor => "#7C5CFF";
    public override string ResolutionDisplay => $"{Width} × {Height}";
    public override string AspectRatioDisplay => FormatAspectRatio(Width, Height);
    public override string DetailThreeLabel => "Resolution";
    public override string DetailThreeValue => ResolutionDisplay;
    public override string DetailFourLabel => "Aspect Ratio";
    public override string DetailFourValue => AspectRatioDisplay;
}

public class VideoItem : MediaItem
{
    public int Width { get; set; }
    public int Height { get; set; }
    public TimeSpan Duration { get; set; }
    public int FrameRate { get; set; }
    public string Codec { get; set; } = "H.264";
    public string FileFormat { get; set; } = "MP4";
    public bool Loop { get; set; }
    public bool Mute { get; set; }
    public int Volume { get; set; } = 80;

    public override string MediaType => "Video";
    public override string FileTypeDisplay => FileFormat.ToUpperInvariant();
    public override string ThumbnailGlyph => "▶";
    public override string AccentColor => "#4F46E5";
    public override string ResolutionDisplay => $"{Width} × {Height}";
    public override string AspectRatioDisplay => FormatAspectRatio(Width, Height);
    public override string DurationDisplay => FormatDuration(Duration);
    public string FrameRateDisplay => $"{FrameRate} FPS";
    public override string DetailThreeLabel => "Duration";
    public override string DetailThreeValue => DurationDisplay;
    public override string DetailFourLabel => "Resolution";
    public override string DetailFourValue => ResolutionDisplay;
}

public class AudioItem : MediaItem
{
    public int Bitrate { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; } = 2;
    public TimeSpan Duration { get; set; }
    public string FileFormat { get; set; } = "MP3";

    public override string MediaType => "Audio";
    public override string FileTypeDisplay => FileFormat.ToUpperInvariant();
    public override string ThumbnailGlyph => "♫";
    public override string AccentColor => "#C026D3";
    public override string DurationDisplay => FormatDuration(Duration);
    public string BitrateDisplay => $"{Bitrate / 1000} kbps";
    public string SampleRateDisplay => $"{SampleRate / 1000.0:0.#} kHz";
    public string ChannelsDisplay => Channels == 1 ? "Mono" : Channels == 2 ? "Stereo" : $"{Channels} Channels";
    public override string DetailThreeLabel => "Duration";
    public override string DetailThreeValue => DurationDisplay;
    public override string DetailFourLabel => "Bitrate";
    public override string DetailFourValue => BitrateDisplay;
}
