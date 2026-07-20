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
    public OperatorConsoleWindow(AppDatabase database, IReadOnlyList<PlannerComponent> components)
    {
        InitializeComponent();
        _database = database;
        _components = components;
        ComponentList.ItemsSource = components;
        if (components.Count > 0) ComponentList.SelectedIndex = 0;
        ScreenStatus.Text = Forms.Screen.AllScreens.Length > 1 ? "Second display detected — HDMI output will use it." : "No second display detected — output will use this screen.";
        // show configured scroll speed for the initially selected item (if any)
        if (ComponentList.SelectedItem is PlannerComponent pc) ScrollSpeedDisplay.Text = pc.ScrollSpeed.ToString();
        Closed += (_, _) => ClosePresentation();
    }
    private PlannerComponent? Selected => ComponentList.SelectedItem as PlannerComponent;
    private void Component_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (Selected is null) return;
        PreviewTitle.Text = Selected.Title;
        PreviewContent.Text = Selected.Content;
        DisplayModeBox.SelectedIndex = Selected.PresentationMode == "Scrollable" ? 1 : 0;
        // show saved per-item scroll speed
        ScrollSpeedDisplay.Text = Selected.ScrollSpeed.ToString();
        ApplyPreviewMode();
        if (_presentation is not null) StartPresentation();
    }
    private void DisplayMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Selected is null || DisplayModeBox.SelectedItem is not ComboBoxItem mode) return; _database.SetPlannerPresentationMode(Selected.Id, mode.Content?.ToString() ?? "Static"); ApplyPreviewMode();
    }
    private void ApplyPreviewMode() { PreviewScroller.VerticalScrollBarVisibility = DisplayModeBox.SelectedIndex == 1 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled; }
    private void Play_Click(object sender, RoutedEventArgs e)
    {
        StartPresentation();
    }

    private void StartPresentation()
    {
        if (Selected is null) { MessageBox.Show("Select a service item first.", "Church Presenter"); return; }
        _presentation?.Close();
        var background = _database.Get("BackgroundColor", "#FFFFFF");
        var (foreground, size) = _database.GetPresentationStyle(Selected.Type);
        var speed = Selected.ScrollSpeed;
        _presentation = new PresentationWindow(Selected.Title, Selected.Content, background, foreground, size, Selected.PresentationMode == "Scrollable", speed);
        MoveToPresentationScreen(_presentation);
        _presentation.Show();
        Selected.IsCompleted = true;
        ComponentList.Items.Refresh();
        UpdatePresentationToggle();
        // start marquee only when Play is clicked from console
        if (Selected.PresentationMode == "Scrollable") _presentation.StartScrolling();
    }

    private void CloseConsole_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ClosePresentation()
    {
        try { _presentation?.StopScrolling(); _presentation?.Close(); } catch { }
        _presentation = null;
        UpdatePresentationToggle();
    }

    private void TogglePresentation_Click(object sender, RoutedEventArgs e)
    {
        if (_presentation is null) StartPresentation();
        else ClosePresentation();
    }

    private void UpdatePresentationToggle()
    {
        if (PresentationToggleButton is not null)
            PresentationToggleButton.Content = _presentation is null ? "Open presenter" : "Close presenter";
    }

    private void IncreaseSpeed_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null) { MessageBox.Show("Select a service item first.", "Church Presenter"); return; }
        var current = int.Parse(ScrollSpeedDisplay.Text ?? _database.Get("ScrollSpeed", "2"));
        current++;
        if (current > 100) current = 100;
        _database.SetPlannerComponentScrollSpeed(Selected.Id, current);
        ScrollSpeedDisplay.Text = current.ToString();
        if (_presentation is not null) _presentation.SetScrollSpeed(current);
    }

    private void DecreaseSpeed_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null) { MessageBox.Show("Select a service item first.", "Church Presenter"); return; }
        var current = int.Parse(ScrollSpeedDisplay.Text ?? _database.Get("ScrollSpeed", "2"));
        current--;
        if (current < 1) current = 1;
        _database.SetPlannerComponentScrollSpeed(Selected.Id, current);
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

        if (toggle.IsChecked == true)
        {
            // Start or resume
            try
            {
                // If there's no presentation yet, create and show one
                if (_presentation is null)
                {
                    Play_Click(this, e);
                }
                else
                {
                    // resume scrolling
                    _presentation.StartScrolling();
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
            // Pause
            toggle.Content = "Play";
            try { _presentation?.StopScrolling(); } catch { }
        }
    }

    private void ClearPreview_Click(object sender, RoutedEventArgs e)
    {
        PreviewTitle.Text = "Select a component";
        PreviewContent.Text = string.Empty;
        PreviewScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
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
