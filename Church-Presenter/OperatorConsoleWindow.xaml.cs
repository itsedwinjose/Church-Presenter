using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace Church_Presenter;

public partial class OperatorConsoleWindow : Window
{
    private readonly AppDatabase _database;
    private readonly IReadOnlyList<PlannerComponent> _components;
    private PresentationWindow? _presentation;
    private bool _isMarqueePlaying;

    public OperatorConsoleWindow(AppDatabase database, IReadOnlyList<PlannerComponent> components)
    {
        InitializeComponent();
        _database = database;
        _components = components;
        ComponentList.ItemsSource = components;
        if (components.Count > 0) ComponentList.SelectedIndex = 0;
        ScreenStatus.Text = Forms.Screen.AllScreens.Length > 1 ? "Second display detected — HDMI output will use it." : "No second display detected — output will use this screen.";
        if (ComponentList.SelectedItem is PlannerComponent pc) ScrollSpeedDisplay.Text = pc.ScrollSpeed.ToString();
        Closed += (_, _) => ClosePresentation();
        UpdatePresentationToggle();
        UpdatePlaybackControls();
    }

    private PlannerComponent? Selected => ComponentList.SelectedItem as PlannerComponent;

    private void Component_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (Selected is null) return;
        PreviewTitle.Text = Selected.Title;
        PreviewContent.Text = Selected.Content;
        DisplayModeBox.SelectedIndex = Selected.PresentationMode == "Scrollable" ? 1 : 0;
        ScrollSpeedDisplay.Text = Selected.ScrollSpeed.ToString();
        ApplyPreviewMode();
        UpdatePlaybackControls();

