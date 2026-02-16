using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MacroTool.WinForms.Dialogs;

public partial class FindTextOcrDialog
{
    private IContainer? components = null;

    // code-behind 側でリソース解放を行うためのフック（FindTextOcrDialog.cs 側に実装）
    partial void DisposeManagedResources();

    // ===== Containers =====
    private GroupBox grpSpec;
    private TableLayoutPanel tblSpec;
    private TableLayoutPanel tblRight;
    private FlowLayoutPanel pnlAreaBtns;

    private GroupBox grpFound;
    private TableLayoutPanel tblFound;

    private GroupBox grpNotFound;
    private TableLayoutPanel tblNotFound;

    private FlowLayoutPanel pnlBottom;

    // ===== Labels =====
    private Label lblLang;
    private Label lblArea;
    private Label lblAndY;
    private Label lblGoToFound;
    private Label lblContinueWaiting;
    private Label lblSecondsAndThen;
    private Label lblGoToNotFound;

    // ===== UI controls (code-behind から参照するためフィールド化) =====
    private TextBox _txtText;
    private ComboBox _cmbLang;

    private ComboBox _cmbArea;
    private Button _btnDefineArea;
    private Button _btnConfirmArea;
    private Button _btnTest;

    private CheckBox _chkMouseAction;
    private ComboBox _cmbMouseAction;
    private ComboBox _cmbMousePos;

    private CheckBox _chkSaveCoord;
    private TextBox _txtSaveX;
    private TextBox _txtSaveY;

    private ComboBox _cmbTrueGoTo;
    private NumericUpDown _numTimeoutSec;
    private ComboBox _cmbFalseGoTo;

    private Button _btnOk;
    private Button _btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            DisposeManagedResources();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();

        // --- form ---
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Text = "Find text (OCR)";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(716, 599);
        MinimumSize = new Size(716, 599);

        SuspendLayout();

        // ===== grpSpec =====
        grpSpec = new GroupBox();
        grpSpec.Name = "grpSpec";
        grpSpec.Text = "Text to search";
        grpSpec.Dock = DockStyle.Top;
        grpSpec.Height = 185;
        grpSpec.Padding = new Padding(10);

        tblSpec = new TableLayoutPanel();
        tblSpec.Name = "tblSpec";
        tblSpec.Dock = DockStyle.Fill;
        tblSpec.ColumnCount = 2;
        tblSpec.RowCount = 1;
        tblSpec.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
        tblSpec.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblSpec.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _txtText = new TextBox();
        _txtText.Name = "_txtText";
        _txtText.Multiline = true;
        _txtText.ScrollBars = ScrollBars.Vertical;
        _txtText.Dock = DockStyle.Fill;

        tblRight = new TableLayoutPanel();
        tblRight.Name = "tblRight";
        tblRight.Dock = DockStyle.Fill;
        tblRight.ColumnCount = 3;
        tblRight.RowCount = 3;
        tblRight.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        tblRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblRight.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

        lblLang = new Label();
        lblLang.Name = "lblLang";
        lblLang.Text = "Language:";
        lblLang.AutoSize = true;
        lblLang.Margin = new Padding(0, 9, 0, 0);

        _cmbLang = new ComboBox();
        _cmbLang.Name = "_cmbLang";
        _cmbLang.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbLang.Items.AddRange(new object[] { "English", "Japanese" });
        _cmbLang.SelectedIndex = 0;
        _cmbLang.Width = 180;
        _cmbLang.Anchor = AnchorStyles.Left | AnchorStyles.Top;

        lblArea = new Label();
        lblArea.Name = "lblArea";
        lblArea.Text = "Search area:";
        lblArea.AutoSize = true;
        lblArea.Margin = new Padding(0, 9, 0, 0);

        _cmbArea = new ComboBox();
        _cmbArea.Name = "_cmbArea";
        _cmbArea.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbArea.Items.AddRange(new object[] { "Entire desktop", "Focused window", "Area of desktop", "Area of focused window" });
        _cmbArea.SelectedIndex = 0;
        _cmbArea.Width = 240;
        _cmbArea.Margin = new Padding(0, 6, 0, 0);
        _cmbArea.Anchor = AnchorStyles.Left | AnchorStyles.Top;

        _btnDefineArea = new Button();
        _btnDefineArea.Name = "_btnDefineArea";
        _btnDefineArea.Text = "Define...";
        _btnDefineArea.Width = 110;
        _btnDefineArea.Height = 26;
        _btnDefineArea.Enabled = false;

        _btnConfirmArea = new Button();
        _btnConfirmArea.Name = "_btnConfirmArea";
        _btnConfirmArea.Text = "Confirm Area";
        _btnConfirmArea.Width = 110;
        _btnConfirmArea.Height = 26;
        _btnConfirmArea.Enabled = false;

