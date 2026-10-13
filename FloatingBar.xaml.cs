using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace NotifyAgent;

public sealed partial class FloatingBar : Window
{
    public event Action? CloseRequested;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x80;
    private const int WS_EX_NOACTIVATE = unchecked((int)0x80000000);
    private const int SW_SHOWNA = 8;

    private IntPtr _hwnd;

    public FloatingBar()
    {
        InitializeComponent();
        Title = "notify-agent";

        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.SetBorderAndTitleBar(true, false);
        presenter.IsAlwaysOnTop = true;
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        SystemBackdrop = new MicaBackdrop();

        AppWindow.Resize(new SizeInt32(280, 64));

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        _hwnd = hwnd;
        var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE));
    }

    internal void ShowFloating()
    {
        PlaceTopRight();
        ShowWindowNative(_hwnd, SW_SHOWNA);
    }

    private void PlaceTopRight()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        AppWindow.Move(new PointInt32(
            area.WorkArea.X + area.WorkArea.Width - AppWindow.Size.Width - 12,
            area.WorkArea.Y + 12));
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();

    [DllImport("user32.dll", EntryPoint = "ShowWindow")]
    private static extern bool ShowWindowNative(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}
