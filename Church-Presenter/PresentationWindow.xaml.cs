using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace Church_Presenter;
public partial class PresentationWindow : Window
{
    public PresentationWindow(string title, string content, string background, string foreground, double fontSize, bool scrollable = false)
    {
        InitializeComponent();
        Stage.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(background));
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(foreground));
        TitleText.Foreground = ContentText.Foreground = brush;
        TitleText.Text = title;
        ContentText.Text = content;
        TitleText.FontSize = fontSize * 1.15;
        ContentText.FontSize = fontSize;
        ContentScroller.VerticalScrollBarVisibility = scrollable ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close();
            if (e.Key == Key.B) Stage.Background = Brushes.Black;
        };
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
