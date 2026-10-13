using System.Threading;
using Microsoft.UI.Xaml;

namespace NotifyAgent;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\NotifyAgent.SingleInstance";
    private const string ShowPanelEventName = @"Local\NotifyAgent.ShowPanel";

    private static Mutex? _singleInstanceMutex;

    public App()
    {
        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            new EventWaitHandle(false, EventResetMode.AutoReset, ShowPanelEventName).Set();
            Environment.Exit(0);
        }
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
        window.Activate();

        var signal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowPanelEventName);
        var listener = new Thread(() =>
        {
            while (signal.WaitOne())
                window.DispatcherQueue.TryEnqueue(window.ShowPanel);
        })
        { IsBackground = true };
        listener.Start();
    }
}
