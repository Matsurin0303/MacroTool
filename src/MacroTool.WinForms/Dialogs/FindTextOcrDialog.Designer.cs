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
    private Label lblDesc;
    private GroupBox grpSpec;
    private TableLayoutPanel tblSpec;
    private TableLayoutPanel tblAreaRow;
    private FlowLayoutPanel pnlAreaBtns;

    private GroupBox grpFound;
    private TableLayoutPanel tblFound;

    private GroupBox grpNotFound;
    private TableLayoutPanel tblNotFound;

    private FlowLayoutPanel pnlBottom;

    // ===== Labels =====
    private Label lblText;
    private Label lblLangTitle;
    private Label lblAreaTitle;
    private Label lblAndY;
    private Label lblGoToFound;
    private Label lblContinueWaiting;
    private Label lblSecondsAndThen;
    private Label lblGoToNotFound;

    // ===== UI controls (code-behind から参照するためフィールド化) =====
    private TextBox _txtText;
    private CheckBox _chkRegex;
    private ComboBox _cmbLang;

    private CheckBox _chkOptimizeContrast;
    private CheckBox _chkOptimizeShortText;

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
        lblDesc = new Label();
        grpSpec = new GroupBox();
        tblSpec = new TableLayoutPanel();
        lblText = new Label();
        _txtText = new TextBox();
        _chkRegex = new CheckBox();
        lblLangTitle = new Label();
        _cmbLang = new ComboBox();
        lblAreaTitle = new Label();
        tblAreaRow = new TableLayoutPanel();
        _cmbArea = new ComboBox();
        pnlAreaBtns = new FlowLayoutPanel();
        _btnDefineArea = new Button();
        _btnConfirmArea = new Button();
        _btnTest = new Button();
        _chkOptimizeContrast = new CheckBox();
        _chkOptimizeShortText = new CheckBox();
        grpFound = new GroupBox();
        tblFound = new TableLayoutPanel();
        _chkMouseAction = new CheckBox();
        _cmbMouseAction = new ComboBox();
        _cmbMousePos = new ComboBox();
        _chkSaveCoord = new CheckBox();
        _txtSaveX = new TextBox();
        lblAndY = new Label();
        _txtSaveY = new TextBox();
        lblGoToFound = new Label();
        _cmbTrueGoTo = new ComboBox();
        grpNotFound = new GroupBox();
        tblNotFound = new TableLayoutPanel();
        lblContinueWaiting = new Label();
        _numTimeoutSec = new NumericUpDown();
        lblSecondsAndThen = new Label();
        lblGoToNotFound = new Label();
        _cmbFalseGoTo = new ComboBox();
        pnlBottom = new FlowLayoutPanel();
        _btnOk = new Button();
        _btnCancel = new Button();
        _lblTestResult = new Label();
        grpSpec.SuspendLayout();
        tblSpec.SuspendLayout();
        tblAreaRow.SuspendLayout();
        pnlAreaBtns.SuspendLayout();
        grpFound.SuspendLayout();
        tblFound.SuspendLayout();
        grpNotFound.SuspendLayout();
        tblNotFound.SuspendLayout();
        ((ISupportInitialize)_numTimeoutSec).BeginInit();
        pnlBottom.SuspendLayout();
        SuspendLayout();
        // 
        // lblDesc
        // 
        lblDesc.Dock = DockStyle.Top;
        lblDesc.Location = new Point(0, 652);
        lblDesc.Name = "lblDesc";
        lblDesc.Padding = new Padding(10, 8, 10, 0);
        lblDesc.Size = new Size(700, 47);
        lblDesc.TabIndex = 4;
        lblDesc.Text = "Searches the position of the defined text with on-screen character recognition (OCR)\r\nin the selected screen area.";
        // 
        // grpSpec
        // 
        grpSpec.Controls.Add(tblSpec);
        grpSpec.Dock = DockStyle.Top;
        grpSpec.Location = new Point(0, 0);
        grpSpec.Name = "grpSpec";
        grpSpec.Padding = new Padding(10);
        grpSpec.Size = new Size(700, 421);
        grpSpec.TabIndex = 3;
        grpSpec.TabStop = false;
        grpSpec.Text = "What and where to search for:";
        // 
        // tblSpec
        // 
        tblSpec.ColumnCount = 1;
        tblSpec.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblSpec.Controls.Add(lblText, 0, 0);
        tblSpec.Controls.Add(_txtText, 0, 1);
        tblSpec.Controls.Add(_chkRegex, 0, 2);
        tblSpec.Controls.Add(lblLangTitle, 0, 3);
        tblSpec.Controls.Add(_cmbLang, 0, 4);
        tblSpec.Controls.Add(lblAreaTitle, 0, 5);
        tblSpec.Controls.Add(tblAreaRow, 0, 6);
        tblSpec.Controls.Add(_chkOptimizeContrast, 0, 7);
        tblSpec.Controls.Add(_chkOptimizeShortText, 0, 8);
        tblSpec.Dock = DockStyle.Fill;
        tblSpec.Location = new Point(10, 26);
        tblSpec.Name = "tblSpec";
        tblSpec.RowCount = 9;
        tblSpec.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        tblSpec.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        tblSpec.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        tblSpec.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        tblSpec.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        tblSpec.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        tblSpec.RowStyles.Add(new RowStyle(SizeType.Absolute, 135F));
        tblSpec.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblSpec.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        tblSpec.Size = new Size(680, 385);
        tblSpec.TabIndex = 0;
        // 
        // lblText
        // 
        lblText.AutoSize = true;
        lblText.Location = new Point(3, 0);
        lblText.Name = "lblText";
        lblText.Size = new Size(251, 15);
        lblText.TabIndex = 0;
        lblText.Text = "Text to search for (right-click to add variables):";
        // 
        // _txtText
        // 
        _txtText.Dock = DockStyle.Fill;
        _txtText.Location = new Point(3, 23);
        _txtText.Multiline = true;
        _txtText.Name = "_txtText";
        _txtText.ScrollBars = ScrollBars.Vertical;
        _txtText.Size = new Size(674, 58);
        _txtText.TabIndex = 1;
        // 
        // _chkRegex
        // 
        _chkRegex.AutoSize = true;
        _chkRegex.Location = new Point(3, 87);
        _chkRegex.Name = "_chkRegex";
        _chkRegex.Size = new Size(85, 18);
        _chkRegex.TabIndex = 2;
        _chkRegex.Text = "RegEx term";
        // 
        // lblLangTitle
        // 
        lblLangTitle.AutoSize = true;
        lblLangTitle.Location = new Point(0, 114);
        lblLangTitle.Margin = new Padding(0, 6, 0, 0);
        lblLangTitle.Name = "lblLangTitle";
        lblLangTitle.Size = new Size(119, 14);
        lblLangTitle.TabIndex = 3;
        lblLangTitle.Text = "Language of the text:";
        // 
        // _cmbLang
        // 
        _cmbLang.Anchor = AnchorStyles.Left;
        _cmbLang.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbLang.Items.AddRange(new object[] { "English", "Japanese" });
        _cmbLang.Location = new Point(3, 132);
        _cmbLang.Name = "_cmbLang";
        _cmbLang.Size = new Size(200, 23);
        _cmbLang.TabIndex = 4;
        // 
        // lblAreaTitle
        // 
        lblAreaTitle.AutoSize = true;
        lblAreaTitle.Location = new Point(0, 166);
        lblAreaTitle.Margin = new Padding(0, 6, 0, 0);
        lblAreaTitle.Name = "lblAreaTitle";
        lblAreaTitle.Size = new Size(125, 15);
        lblAreaTitle.TabIndex = 5;
        lblAreaTitle.Text = "Define the search area:";
        // 
        // tblAreaRow
        // 
        tblAreaRow.ColumnCount = 2;
        tblAreaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblAreaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122F));
        tblAreaRow.Controls.Add(_cmbArea, 0, 0);
        tblAreaRow.Controls.Add(pnlAreaBtns, 1, 0);
        tblAreaRow.Dock = DockStyle.Fill;
        tblAreaRow.Location = new Point(3, 189);
        tblAreaRow.Name = "tblAreaRow";
        tblAreaRow.RowCount = 1;
        tblAreaRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tblAreaRow.Size = new Size(674, 129);
        tblAreaRow.TabIndex = 6;
        // 
        // _cmbArea
        // 
        _cmbArea.Dock = DockStyle.Top;
        _cmbArea.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbArea.Items.AddRange(new object[] { "Entire desktop", "Focused window", "Area of desktop", "Area of focused window" });
        _cmbArea.Location = new Point(3, 3);
        _cmbArea.Name = "_cmbArea";
        _cmbArea.Size = new Size(546, 23);
        _cmbArea.TabIndex = 0;
        // 
        // pnlAreaBtns
        // 
        pnlAreaBtns.Controls.Add(_btnDefineArea);
        pnlAreaBtns.Controls.Add(_btnConfirmArea);
        pnlAreaBtns.Controls.Add(_btnTest);
        pnlAreaBtns.Controls.Add(_lblTestResult);
        pnlAreaBtns.Dock = DockStyle.Fill;
        pnlAreaBtns.FlowDirection = FlowDirection.TopDown;
        pnlAreaBtns.Location = new Point(552, 4);
        pnlAreaBtns.Margin = new Padding(0, 4, 0, 0);
        pnlAreaBtns.Name = "pnlAreaBtns";
        pnlAreaBtns.Size = new Size(122, 125);
        pnlAreaBtns.TabIndex = 1;
        pnlAreaBtns.WrapContents = false;
        // 
        // _btnDefineArea
        // 
        _btnDefineArea.Enabled = false;
        _btnDefineArea.Location = new Point(3, 3);
        _btnDefineArea.Name = "_btnDefineArea";
        _btnDefineArea.Size = new Size(110, 26);
        _btnDefineArea.TabIndex = 0;
        _btnDefineArea.Text = "Define...";
        // 
        // _btnConfirmArea
        // 
        _btnConfirmArea.Enabled = false;
        _btnConfirmArea.Location = new Point(3, 35);
        _btnConfirmArea.Name = "_btnConfirmArea";
        _btnConfirmArea.Size = new Size(110, 26);
        _btnConfirmArea.TabIndex = 1;
        _btnConfirmArea.Text = "Confirm Area";
        // 
        // _btnTest
        // 
        _btnTest.Location = new Point(3, 67);
        _btnTest.Name = "_btnTest";
        _btnTest.Size = new Size(110, 26);
        _btnTest.TabIndex = 2;
        _btnTest.Text = "Test";
        // 
        // _chkOptimizeContrast
        // 
        _chkOptimizeContrast.AutoSize = true;
        _chkOptimizeContrast.Location = new Point(3, 324);
        _chkOptimizeContrast.Name = "_chkOptimizeContrast";
        _chkOptimizeContrast.Size = new Size(197, 19);
        _chkOptimizeContrast.TabIndex = 7;
        _chkOptimizeContrast.Text = "Optimize contrast and sharpness";
        // 
        // _chkOptimizeShortText
        // 
        _chkOptimizeShortText.AutoSize = true;
        _chkOptimizeShortText.Location = new Point(3, 354);
        _chkOptimizeShortText.Name = "_chkOptimizeShortText";
        _chkOptimizeShortText.Size = new Size(249, 19);
        _chkOptimizeShortText.TabIndex = 8;
        _chkOptimizeShortText.Text = "Optimize for single characters or short text";
        // 
        // grpFound
        // 
        grpFound.Controls.Add(tblFound);
        grpFound.Dock = DockStyle.Top;
        grpFound.Location = new Point(0, 421);
        grpFound.Name = "grpFound";
        grpFound.Padding = new Padding(10);
        grpFound.Size = new Size(700, 130);
        grpFound.TabIndex = 2;
        grpFound.TabStop = false;
        grpFound.Text = "If text is found";
        // 
        // tblFound
        // 
        tblFound.ColumnCount = 4;
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblFound.Controls.Add(_chkMouseAction, 0, 0);
        tblFound.Controls.Add(_cmbMouseAction, 1, 0);
        tblFound.Controls.Add(_cmbMousePos, 2, 0);
        tblFound.Controls.Add(_chkSaveCoord, 0, 1);
        tblFound.Controls.Add(_txtSaveX, 1, 1);
        tblFound.Controls.Add(lblAndY, 2, 1);
        tblFound.Controls.Add(_txtSaveY, 3, 1);
        tblFound.Controls.Add(lblGoToFound, 0, 2);
        tblFound.Controls.Add(_cmbTrueGoTo, 1, 2);
        tblFound.Dock = DockStyle.Fill;
        tblFound.Location = new Point(10, 26);
        tblFound.Name = "tblFound";
        tblFound.RowCount = 3;
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblFound.Size = new Size(680, 94);
        tblFound.TabIndex = 0;
        // 
        // _chkMouseAction
        // 
        _chkMouseAction.AutoSize = true;
        _chkMouseAction.Checked = true;
        _chkMouseAction.CheckState = CheckState.Checked;
        _chkMouseAction.Location = new Point(3, 3);
        _chkMouseAction.Name = "_chkMouseAction";
        _chkMouseAction.Size = new Size(101, 19);
        _chkMouseAction.TabIndex = 0;
        _chkMouseAction.Text = "Mouse action:";
        // 
        // _cmbMouseAction
        // 
        _cmbMouseAction.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbMouseAction.Items.AddRange(new object[] { "Positioning", "LeftClick", "RightClick", "MiddleClick", "DoubleClick" });
        _cmbMouseAction.Location = new Point(143, 3);
        _cmbMouseAction.Name = "_cmbMouseAction";
        _cmbMouseAction.Size = new Size(130, 23);
        _cmbMouseAction.TabIndex = 1;
        // 
        // _cmbMousePos
        // 
        tblFound.SetColumnSpan(_cmbMousePos, 2);
        _cmbMousePos.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbMousePos.Items.AddRange(new object[] { "Centered", "Top-left", "Top-right", "Bottom-left", "Bottom-right" });
        _cmbMousePos.Location = new Point(283, 3);
        _cmbMousePos.Name = "_cmbMousePos";
        _cmbMousePos.Size = new Size(130, 23);
        _cmbMousePos.TabIndex = 2;
        // 
        // _chkSaveCoord
        // 
        _chkSaveCoord.AutoSize = true;
        _chkSaveCoord.Location = new Point(3, 33);
        _chkSaveCoord.Name = "_chkSaveCoord";
        _chkSaveCoord.Size = new Size(77, 19);
        _chkSaveCoord.TabIndex = 3;
        _chkSaveCoord.Text = "Save X to:";
        // 
        // _txtSaveX
        // 
        _txtSaveX.Enabled = false;
        _txtSaveX.Location = new Point(143, 33);
        _txtSaveX.Name = "_txtSaveX";
        _txtSaveX.Size = new Size(120, 23);
        _txtSaveX.TabIndex = 4;
        _txtSaveX.Text = "X";
        // 
        // lblAndY
        // 
        lblAndY.AutoSize = true;
        lblAndY.Location = new Point(280, 38);
        lblAndY.Margin = new Padding(0, 8, 0, 0);
        lblAndY.Name = "lblAndY";
        lblAndY.Size = new Size(54, 15);
        lblAndY.TabIndex = 5;
        lblAndY.Text = "and Y to:";
        // 
        // _txtSaveY
        // 
        _txtSaveY.Enabled = false;
        _txtSaveY.Location = new Point(373, 33);
        _txtSaveY.Name = "_txtSaveY";
        _txtSaveY.Size = new Size(120, 23);
        _txtSaveY.TabIndex = 6;
        _txtSaveY.Text = "Y";
        // 
        // lblGoToFound
        // 
        lblGoToFound.AutoSize = true;
        lblGoToFound.Location = new Point(0, 68);
        lblGoToFound.Margin = new Padding(0, 8, 0, 0);
        lblGoToFound.Name = "lblGoToFound";
        lblGoToFound.Size = new Size(36, 15);
        lblGoToFound.TabIndex = 7;
        lblGoToFound.Text = "Go to";
        // 
        // _cmbTrueGoTo
        // 
        tblFound.SetColumnSpan(_cmbTrueGoTo, 3);
        _cmbTrueGoTo.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbTrueGoTo.Items.AddRange(new object[] { "Next", "End", "Label..." });
        _cmbTrueGoTo.Location = new Point(143, 63);
        _cmbTrueGoTo.Name = "_cmbTrueGoTo";
        _cmbTrueGoTo.Size = new Size(240, 23);
        _cmbTrueGoTo.TabIndex = 8;
        // 
        // grpNotFound
        // 
        grpNotFound.Controls.Add(tblNotFound);
        grpNotFound.Dock = DockStyle.Top;
        grpNotFound.Location = new Point(0, 551);
        grpNotFound.Name = "grpNotFound";
        grpNotFound.Padding = new Padding(10);
        grpNotFound.Size = new Size(700, 101);
        grpNotFound.TabIndex = 1;
        grpNotFound.TabStop = false;
        grpNotFound.Text = "If text is not found";
        // 
        // tblNotFound
        // 
        tblNotFound.ColumnCount = 4;
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblNotFound.Controls.Add(lblContinueWaiting, 0, 0);
        tblNotFound.Controls.Add(_numTimeoutSec, 1, 0);
        tblNotFound.Controls.Add(lblSecondsAndThen, 2, 0);
        tblNotFound.Controls.Add(lblGoToNotFound, 0, 1);
        tblNotFound.Controls.Add(_cmbFalseGoTo, 1, 1);
        tblNotFound.Dock = DockStyle.Fill;
        tblNotFound.Location = new Point(10, 26);
        tblNotFound.Name = "tblNotFound";
        tblNotFound.RowCount = 2;
        tblNotFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblNotFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        tblNotFound.Size = new Size(680, 65);
        tblNotFound.TabIndex = 0;
        // 
        // lblContinueWaiting
        // 
        lblContinueWaiting.AutoSize = true;
        lblContinueWaiting.Location = new Point(0, 8);
        lblContinueWaiting.Margin = new Padding(0, 8, 0, 0);
        lblContinueWaiting.Name = "lblContinueWaiting";
        lblContinueWaiting.Size = new Size(97, 15);
        lblContinueWaiting.TabIndex = 0;
        lblContinueWaiting.Text = "Continue waiting";
        // 
        // _numTimeoutSec
        // 
        _numTimeoutSec.Location = new Point(143, 3);
        _numTimeoutSec.Maximum = new decimal(new int[] { 86400, 0, 0, 0 });
        _numTimeoutSec.Name = "_numTimeoutSec";
        _numTimeoutSec.Size = new Size(80, 23);
        _numTimeoutSec.TabIndex = 1;
        _numTimeoutSec.Value = new decimal(new int[] { 120, 0, 0, 0 });
        // 
        // lblSecondsAndThen
        // 
        lblSecondsAndThen.AutoSize = true;
        lblSecondsAndThen.Location = new Point(230, 8);
        lblSecondsAndThen.Margin = new Padding(0, 8, 0, 0);
        lblSecondsAndThen.Name = "lblSecondsAndThen";
        lblSecondsAndThen.Size = new Size(100, 15);
        lblSecondsAndThen.TabIndex = 2;
        lblSecondsAndThen.Text = "seconds and then";
        // 
        // lblGoToNotFound
        // 
        lblGoToNotFound.AutoSize = true;
        lblGoToNotFound.Location = new Point(0, 38);
        lblGoToNotFound.Margin = new Padding(0, 8, 0, 0);
        lblGoToNotFound.Name = "lblGoToNotFound";
        lblGoToNotFound.Size = new Size(36, 15);
        lblGoToNotFound.TabIndex = 3;
        lblGoToNotFound.Text = "Go to";
        // 
        // _cmbFalseGoTo
        // 
        tblNotFound.SetColumnSpan(_cmbFalseGoTo, 3);
        _cmbFalseGoTo.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbFalseGoTo.Items.AddRange(new object[] { "End", "Next", "Label..." });
        _cmbFalseGoTo.Location = new Point(143, 33);
        _cmbFalseGoTo.Name = "_cmbFalseGoTo";
        _cmbFalseGoTo.Size = new Size(240, 23);
        _cmbFalseGoTo.TabIndex = 4;
        // 
        // pnlBottom
        // 
        pnlBottom.Controls.Add(_btnOk);
        pnlBottom.Controls.Add(_btnCancel);
        pnlBottom.Dock = DockStyle.Bottom;
        pnlBottom.FlowDirection = FlowDirection.RightToLeft;
        pnlBottom.Location = new Point(0, 712);
        pnlBottom.Name = "pnlBottom";
        pnlBottom.Padding = new Padding(10);
        pnlBottom.Size = new Size(700, 55);
        pnlBottom.TabIndex = 0;
        // 
        // _btnOk
        // 
        _btnOk.DialogResult = DialogResult.OK;
        _btnOk.Location = new Point(587, 13);
        _btnOk.Name = "_btnOk";
        _btnOk.Size = new Size(90, 28);
        _btnOk.TabIndex = 0;
        _btnOk.Text = "OK";
        // 
        // _btnCancel
        // 
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Location = new Point(491, 13);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new Size(90, 28);
        _btnCancel.TabIndex = 1;
        _btnCancel.Text = "Cancel";
        // 
        // _lblTestResult
        // 
        _lblTestResult.Location = new Point(3, 96);
        _lblTestResult.Name = "_lblTestResult";
        _lblTestResult.Size = new Size(110, 18);
        _lblTestResult.TabIndex = 3;
        _lblTestResult.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // FindTextOcrDialog
        // 
        AcceptButton = _btnOk;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = _btnCancel;
        ClientSize = new Size(700, 767);
        Controls.Add(lblDesc);
        Controls.Add(pnlBottom);
        Controls.Add(grpNotFound);
        Controls.Add(grpFound);
        Controls.Add(grpSpec);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(716, 599);
        Name = "FindTextOcrDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Find text (OCR)";
        grpSpec.ResumeLayout(false);
        tblSpec.ResumeLayout(false);
        tblSpec.PerformLayout();
        tblAreaRow.ResumeLayout(false);
        pnlAreaBtns.ResumeLayout(false);
        grpFound.ResumeLayout(false);
        tblFound.ResumeLayout(false);
        tblFound.PerformLayout();
        grpNotFound.ResumeLayout(false);
        tblNotFound.ResumeLayout(false);
        tblNotFound.PerformLayout();
        ((ISupportInitialize)_numTimeoutSec).EndInit();
        pnlBottom.ResumeLayout(false);
        ResumeLayout(false);
    }
    private Label _lblTestResult;
}
