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
    private bool _scrollPending;

    public PresentationWindow(string title, string content, string background, string foreground, double fontSize, bool scrollable = false, int scrollSpeed = 2)
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowState = WindowState.Maximized;
        EnsureScrollTimer();
        UpdatePresentation(title, content, background, foreground, fontSize, scrollable, scrollSpeed);

        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); if (e.Key == Key.B) Stage.Background = Brushes.Black; };
        Closed += (_, _) => { try { _scrollTimer?.Stop(); } catch { } };
    }

    public void UpdatePresentation(string title, string content, string background, string foreground, double fontSize, bool scrollable, int scrollSpeed)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "Church Presenter" : title;
        Stage.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
        ContentText.Foreground = new SolidColorBrush(GetReadableForeground(foreground));
        ContentText.Text = content;
        ContentText.FontSize = fontSize;

        _isScrollable = scrollable;
        _scrollPending = false;
        SetScrollSpeed(scrollSpeed);
        StopScrolling();

        ContentHost.VerticalAlignment = scrollable ? VerticalAlignment.Top : VerticalAlignment.Center;
        ContentScroller.VerticalScrollBarVisibility = scrollable ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled;
        UpdateMarqueeSpacing();
        ResetScrollPosition();
    }

    public void SetScrollSpeed(int speed)
    {
        _scrollSpeed = Math.Clamp(speed, 1, 100);
    }

    public void StartScrolling()
    {
        if (!_isScrollable || _scrollTimer is null) return;

        _scrollPending = true;
        UpdateMarqueeSpacing();
        ResetScrollPosition();
        _pauseCounter = 0;
        Dispatcher.BeginInvoke(() =>
        {
            if (!_isScrollable || _scrollTimer is null)
                return;

            _scrollPending = false;
            _scrollTimer.Start();
        }, DispatcherPriority.Loaded);
    }

    public void StopScrolling()
    {
        _scrollPending = false;
        try { _scrollTimer?.Stop(); } catch { }
    }

    private void EnsureScrollTimer()
    {
        if (_scrollTimer is not null)
            return;

        _scrollTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _scrollTimer.Tick += (_, _) => AdvanceScroll();
        ContentScroller.Loaded += (_, _) =>
        {
            UpdateMarqueeSpacing();
            ResetScrollPosition();
        };
        ContentScroller.SizeChanged += (_, _) => UpdateMarqueeSpacing();
    }

    private void AdvanceScroll()
    {
        try
        {
            if (!_isScrollable || ContentScroller.ScrollableHeight <= 0)
                return;

            if (_pauseCounter > 0)
            {
                _pauseCounter--;
                return;
            }

            var newOffset = ContentScroller.VerticalOffset + _scrollSpeed;
            if (newOffset >= ContentScroller.ScrollableHeight)
            {
                ContentScroller.ScrollToVerticalOffset(ContentScroller.ScrollableHeight);
                StopScrolling();
                return;
            }
            else
            {
                ContentScroller.ScrollToVerticalOffset(newOffset);
            }
        }
        catch { }
    }

    private void ResetScrollPosition()
    {
        try
        {
            ContentScroller.ScrollToVerticalOffset(0);
        }
        catch { }
    }

    private void UpdateMarqueeSpacing()
    {
        var spacerHeight = _isScrollable
            ? Math.Max(ContentScroller.ViewportHeight, ActualHeight) * 0.5
            : 0;

        TopSpacer.Height = spacerHeight;
        BottomSpacer.Height = spacerHeight;

        if (_scrollPending)
            Dispatcher.BeginInvoke(() => ResetScrollPosition(), DispatcherPriority.Loaded);
    }

    private static Color GetReadableForeground(string foreground)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(foreground);
            var brightness = ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255d;
            return brightness > 0.72
                ? (Color)ColorConverter.ConvertFromString("#1F2937")
                : color;
        }
        catch
        {
            return (Color)ColorConverter.ConvertFromString("#1F2937");
        }
    }

}
