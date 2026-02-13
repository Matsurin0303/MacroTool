using System.ComponentModel;

namespace MacroTool.WinForms.Settings;

public sealed class SettingsForm : Form
{
    private readonly SettingsStore _store;
    private readonly AppSettings _settings;

    private readonly ListBox _nav = new();
    private readonly Panel _content = new();
    private readonly Button _btnOk = new();
    private readonly Button _btnCancel = new();
    private readonly Button _btnApply = new();

    private readonly PlaybackSettingsPage _pagePlayback;
    private readonly UiSettingsPage _pageUi;

    public AppSettings Result => _settings;

    public SettingsForm(SettingsStore store)
    {
        _store = store;
        _settings = _store.Load();

        Text = "Settings";
        StartPosition = FormStartPosition.CenterParent;
        Width = 820;
        Height = 520;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.Sizable;

        _nav.Dock = DockStyle.Left;
        _nav.Width = 200;
        _nav.IntegralHeight = false;

        _content.Dock = DockStyle.Fill;

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 48 };
        _btnOk.Text = "OK";
        _btnCancel.Text = "Cancel";
        _btnApply.Text = "Apply";

        _btnOk.Width = 100;
        _btnCancel.Width = 100;
        _btnApply.Width = 100;

        _btnCancel.DialogResult = DialogResult.Cancel;

        _btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _btnApply.Anchor = AnchorStyles.Right | AnchorStyles.Top;

        _btnApply.Left = bottom.Width - 110;
        bottom.Resize += (_, __) =>
        {
            _btnApply.Left = bottom.Width - 110;
            _btnCancel.Left = bottom.Width - 220;
            _btnOk.Left = bottom.Width - 330;

            _btnApply.Top = 10;
            _btnCancel.Top = 10;
            _btnOk.Top = 10;
        };

        bottom.Controls.AddRange(new Control[] { _btnApply, _btnCancel, _btnOk });

        Controls.AddRange(new Control[] { _content, _nav, bottom });

        // pages
        _pagePlayback = new PlaybackSettingsPage(_settings.Playback);
        _pageUi = new UiSettingsPage(_settings.Ui);

        _nav.Items.AddRange(new object[]
        {
            "Playback",
            "User Interface",
            "Recording (coming soon)",
            "Hotkeys (coming soon)"
        });

        _nav.SelectedIndexChanged += (_, __) => ShowSelectedPage();
        _nav.SelectedIndex = 0;

        _btnApply.Click += (_, __) => Apply();
        _btnOk.Click += (_, __) =>
        {
            Apply();
            DialogResult = DialogResult.OK;
            Close();
        };
    }

    private void ShowSelectedPage()
    {
        _content.Controls.Clear();

        var key = _nav.SelectedItem?.ToString() ?? "";
        Control page = key switch
        {
            "Playback" => _pagePlayback,
            "User Interface" => _pageUi,
            _ => new Label
            {
                Text = "Not implemented yet.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            }
        };

        page.Dock = DockStyle.Fill;
        _content.Controls.Add(page);
    }

    private void Apply()
    {
        // pages -> settings object
        _pagePlayback.ApplyTo(_settings.Playback);
        _pageUi.ApplyTo(_settings.Ui);

        _store.Save(_settings);
    }
}
