using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Church_Presenter;

public partial class PresentationWindow : Window
{
    private DispatcherTimer? _scrollTimer;
    private int _scrollSpeed = 2;
    private int _pauseCounter;
    private readonly int _pauseTicks = 40; // ~2s pause at 50ms interval
    private bool _isScrollable;

    public PresentationWindow(string title, string content, string background, string foreground, double fontSize, bool scrollable = false, int scrollSpeed = 2)
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
        _scrollSpeed = scrollSpeed;

        _isScrollable = scrollable;
        if (scrollable)
        {
            _scrollTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _scrollTimer.Tick += (_, _) =>
            {
                try
                {
                    if (ContentScroller.ScrollableHeight <= 0) return;

                    if (_pauseCounter > 0)
                    {
                        _pauseCounter--;
                        return;
                    }

                    var newOffset = ContentScroller.VerticalOffset - _scrollSpeed;
                    if (newOffset <= 0)
                    {
                        ContentScroller.ScrollToVerticalOffset(0);
                        _pauseCounter = _pauseTicks;
                    }
                    else
                    {
                        ContentScroller.ScrollToVerticalOffset(newOffset);
                    }
                }
                catch { }
            };

            // ensure initial bottom position is set after layout, but do not start the timer yet
            ContentScroller.Loaded += (_, _) =>
            {
                try
                {
                    if (ContentScroller.ScrollableHeight > 0)
                        ContentScroller.ScrollToVerticalOffset(ContentScroller.ScrollableHeight);
                }
                catch { }
            };
        }

        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); if (e.Key == Key.B) Stage.Background = Brushes.Black; };
        Closed += (_, _) => { try { _scrollTimer?.Stop(); } catch { } };
    }

    public void SetScrollSpeed(int speed)
    {
        if (speed < 1) speed = 1;
        _scrollSpeed = speed;
    }

    public void StartScrolling()
    {
        if (!_isScrollable || _scrollTimer is null) return;
        try
        {
            if (ContentScroller.ScrollableHeight > 0)
                ContentScroller.ScrollToVerticalOffset(ContentScroller.ScrollableHeight);
        }
        catch { }
        _pauseCounter = 0;
        _scrollTimer.Start();
    }

    public void StopScrolling()
    {
        try { _scrollTimer?.Stop(); } catch { }
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
