using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using Church_Presenter.Models;

namespace Church_Presenter.ViewModels;

public sealed class PlannerViewModel : ObservableObject
{
    private readonly AppDatabase _database = new();
    private bool _suppressPlanSelectionChange;
    private int _currentPlannerId;
    private PlannerSummary? _selectedPlan;
    private PlannerComponent? _selectedItem;
    private DateTime _serviceDate = DateTime.Today;
    private string _serviceName = "Sunday Service";
    private string _themeName = string.Empty;
    private string _selectedItemType = "Select a component";
    private string _selectedItemTitle = string.Empty;
    private string _selectedItemContent = string.Empty;
    private string _selectedPresentationMode = "Static";
    private int _selectedScrollSpeed = 2;
    private bool _isNewPlan = true;
    private string _statusMessage = "Create a new plan or open an existing one.";

    public PlannerViewModel()
    {
        _database.Initialize();
        PresentationModes = ["Static", "Scrollable"];
        Plans = [];
        Items = [];
        Toolbox = [];

        NewPlanCommand = new RelayCommand(NewPlan);
        SavePlanCommand = new RelayCommand(SavePlan);
        DeletePlanCommand = new RelayCommand(DeletePlan);
        AddComponentCommand = new RelayCommand<PlannerComponentModel?>(AddComponent);
        SaveSelectedItemCommand = new RelayCommand(SaveSelectedItem);
        RemoveSelectedItemCommand = new RelayCommand(RemoveSelectedItem);
        MoveSelectedItemCommand = new RelayCommand<string?>(MoveSelectedItem);
        PresentPlanCommand = new RelayCommand(PresentPlan);

        LoadToolbox();
        LoadPlans();

        if (Plans.Count > 0)
        {
            SelectedPlan = Plans[0];
        }
        else
        {
            ResetForNewPlan();
        }
    }

    public IReadOnlyList<string> PresentationModes { get; }
    public ObservableCollection<PlannerSummary> Plans { get; }
    public ObservableCollection<PlannerComponent> Items { get; }
    public ObservableCollection<PlannerComponentModel> Toolbox { get; }

    public IRelayCommand NewPlanCommand { get; }
    public IRelayCommand SavePlanCommand { get; }
    public IRelayCommand DeletePlanCommand { get; }
    public IRelayCommand<PlannerComponentModel?> AddComponentCommand { get; }
    public IRelayCommand SaveSelectedItemCommand { get; }
    public IRelayCommand RemoveSelectedItemCommand { get; }
    public IRelayCommand<string?> MoveSelectedItemCommand { get; }
    public IRelayCommand PresentPlanCommand { get; }

    public PlannerSummary? SelectedPlan
    {
        get => _selectedPlan;
        set
        {
            if (!SetProperty(ref _selectedPlan, value))
            {
                return;
            }

            if (_suppressPlanSelectionChange || value is null)
            {
                return;
            }

            LoadPlanner(value.Id);
        }
    }