        _btnTest = new Button();
        _btnTest.Name = "_btnTest";
        _btnTest.Text = "Test";
        _btnTest.Width = 110;
        _btnTest.Height = 26;

        pnlAreaBtns = new FlowLayoutPanel();
        pnlAreaBtns.Name = "pnlAreaBtns";
        pnlAreaBtns.Dock = DockStyle.Fill;
        pnlAreaBtns.FlowDirection = FlowDirection.TopDown;
        pnlAreaBtns.WrapContents = false;
        pnlAreaBtns.Margin = new Padding(0, 4, 0, 0);
        pnlAreaBtns.Controls.Add(_btnDefineArea);
        pnlAreaBtns.Controls.Add(_btnConfirmArea);

        // right add
        tblRight.Controls.Add(lblLang, 0, 0);
        tblRight.Controls.Add(_cmbLang, 1, 0);
        tblRight.SetColumnSpan(_cmbLang, 2);

        tblRight.Controls.Add(lblArea, 0, 1);
        tblRight.Controls.Add(_cmbArea, 1, 1);
        tblRight.Controls.Add(pnlAreaBtns, 2, 1);

        tblRight.Controls.Add(_btnTest, 2, 2);

        // spec add
        tblSpec.Controls.Add(_txtText, 0, 0);
        tblSpec.Controls.Add(tblRight, 1, 0);
        grpSpec.Controls.Add(tblSpec);

        // ===== grpFound =====
        grpFound = new GroupBox();
        grpFound.Name = "grpFound";
        grpFound.Text = "If text is found";
        grpFound.Dock = DockStyle.Top;
        grpFound.Height = 150;
        grpFound.Padding = new Padding(10);

        tblFound = new TableLayoutPanel();
        tblFound.Name = "tblFound";
        tblFound.Dock = DockStyle.Fill;
        tblFound.ColumnCount = 4;
        tblFound.RowCount = 3;
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

        _chkMouseAction = new CheckBox();
        _chkMouseAction.Name = "_chkMouseAction";
        _chkMouseAction.Text = "Mouse action:";
        _chkMouseAction.AutoSize = true;
        _chkMouseAction.Checked = true;

        _cmbMouseAction = new ComboBox();
        _cmbMouseAction.Name = "_cmbMouseAction";
        _cmbMouseAction.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbMouseAction.Items.AddRange(new object[] { "Positioning", "LeftClick", "RightClick", "MiddleClick", "DoubleClick" });
        _cmbMouseAction.SelectedIndex = 0;
        _cmbMouseAction.Width = 130;

        _cmbMousePos = new ComboBox();
        _cmbMousePos.Name = "_cmbMousePos";
        _cmbMousePos.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbMousePos.Items.AddRange(new object[] { "Centered", "Top-left", "Top-right", "Bottom-left", "Bottom-right" });
        _cmbMousePos.SelectedIndex = 0;
        _cmbMousePos.Width = 130;

        _chkSaveCoord = new CheckBox();
        _chkSaveCoord.Name = "_chkSaveCoord";
        _chkSaveCoord.Text = "Save X to:";
        _chkSaveCoord.AutoSize = true;
        _chkSaveCoord.Checked = false;

        _txtSaveX = new TextBox();
        _txtSaveX.Name = "_txtSaveX";
        _txtSaveX.Width = 120;
        _txtSaveX.Text = "X";
        _txtSaveX.Enabled = false;

        _txtSaveY = new TextBox();
        _txtSaveY.Name = "_txtSaveY";
        _txtSaveY.Width = 120;
        _txtSaveY.Text = "Y";
        _txtSaveY.Enabled = false;

        lblAndY = new Label();
        lblAndY.Name = "lblAndY";
        lblAndY.Text = "and Y to:";
        lblAndY.AutoSize = true;
        lblAndY.Margin = new Padding(0, 8, 0, 0);

        lblGoToFound = new Label();
        lblGoToFound.Name = "lblGoToFound";
        lblGoToFound.Text = "Go to";
        lblGoToFound.AutoSize = true;
        lblGoToFound.Margin = new Padding(0, 8, 0, 0);

        _cmbTrueGoTo = new ComboBox();
        _cmbTrueGoTo.Name = "_cmbTrueGoTo";
        _cmbTrueGoTo.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbTrueGoTo.Items.AddRange(new object[] { "Next", "End", "Label..." });
        _cmbTrueGoTo.SelectedIndex = 0;
        _cmbTrueGoTo.Width = 240;

