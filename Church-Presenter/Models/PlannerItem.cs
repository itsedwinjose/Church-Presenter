using System;

namespace Church_Presenter.Models;

public class PlannerItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PlannerComponentModel Component { get; set; } = new PlannerComponentModel();
    public string Title { get; set; } = "Untitled";
    public string Subtitle { get; set; } = "";
    public TimeSpan? Duration { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsExpanded { get; set; } = false;
}
