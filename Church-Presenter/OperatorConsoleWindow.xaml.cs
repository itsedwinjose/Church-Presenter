using System.Windows;
using System.Windows.Controls;
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
        InitializeComponent(); _database=database; _components=components; ComponentList.ItemsSource=components; if(components.Count>0) ComponentList.SelectedIndex=0;
        ScreenStatus.Text = Forms.Screen.AllScreens.Length > 1 ? "Second display detected — HDMI output will use it." : "No second display detected — output will use this screen.";
    }
    private PlannerComponent? Selected => ComponentList.SelectedItem as PlannerComponent;
    private void Component_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (Selected is null) return; PreviewTitle.Text=Selected.Title;PreviewContent.Text=Selected.Content;DisplayModeBox.SelectedIndex=Selected.PresentationMode == "Scrollable" ? 1 : 0; ApplyPreviewMode();
    }
    private void DisplayMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Selected is null || DisplayModeBox.SelectedItem is not ComboBoxItem mode) return; _database.SetPlannerPresentationMode(Selected.Id, mode.Content?.ToString() ?? "Static"); ApplyPreviewMode();
    }
    private void ApplyPreviewMode() { PreviewScroller.VerticalScrollBarVisibility = DisplayModeBox.SelectedIndex == 1 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled; }
    private void Play_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null) { MessageBox.Show("Select a service item first.", "Church Presenter"); return; }
        _presentation?.Close(); var background=_database.Get("BackgroundColor", "#101828");var foreground=_database.Get("FontColor", "#FFFFFF");var size=double.Parse(_database.Get("FontSize","48"),System.Globalization.CultureInfo.InvariantCulture);
        _presentation=new PresentationWindow(Selected.Title,Selected.Content,background,foreground,size,Selected.PresentationMode=="Scrollable"); MoveToPresentationScreen(_presentation); _presentation.Show();
    }
    private static void MoveToPresentationScreen(Window window)
    {
        var screen=Forms.Screen.AllScreens.Length>1 ? Forms.Screen.AllScreens[1] : Forms.Screen.PrimaryScreen; if(screen is null)return; var bounds=screen.Bounds; window.WindowStartupLocation=WindowStartupLocation.Manual;window.Left=bounds.Left;window.Top=bounds.Top;window.Width=bounds.Width;window.Height=bounds.Height;window.WindowState=WindowState.Maximized;
    }
}
