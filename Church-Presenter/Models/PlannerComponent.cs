namespace Church_Presenter.Models;

public enum PlannerComponentType
{
    Heading,
    Paragraph,
    Song,
    BibleReading,
    Image,
    Video,
    Audio,
    MediaText,
    Announcement,
    Countdown,
    Blank,
    Prayer,
    Offering,
    Welcome,
    Custom
}

public class PlannerComponentModel
{
    public PlannerComponentType Type { get; set; }
    public string IconGlyph { get; set; } = ""; // Emoji or glyph placeholder
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}
