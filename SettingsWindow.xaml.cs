using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace NotifyAgent;

public sealed partial class SettingsWindow : Window
{
    // 逻辑尺寸(DIP),按窗口 DPI 换算为物理像素,保证各缩放下视觉一致
    private const int LogicalWidth = 800;
    private const int LogicalHeight = 480;

    private readonly IntPtr _hwnd;

    public SettingsWindow()
    {
        InitializeComponent();
        Title = "设置";
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));

        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        SystemBackdrop = new MicaBackdrop();

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var scale = GetDpiForWindow(_hwnd) / 96.0;
        AppWindow.Resize(new SizeInt32(
            (int)Math.Round(LogicalWidth * scale),
            (int)Math.Round(LogicalHeight * scale)));
        PlaceAtCenter();
    }

    private void PlaceAtCenter()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        AppWindow.Move(new PointInt32(
            area.WorkArea.X + (area.WorkArea.Width - AppWindow.Size.Width) / 2,
            area.WorkArea.Y + (area.WorkArea.Height - AppWindow.Size.Height) / 2));
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);
}
