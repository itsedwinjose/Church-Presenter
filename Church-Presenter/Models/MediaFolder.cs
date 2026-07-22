using System.Collections.ObjectModel;

namespace Church_Presenter.Models;

public class MediaFolder
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "🗂";
    public int MediaCount { get; set; }
    public ObservableCollection<MediaFolder> SubFolders { get; set; } = new();
    public bool IsSelected { get; set; }
    public string CountDisplay => MediaCount.ToString();
}

public class MediaTag
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public string DisplayName => Count > 0 ? $"{Name} ({Count})" : Name;
}
