using System.Collections.Generic;

namespace Church_Presenter.Models;

public class Planner
{
    public string Name { get; set; } = "New Service";
    public string Theme { get; set; } = "Default";
    public System.DateTime Date { get; set; } = System.DateTime.Today;
    public List<PlannerItem> Items { get; set; } = new List<PlannerItem>();
}