        if (_presentation is not null)
            StartPresentation(IsScrollableModeSelected);
    }

    private void DisplayMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Selected is null || DisplayModeBox.SelectedItem is not ComboBoxItem mode) return;

        Selected.PresentationMode = mode.Content?.ToString() ?? "Static";
        _database.SetPlannerPresentationMode(Selected.Id, Selected.PresentationMode);
        ApplyPreviewMode();
        UpdatePlaybackControls();

        if (_presentation is not null)
            StartPresentation(IsScrollableModeSelected);
    }

    private void ApplyPreviewMode() => PreviewScroller.VerticalScrollBarVisibility = IsScrollableModeSelected ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        StartPresentation(true);
    }

    private void StartPresentation(bool startMarquee)
    {
        if (Selected is null) { MessageBox.Show("Select a service item first.", "Church Presenter"); return; }

        var background = _database.Get("BackgroundColor", "#FFFFFF");
        var (foreground, size) = _database.GetPresentationStyle(Selected.Type);
        var speed = GetSelectedScrollSpeed();
        var scrollable = IsScrollableModeSelected;

        if (_presentation is null)
        {
            _presentation = new PresentationWindow(Selected.Title, Selected.Content, background, foreground, size, scrollable, speed);
            _presentation.Closed += Presentation_Closed;
            MoveToPresentationScreen(_presentation);
            _presentation.Show();
        }
        else
        {
            _presentation.UpdatePresentation(Selected.Title, Selected.Content, background, foreground, size, scrollable, speed);
            if (!_presentation.IsVisible)
                _presentation.Show();
            _presentation.Activate();
        }

        Selected.IsCompleted = true;
        ComponentList.Items.Refresh();
        ApplyMarqueeState(scrollable && startMarquee);
        UpdatePresentationToggle();
        UpdatePlaybackControls();
    }

    private void ClosePresentation_Click(object sender, RoutedEventArgs e)
    {
        ClosePresentation();
    }

    private void ClosePresentation()
    {
        if (_presentation is null)
        {
            _isMarqueePlaying = false;
            UpdatePresentationToggle();
            UpdatePlaybackControls();
            return;
        }

        var presentation = _presentation;
        _presentation = null;
        _isMarqueePlaying = false;

        try
        {
            presentation.Closed -= Presentation_Closed;
            presentation.StopScrolling();
            presentation.Close();
        }
        catch { }

        _presentation = null;
        UpdatePresentationToggle();
        UpdatePlaybackControls();
    }

    private void TogglePresentation_Click(object sender, RoutedEventArgs e)
    {
        if (_presentation is null) StartPresentation(IsScrollableModeSelected);
        else ClosePresentation();
    }

    private void UpdatePresentationToggle()
    {
        if (PresentationToggleButton is not null)
            PresentationToggleButton.Content = _presentation is null ? "Open presentation" : "Close presentation";
    }

    private void IncreaseSpeed_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null) { MessageBox.Show("Select a service item first.", "Church Presenter"); return; }
        var current = GetSelectedScrollSpeed();
        current++;
        if (current > 100) current = 100;
        _database.SetPlannerComponentScrollSpeed(Selected.Id, current);
        Selected.ScrollSpeed = current;
        ScrollSpeedDisplay.Text = current.ToString();
        if (_presentation is not null) _presentation.SetScrollSpeed(current);
    }

    private void DecreaseSpeed_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null) { MessageBox.Show("Select a service item first.", "Church Presenter"); return; }
        var current = GetSelectedScrollSpeed();
        current--;
        if (current < 1) current = 1;
        _database.SetPlannerComponentScrollSpeed(Selected.Id, current);
        Selected.ScrollSpeed = current;
        ScrollSpeedDisplay.Text = current.ToString();
        if (_presentation is not null) _presentation.SetScrollSpeed(current);
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        if (_presentation is null) return;
        ClosePresentation();
        if (PlayPauseToggle is not null) { PlayPauseToggle.IsChecked = false; PlayPauseToggle.Content = "Play"; }
        PreviewTitle.Text = "Select a component";
        PreviewContent.Text = string.Empty;
        PreviewScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle) return;

        if (!IsScrollableModeSelected)
        {
            toggle.IsChecked = false;
            toggle.Content = "Play";
            return;
        }

        if (toggle.IsChecked == true)
        {
            try
            {
                if (_presentation is null)
                {
                    StartPresentation(true);
                }
                else
                {
                    _presentation.StartScrolling();
                    _isMarqueePlaying = true;
                }

                toggle.Content = "Pause";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Unable to start presentation: {ex.Message}", "Church Presenter");
                toggle.IsChecked = false;
                toggle.Content = "Play";
            }
        }
        else
        {
            toggle.Content = "Play";
            _isMarqueePlaying = false;
            try { _presentation?.StopScrolling(); } catch { }
        }
    }

    private void ClearPreview_Click(object sender, RoutedEventArgs e)
    {
        PreviewTitle.Text = "Select a component";
        PreviewContent.Text = string.Empty;
        PreviewScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    private int GetSelectedScrollSpeed()
    {
        if (int.TryParse(ScrollSpeedDisplay.Text, out var speed))
            return Math.Clamp(speed, 1, 100);

        return Selected?.ScrollSpeed is int selectedSpeed
            ? Math.Clamp(selectedSpeed, 1, 100)
            : 2;
    }

    private bool IsScrollableModeSelected => DisplayModeBox.SelectedIndex == 1;

    private void ApplyMarqueeState(bool shouldScroll)
    {
        if (_presentation is null)
        {
            _isMarqueePlaying = false;
            return;
        }

        _isMarqueePlaying = shouldScroll;
        if (shouldScroll)
            _presentation.StartScrolling();
        else
            _presentation.StopScrolling();
    }

    private void UpdatePlaybackControls()
    {
        if (PlayPauseToggle is null)
            return;

        PlayPauseToggle.IsEnabled = IsScrollableModeSelected;
        PlayPauseToggle.IsChecked = IsScrollableModeSelected && _isMarqueePlaying;
        PlayPauseToggle.Content = _isMarqueePlaying ? "Pause" : "Play";
    }

    private void Presentation_Closed(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, _presentation))
            return;

        _presentation = null;
        _isMarqueePlaying = false;
        UpdatePresentationToggle();
        UpdatePlaybackControls();
    }

    private static void MoveToPresentationScreen(Window window)
    {
        var screens = Forms.Screen.AllScreens;
        var screen = screens.Length > 1 ? screens[1] : Forms.Screen.PrimaryScreen;
        if (screen is null) return;

        var bounds = screen.Bounds;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.WindowState = WindowState.Normal;
        window.Left = bounds.Left;
        window.Top = bounds.Top;
        window.Width = bounds.Width;
        window.Height = bounds.Height;
    }
}
