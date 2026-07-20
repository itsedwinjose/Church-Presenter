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

    public PresentationWindow(string _, string content, string background, string foreground, double fontSize, bool scrollable = false, int scrollSpeed = 2)
    {
        InitializeComponent();
        Stage.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(background));
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(foreground));
        ContentText.Foreground = brush;
        ContentText.Text = content;
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

                    var newOffset = ContentScroller.VerticalOffset + _scrollSpeed;
                    if (newOffset >= ContentScroller.ScrollableHeight)
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

            // Start at the top so the content scrolls upward through the display.
            ContentScroller.Loaded += (_, _) =>
            {
                try
                {
                    ContentScroller.ScrollToVerticalOffset(0);
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
            ContentScroller.ScrollToVerticalOffset(0);
        }
        catch { }
        _pauseCounter = 0;
        _scrollTimer.Start();
    }

    public void StopScrolling()
    {
        try { _scrollTimer?.Stop(); } catch { }
    }

}
