namespace NotifyAgent;

internal sealed class MainForm : Form
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _leftMenu;
    private readonly ContextMenuStrip _trayMenu;

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

        _leftMenu = BuildLeftMenu();
        _trayMenu = BuildTrayMenu();

        _notifyIcon.ContextMenuStrip = _trayMenu;
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                _leftMenu.Show(Cursor.Position);
        };
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
        {
            _notifyIcon.Dispose();
            _leftMenu.Dispose();
            _trayMenu.Dispose();
        }
        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildLeftMenu()
    {
        var menu = new ContextMenuStrip();
        foreach (var label in new[] { "示例 A", "示例 B", "示例 C" })
            menu.Items.Add(new ToolStripMenuItem(label));
        return menu;
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        var showItem = new ToolStripMenuItem("显示", null, (_, _) => ShowWindow());
        var hideItem = new ToolStripMenuItem("隐藏", null, (_, _) => Hide());
        menu.Items.Add(showItem);
        menu.Items.Add(hideItem);
        menu.Items.Add(new ToolStripMenuItem("退出", null, (_, _) => Application.Exit()));
        menu.Opening += (_, _) =>
        {
            showItem.Enabled = !Visible;
            hideItem.Enabled = Visible;
        };
        return menu;
    }

    private void ShowWindow()
    {
        WindowState = FormWindowState.Normal;
        Show();
        Activate();
    }
}