        tblFound.Controls.Add(_chkMouseAction, 0, 0);
        tblFound.Controls.Add(_cmbMouseAction, 1, 0);
        tblFound.Controls.Add(_cmbMousePos, 2, 0);
        tblFound.SetColumnSpan(_cmbMousePos, 2);

        tblFound.Controls.Add(_chkSaveCoord, 0, 1);
        tblFound.Controls.Add(_txtSaveX, 1, 1);
        tblFound.Controls.Add(lblAndY, 2, 1);
        tblFound.Controls.Add(_txtSaveY, 3, 1);

        tblFound.Controls.Add(lblGoToFound, 0, 2);
        tblFound.Controls.Add(_cmbTrueGoTo, 1, 2);
        tblFound.SetColumnSpan(_cmbTrueGoTo, 3);

        grpFound.Controls.Add(tblFound);

        // ===== grpNotFound =====
        grpNotFound = new GroupBox();
        grpNotFound.Name = "grpNotFound";
        grpNotFound.Text = "If text is not found";
        grpNotFound.Dock = DockStyle.Top;
        grpNotFound.Height = 125;
        grpNotFound.Padding = new Padding(10);

        tblNotFound = new TableLayoutPanel();
        tblNotFound.Name = "tblNotFound";
        tblNotFound.Dock = DockStyle.Fill;
        tblNotFound.ColumnCount = 4;
        tblNotFound.RowCount = 2;
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblNotFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblNotFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

        lblContinueWaiting = new Label();
        lblContinueWaiting.Name = "lblContinueWaiting";
        lblContinueWaiting.Text = "Continue waiting";
        lblContinueWaiting.AutoSize = true;
        lblContinueWaiting.Margin = new Padding(0, 8, 0, 0);

        _numTimeoutSec = new NumericUpDown();
        _numTimeoutSec.Name = "_numTimeoutSec";
        ((ISupportInitialize)_numTimeoutSec).BeginInit();
        _numTimeoutSec.Minimum = 0;
        _numTimeoutSec.Maximum = 86400;
        _numTimeoutSec.Value = 120;
        _numTimeoutSec.Width = 80;

        lblSecondsAndThen = new Label();
        lblSecondsAndThen.Name = "lblSecondsAndThen";
        lblSecondsAndThen.Text = "seconds and then";
        lblSecondsAndThen.AutoSize = true;
        lblSecondsAndThen.Margin = new Padding(0, 8, 0, 0);

        lblGoToNotFound = new Label();
        lblGoToNotFound.Name = "lblGoToNotFound";
        lblGoToNotFound.Text = "Go to";
        lblGoToNotFound.AutoSize = true;
        lblGoToNotFound.Margin = new Padding(0, 8, 0, 0);

        _cmbFalseGoTo = new ComboBox();
        _cmbFalseGoTo.Name = "_cmbFalseGoTo";
        _cmbFalseGoTo.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbFalseGoTo.Items.AddRange(new object[] { "End", "Next", "Label..." });
        _cmbFalseGoTo.SelectedIndex = 0;
        _cmbFalseGoTo.Width = 240;

        tblNotFound.Controls.Add(lblContinueWaiting, 0, 0);
        tblNotFound.Controls.Add(_numTimeoutSec, 1, 0);
        tblNotFound.Controls.Add(lblSecondsAndThen, 2, 0);

        tblNotFound.Controls.Add(lblGoToNotFound, 0, 1);
        tblNotFound.Controls.Add(_cmbFalseGoTo, 1, 1);
        tblNotFound.SetColumnSpan(_cmbFalseGoTo, 3);

        ((ISupportInitialize)_numTimeoutSec).EndInit();

        grpNotFound.Controls.Add(tblNotFound);

        // ===== bottom buttons =====
        pnlBottom = new FlowLayoutPanel();
        pnlBottom.Name = "pnlBottom";
        pnlBottom.Dock = DockStyle.Bottom;
        pnlBottom.FlowDirection = FlowDirection.RightToLeft;
        pnlBottom.Padding = new Padding(10);
        pnlBottom.Height = 55;

        _btnOk = new Button();
        _btnOk.Name = "_btnOk";
        _btnOk.Text = "OK";
        _btnOk.DialogResult = DialogResult.OK;
        _btnOk.Width = 90;
        _btnOk.Height = 28;

        _btnCancel = new Button();
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Text = "Cancel";
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Width = 90;
        _btnCancel.Height = 28;

        pnlBottom.Controls.Add(_btnOk);
        pnlBottom.Controls.Add(_btnCancel);

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        // --- Add to form (Dock順を意識) ---
        Controls.Add(pnlBottom);
        Controls.Add(grpNotFound);
        Controls.Add(grpFound);
        Controls.Add(grpSpec);

        ResumeLayout(false);
        PerformLayout();
    }
}