    public PlannerComponent? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value))
            {
                return;
            }

            if (value is null)
            {
                ClearComponentEditor();
                return;
            }

            SelectedItemType = value.Type == "Song" ? "Song — edits apply only to this planner" : value.Type;
            SelectedItemTitle = value.Title;
            SelectedItemContent = value.Content;
            SelectedPresentationMode = string.IsNullOrWhiteSpace(value.PresentationMode) ? "Static" : value.PresentationMode;
            SelectedScrollSpeed = value.ScrollSpeed < 1 ? 1 : value.ScrollSpeed;
            OnPropertyChanged(nameof(HasSelectedItem));
        }
    }

    public DateTime ServiceDate
    {
        get => _serviceDate;
        set
        {
            if (!SetProperty(ref _serviceDate, value))
            {
                return;
            }

            OnPropertyChanged(nameof(PlannerDateSummary));
            OnPropertyChanged(nameof(SelectedPlanSummary));
        }
    }

    public string ServiceName
    {
        get => _serviceName;
        set
        {
            if (!SetProperty(ref _serviceName, value))
            {
                return;
            }

            OnPropertyChanged(nameof(PlannerHeading));
            OnPropertyChanged(nameof(SelectedPlanSummary));
        }
    }

    public string ThemeName
    {
        get => _themeName;
        set => SetProperty(ref _themeName, value);
    }

    public string SelectedItemType
    {
        get => _selectedItemType;
        set => SetProperty(ref _selectedItemType, value);
    }

    public string SelectedItemTitle
    {
        get => _selectedItemTitle;
        set => SetProperty(ref _selectedItemTitle, value);
    }

    public string SelectedItemContent
    {
        get => _selectedItemContent;
        set => SetProperty(ref _selectedItemContent, value);
    }

    public string SelectedPresentationMode
    {
        get => _selectedPresentationMode;
        set => SetProperty(ref _selectedPresentationMode, value);
    }

    public int SelectedScrollSpeed
    {
        get => _selectedScrollSpeed;
        set => SetProperty(ref _selectedScrollSpeed, value);
    }

    public bool IsNewPlan
    {
        get => _isNewPlan;
        set
        {
            if (!SetProperty(ref _isNewPlan, value))
            {
                return;
            }

            OnPropertyChanged(nameof(PlannerHeading));
            OnPropertyChanged(nameof(SelectedPlanSummary));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string PlannerHeading => IsNewPlan ? "New Planner" : ServiceName;
    public string PlannerDateSummary => ServiceDate.ToString("dddd, dd MMM yyyy");
    public bool HasSelectedItem => SelectedItem is not null;
    public bool HasSavedPlan => _currentPlannerId > 0;
    public string SelectedPlanSummary => IsNewPlan ? "Unsaved planner" : $"{ServiceDate:dd MMM yyyy} • {ServiceName}";

    private void NewPlan()
    {
        _suppressPlanSelectionChange = true;
        SelectedPlan = null;
        _suppressPlanSelectionChange = false;
        ResetForNewPlan();
    }

    private void SavePlan()
    {
        var trimmedName = ServiceName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            MessageBox.Show("Enter a service name before saving the planner.", "Church Presenter");
            return;
        }

        try
        {
            if (_currentPlannerId == 0)
            {
                _currentPlannerId = _database.CreatePlanner(ServiceDate, trimmedName, ThemeName);
                IsNewPlan = false;
                OnPropertyChanged(nameof(HasSavedPlan));
                StatusMessage = "Planner saved. You can now add components.";
            }
            else
            {
                _database.UpdatePlanner(_currentPlannerId, ServiceDate, trimmedName, ThemeName);
                StatusMessage = "Planner changes saved.";
            }

            LoadPlans(_currentPlannerId);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            MessageBox.Show("A planner with this date and service name already exists. Choose a different combination.", "Church Presenter");
        }
    }

    private void DeletePlan()
    {
        if (_currentPlannerId == 0 || SelectedPlan is null)
        {
            MessageBox.Show("Select a saved planner first.", "Church Presenter");
            return;
        }

        if (MessageBox.Show($"Delete planner '{SelectedPlan.ServiceName}'? This will remove all planner components.", "Church Presenter", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        _database.DeletePlanner(_currentPlannerId);
        var deletedPlannerId = _currentPlannerId;
        LoadPlans();

        var nextPlan = Plans.FirstOrDefault();
        if (nextPlan is not null)
        {
            _suppressPlanSelectionChange = true;
            SelectedPlan = nextPlan;
            _suppressPlanSelectionChange = false;
            LoadPlanner(nextPlan.Id);
        }
        else
        {
            _suppressPlanSelectionChange = true;
            SelectedPlan = null;
            _suppressPlanSelectionChange = false;
            ResetForNewPlan();
        }

        StatusMessage = $"Planner {deletedPlannerId} deleted.";
    }

    private void AddComponent(PlannerComponentModel? component)
    {
        if (component is null)
        {
            return;
        }

        if (_currentPlannerId == 0)
        {
            MessageBox.Show("Save the planner before adding components.", "Church Presenter");
            return;
        }

        string type;
        string title;
        string content;

        switch (component.Type)
        {
            case PlannerComponentType.Song:
                var songPicker = new SongPickerWindow(_database) { Owner = Application.Current.MainWindow };
                if (songPicker.ShowDialog() != true || songPicker.SelectedSong is null)
                {
                    return;
                }

                type = "Song";
                title = songPicker.SelectedSong.Title;
                content = songPicker.SelectedSong.Lyrics;
                break;

            case PlannerComponentType.BibleReading:
                var readingPicker = new BibleReadingPickerWindow(_database) { Owner = Application.Current.MainWindow };
                if (readingPicker.ShowDialog() != true || readingPicker.ReadingTitle is null || readingPicker.ReadingText is null)
                {
                    return;
                }

                type = "Bible Reading";
                title = readingPicker.ReadingTitle;
                content = readingPicker.ReadingText;
                break;

            default:
                (type, title, content) = GetDefaultComponentValues(component.Type);
                break;
        }

        _database.AddPlannerComponent(_currentPlannerId, type, title, content);
        RefreshItems(selectLast: true);
        LoadPlans(_currentPlannerId);
        StatusMessage = $"Added {type} to the planner.";
    }

    private void SaveSelectedItem()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var title = SelectedItemTitle.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("Enter a title for the selected component.", "Church Presenter");
            return;
        }

        var speed = Math.Clamp(SelectedScrollSpeed, 1, 100);
        SelectedScrollSpeed = speed;

        _database.UpdatePlannerComponentSettings(SelectedItem.Id, title, SelectedItemContent.TrimEnd(), SelectedPresentationMode, speed);
        RefreshItems(SelectedItem.Id);
        LoadPlans(_currentPlannerId);
        StatusMessage = "Component changes saved.";
    }

    private void RemoveSelectedItem()
    {
        if (SelectedItem is null)
        {
            return;
        }

        if (MessageBox.Show($"Remove '{SelectedItem.Title}' from this planner?", "Church Presenter", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        _database.DeletePlannerComponent(SelectedItem.Id);
        RefreshItems();
        LoadPlans(_currentPlannerId);
        StatusMessage = "Component removed.";
    }

    private void MoveSelectedItem(string? directionValue)
    {
        if (!int.TryParse(directionValue, out var direction))
        {
            return;
        }

        if (SelectedItem is null || _currentPlannerId == 0)
        {
            return;
        }

        _database.MovePlannerComponent(_currentPlannerId, SelectedItem.Id, direction);
        RefreshItems(SelectedItem.Id);
        StatusMessage = direction < 0 ? "Component moved up." : "Component moved down.";
    }

    private void PresentPlan()
    {
        if (_currentPlannerId == 0)
        {
            MessageBox.Show("Save the planner before opening the presentation console.", "Church Presenter");
            return;
        }

        var components = _database.GetPlannerComponents(_currentPlannerId);
        if (components.Count == 0)
        {
            MessageBox.Show("Add at least one planner component before presenting.", "Church Presenter");
            return;
        }

        new OperatorConsoleWindow(_database, components) { Owner = Application.Current.MainWindow }.Show();
    }

    private void LoadToolbox()
    {
        Toolbox.Clear();
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Heading, IconGlyph = "H", Title = "Heading", Description = "Section heading" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Paragraph, IconGlyph = "T", Title = "Paragraph", Description = "Simple text block" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Song, IconGlyph = "♪", Title = "Song / Lyrics", Description = "Add a song from the library" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.BibleReading, IconGlyph = "📖", Title = "Bible Reading", Description = "Add scripture reading" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Image, IconGlyph = "🖼", Title = "Image", Description = "Add an image path" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Video, IconGlyph = "🎬", Title = "Video", Description = "Add a video path" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Audio, IconGlyph = "🔊", Title = "Audio / Music", Description = "Add audio or music" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.MediaText, IconGlyph = "🧩", Title = "Media + Text", Description = "Media with overlay text" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Announcement, IconGlyph = "📣", Title = "Announcement", Description = "Display an announcement" });
        Toolbox.Add(new PlannerComponentModel { Type = PlannerComponentType.Countdown, IconGlyph = "⏱", Title = "Countdown", Description = "Countdown timer" });
    }

    private void LoadPlans(int? selectedPlannerId = null)
    {
        var plans = _database.GetPlanners();
        Plans.Clear();
        foreach (var plan in plans)
        {
            Plans.Add(plan);
        }

        if (selectedPlannerId is not int plannerId)
        {
            return;
        }

        var match = Plans.FirstOrDefault(plan => plan.Id == plannerId);
        if (match is null)
        {
            return;
        }

        _suppressPlanSelectionChange = true;
        SelectedPlan = match;
        _suppressPlanSelectionChange = false;
    }

    private void LoadPlanner(int plannerId)
    {
        var planner = _database.GetPlanner(plannerId);
        if (planner is null)
        {
            return;
        }

        _currentPlannerId = planner.Id;
        IsNewPlan = false;
        OnPropertyChanged(nameof(HasSavedPlan));
        ServiceDate = planner.ServiceDate;
        ServiceName = planner.ServiceName;
        ThemeName = planner.ThemeName;
        RefreshItems();
        StatusMessage = "Planner loaded.";
    }

    private void RefreshItems(int? reselectId = null, bool selectLast = false)
    {
        var components = _currentPlannerId == 0
            ? Array.Empty<PlannerComponent>()
            : _database.GetPlannerComponents(_currentPlannerId);

        Items.Clear();
        foreach (var component in components)
        {
            Items.Add(component);
        }

        PlannerComponent? selected = null;
        if (reselectId is int componentId)
        {
            selected = Items.FirstOrDefault(item => item.Id == componentId);
        }
        else if (selectLast)
        {
            selected = Items.LastOrDefault();
        }

        SelectedItem = selected;
        if (selected is null)
        {
            ClearComponentEditor();
        }
    }

    private void ResetForNewPlan()
    {
        _currentPlannerId = 0;
        IsNewPlan = true;
        OnPropertyChanged(nameof(HasSavedPlan));
        ServiceDate = DateTime.Today;
        ServiceName = "Sunday Service";
        ThemeName = string.Empty;
        Items.Clear();
        ClearComponentEditor();
        StatusMessage = "New planner ready. Save it before adding components.";
    }

    private void ClearComponentEditor()
    {
        SelectedItemType = "Select a component";
        SelectedItemTitle = string.Empty;
        SelectedItemContent = string.Empty;
        SelectedPresentationMode = "Static";
        SelectedScrollSpeed = int.TryParse(_database.Get("ScrollSpeed", "2"), out var speed) ? speed : 2;
        OnPropertyChanged(nameof(HasSelectedItem));
    }

    private static (string Type, string Title, string Content) GetDefaultComponentValues(PlannerComponentType type) => type switch
    {
        PlannerComponentType.Heading => ("Heading", "New heading", string.Empty),
        PlannerComponentType.Paragraph => ("Paragraph", "Paragraph", "Enter text here."),
        PlannerComponentType.Image => ("Image", "Image", "Choose or enter an image file path."),
        PlannerComponentType.Video => ("Video", "Video", "Choose or enter a video file path."),
        PlannerComponentType.Audio => ("Music", "Music", "Choose or enter a music file path."),
        PlannerComponentType.MediaText => ("Image + Text", "Image and text", "Image path and caption."),
        PlannerComponentType.Announcement => ("Announcement", "Announcement", "Enter announcement text."),
        PlannerComponentType.Countdown => ("Countdown", "Countdown", "05:00"),
        _ => (type.ToString(), type.ToString(), string.Empty)
    };
}
