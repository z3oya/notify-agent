namespace NotifyAgent;

internal sealed class MainForm : Form
{
    private readonly NotifyIcon _notifyIcon;

    public MainForm()
    {
        Text = "notify-agent";
        ClientSize = new Size(420, 300);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "notify-agent",
            Visible = true,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("显示", null, (_, _) => ShowWindow()));
        menu.Items.Add(new ToolStripMenuItem("退出", null, (_, _) =>
        {
            _notifyIcon.Dispose();
            Application.Exit();
        }));
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => ShowWindow();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _notifyIcon.Dispose();
        base.Dispose(disposing);
    }

    private void ShowWindow()
    {
        WindowState = FormWindowState.Normal;
        Show();
        Activate();
    }
}
