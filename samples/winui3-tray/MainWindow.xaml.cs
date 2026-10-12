using System.Runtime.InteropServices;
using System.Windows.Input;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace NotifyAgent.WinUI3;

public sealed partial class MainWindow : Window
{
    private bool _exitRequested;

    private const int GWLP_WNDPROC = -4;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x80;
    private const uint WM_ACTIVATE = 0x0006;
    private const int SW_SHOWNA = 8;

    private delegate IntPtr WndProcCallback(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private readonly WndProcCallback _wndProcCallback;
    private IntPtr _previousWndProc;
    private IntPtr _hwnd;
    private readonly DispatcherQueueTimer _deactivateTimer =
        DispatcherQueue.GetForCurrentThread().CreateTimer();

    private FloatingBar? _floatingBar;

    private readonly MenuFlyout _trayMenu = new();
    private readonly MenuFlyoutItem _showItem = new() { Text = "显示" };
    private readonly MenuFlyoutItem _hideItem = new() { Text = "隐藏" };
    private readonly MenuFlyoutItem _exitItem = new() { Text = "退出" };

    public MainWindow()
    {
        InitializeComponent();
        Title = "notify-agent";

        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.SetBorderAndTitleBar(true, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        SystemBackdrop = new MicaBackdrop();

        AppWindow.Resize(new SizeInt32(420, 500));
        PlaceAtBottomRight();

        BuildMenus();

        TrayIcon.LeftClickCommand = new DelegateCommand(ToggleWindow);

        _deactivateTimer.Interval = TimeSpan.FromMilliseconds(120);
        _deactivateTimer.IsRepeating = false;
        _deactivateTimer.Tick += (_, _) =>
        {
            var foreground = GetForegroundWindow();
            var foregroundThread = foreground != IntPtr.Zero
                ? GetWindowThreadProcessId(foreground, out _)
                : 0;
            if (foregroundThread == GetCurrentThreadId())
                return; // focus is on one of our own popups (e.g. the open tray menu)
            HidePanel();
        };

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        _hwnd = hwnd;
        var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exStyle | WS_EX_TOOLWINDOW));
        _wndProcCallback = WndProc;
        _previousWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcCallback));

        AppWindow.Changed += (_, args) =>
        {
            if (args.DidVisibilityChange)
                UpdateTrayMenuState();
        };
        AppWindow.Closing += (_, args) =>
        {
            if (!_exitRequested)
            {
                args.Cancel = true;
                AppWindow.Hide();
            }
        };
    }

    private void BuildMenus()
    {
        _showItem.Command = new DelegateCommand(ShowFromTray);
        _hideItem.Command = new DelegateCommand(HidePanel);
        _exitItem.Command = new DelegateCommand(ExitApplication);
        _trayMenu.Items.Add(_showItem);
        _trayMenu.Items.Add(_hideItem);
        _trayMenu.Items.Add(_exitItem);

        TrayIcon.ContextFlyout = _trayMenu;
        UpdateTrayMenuState();
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_ACTIVATE && (wParam.ToInt64() & 0xFFFF) == 0)
        {
            _deactivateTimer.Stop();
            _deactivateTimer.Start();
        }
        return CallWindowProc(_previousWndProc, hWnd, msg, wParam, lParam);
    }

    [DllImport("user32.dll", EntryPoint = "ShowWindow")]
    private static extern bool ShowWindowNative(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private void PlaceAtBottomRight()
    {
        const int margin = 12;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        AppWindow.Move(new PointInt32(
            area.WorkArea.X + area.WorkArea.Width - AppWindow.Size.Width - margin,
            area.WorkArea.Y + area.WorkArea.Height - AppWindow.Size.Height - margin));
    }

    private void ToggleWindow()
    {
        _deactivateTimer.Stop();
        if (AppWindow.IsVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
        UpdateTrayMenuState();
    }

    private void BringToFront()
    {
        var foreground = GetForegroundWindow();
        var currentThread = GetCurrentThreadId();
        var foregroundThread = foreground != IntPtr.Zero
            ? GetWindowThreadProcessId(foreground, out _)
            : 0;
        if (foregroundThread != 0 && foregroundThread != currentThread)
            AttachThreadInput(currentThread, foregroundThread, true);
        SetForegroundWindow(_hwnd);
        if (foregroundThread != 0 && foregroundThread != currentThread)
            AttachThreadInput(currentThread, foregroundThread, false);
    }

    private void UpdateTrayMenuState()
    {
        var visible = AppWindow.IsVisible;
        _showItem.IsEnabled = !visible;
        _hideItem.IsEnabled = visible;
    }

    private void ShowFromTray()
    {
        ShowPanel();
        UpdateTrayMenuState();
    }

    internal void ShowPanel()
    {
        ShowWindowNative(_hwnd, SW_SHOWNA);
        BringToFront();
        Activate();
        Content?.Focus(FocusState.Programmatic);
        PlayShowAnimation();
    }

    private void PlayShowAnimation()
    {
        var fade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(170)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(fade, RootGrid);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var slide = new DoubleAnimation
        {
            From = 24,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(170)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(slide, RootTranslate);
        Storyboard.SetTargetProperty(slide, "Y");

        var storyboard = new Storyboard();
        storyboard.Children.Add(fade);
        storyboard.Children.Add(slide);
        storyboard.Begin();
    }

    private bool _hiding;

    private void HidePanel()
    {
        if (_hiding || !AppWindow.IsVisible)
            return;

        _hiding = true;
        var fade = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
        };
        Storyboard.SetTarget(fade, RootGrid);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var slide = new DoubleAnimation
        {
            From = 0,
            To = 16,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
        };
        Storyboard.SetTarget(slide, RootTranslate);
        Storyboard.SetTargetProperty(slide, "Y");

        var storyboard = new Storyboard();
        storyboard.Children.Add(fade);
        storyboard.Children.Add(slide);
        storyboard.Completed += (_, _) =>
        {
            _hiding = false;
            AppWindow.Hide();
            UpdateTrayMenuState();
        };
        storyboard.Begin();
    }

    private void OnFloatingToggled(object sender, RoutedEventArgs e)
    {
        if (((ToggleSwitch)sender).IsOn)
        {
            _floatingBar ??= new FloatingBar();
            _floatingBar.CloseRequested -= OnFloatingCloseRequested;
            _floatingBar.CloseRequested += OnFloatingCloseRequested;
            _floatingBar.ShowFloating();
        }
        else
        {
            _floatingBar?.AppWindow.Hide();
        }
    }

    private void OnFloatingCloseRequested()
    {
        FloatingToggle.IsOn = false;
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        TrayIcon.Dispose();
        Close();
        Environment.Exit(0);
    }

    private sealed class DelegateCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
}
