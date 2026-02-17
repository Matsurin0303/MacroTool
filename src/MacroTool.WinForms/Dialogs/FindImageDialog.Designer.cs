using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MacroTool.WinForms.Dialogs;

public partial class FindImageDialog
{
    private IContainer? components = null;

    // code-behind 側でリソース解放を行うためのフック（FindImageDialog.cs 側に実装）
    partial void DisposeManagedResources();

    // ===== Containers =====
    private Label lblDesc;

    private GroupBox grpSpec;
    private TableLayoutPanel tblSpec;
    private TableLayoutPanel tblLeft;
    private FlowLayoutPanel pnlImgBtns;
    private TableLayoutPanel tblRight;
    private FlowLayoutPanel pnlAreaBtns;

    private GroupBox grpFound;
    private TableLayoutPanel tblFound;

    private GroupBox grpNotFound;
    private TableLayoutPanel tblNotFound;

    private FlowLayoutPanel pnlBottom;

    // ===== Labels =====
    private Label lblImg;
    private Label lblSearchArea;
    private Label lblTolerance;

    private Label lblAndY;
    private Label lblGoToFound;

    private Label lblContinueWaiting;
    private Label lblSecondsAndThen;
    private Label lblGoToNotFound;

    // ===== UI controls (code-behind から参照するためフィールド化) =====
    private PictureBox _picTemplate;
    private Button _btnCapture;
    private Button _btnOpen;
    private Button _btnClear;

    private ComboBox _cmbArea;
    private Button _btnDefineArea;
    private Button _btnConfirmArea;

    private NumericUpDown _numTolerance;
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
        tblLeft = new TableLayoutPanel();
        lblImg = new Label();
        _picTemplate = new PictureBox();
        pnlImgBtns = new FlowLayoutPanel();
        _btnCapture = new Button();
        _btnOpen = new Button();
        _btnClear = new Button();
        tblRight = new TableLayoutPanel();
        lblSearchArea = new Label();
        _cmbArea = new ComboBox();
        pnlAreaBtns = new FlowLayoutPanel();
        _btnDefineArea = new Button();
        _btnConfirmArea = new Button();
        lblTolerance = new Label();
        _numTolerance = new NumericUpDown();
        _btnTest = new Button();
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
        grpSpec.SuspendLayout();
        tblSpec.SuspendLayout();
        tblLeft.SuspendLayout();
        ((ISupportInitialize)_picTemplate).BeginInit();
        pnlImgBtns.SuspendLayout();
        tblRight.SuspendLayout();
        pnlAreaBtns.SuspendLayout();
        ((ISupportInitialize)_numTolerance).BeginInit();
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
        lblDesc.Location = new Point(0, 455);
        lblDesc.Name = "lblDesc";
        lblDesc.Padding = new Padding(10, 8, 10, 0);
        lblDesc.Size = new Size(720, 34);
        lblDesc.TabIndex = 4;
        lblDesc.Text = "Finds position of the defined image in the selected screen area.";
        // 
        // grpSpec
        // 
        grpSpec.Controls.Add(tblSpec);
        grpSpec.Dock = DockStyle.Top;
        grpSpec.Location = new Point(0, 0);
        grpSpec.Name = "grpSpec";
        grpSpec.Padding = new Padding(10);
        grpSpec.Size = new Size(720, 185);
        grpSpec.TabIndex = 3;
        grpSpec.TabStop = false;
        grpSpec.Text = "Image specifications:";
        // 
        // tblSpec
        // 
        tblSpec.ColumnCount = 2;
        tblSpec.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270F));
        tblSpec.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblSpec.Controls.Add(tblLeft, 0, 0);
        tblSpec.Controls.Add(tblRight, 1, 0);
        tblSpec.Dock = DockStyle.Fill;
        tblSpec.Location = new Point(10, 26);
        tblSpec.Name = "tblSpec";
        tblSpec.RowCount = 1;
        tblSpec.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        tblSpec.Size = new Size(700, 149);
        tblSpec.TabIndex = 0;
        // 
        // tblLeft
        // 
        tblLeft.ColumnCount = 1;
        tblLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        tblLeft.Controls.Add(lblImg, 0, 0);
        tblLeft.Controls.Add(_picTemplate, 0, 1);
        tblLeft.Controls.Add(pnlImgBtns, 0, 2);
        tblLeft.Dock = DockStyle.Fill;
        tblLeft.Location = new Point(3, 3);
        tblLeft.Name = "tblLeft";
        tblLeft.RowCount = 3;
        tblLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        tblLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tblLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        tblLeft.Size = new Size(264, 143);
        tblLeft.TabIndex = 0;
        // 
        // lblImg
        // 
        lblImg.AutoSize = true;
        lblImg.Location = new Point(0, 0);
        lblImg.Margin = new Padding(0);
        lblImg.Name = "lblImg";
        lblImg.Size = new Size(42, 15);
        lblImg.TabIndex = 0;
        lblImg.Text = "Image:";
        // 
        // _picTemplate
        // 
        _picTemplate.BorderStyle = BorderStyle.FixedSingle;
        _picTemplate.Dock = DockStyle.Fill;
        _picTemplate.Location = new Point(3, 21);
        _picTemplate.Name = "_picTemplate";
        _picTemplate.Size = new Size(258, 85);
        _picTemplate.SizeMode = PictureBoxSizeMode.Zoom;
        _picTemplate.TabIndex = 1;
        _picTemplate.TabStop = false;
        // 
        // pnlImgBtns
        // 
        pnlImgBtns.Controls.Add(_btnCapture);
        pnlImgBtns.Controls.Add(_btnOpen);
        pnlImgBtns.Controls.Add(_btnClear);
        pnlImgBtns.Dock = DockStyle.Fill;
        pnlImgBtns.Location = new Point(0, 109);
        pnlImgBtns.Margin = new Padding(0);
        pnlImgBtns.Name = "pnlImgBtns";
        pnlImgBtns.Size = new Size(264, 34);
        pnlImgBtns.TabIndex = 2;
        pnlImgBtns.WrapContents = false;
        // 
        // _btnCapture
        // 
        _btnCapture.Image = Properties.Resources.Capture;
        _btnCapture.Location = new Point(0, 0);
        _btnCapture.Margin = new Padding(0, 0, 6, 0);
        _btnCapture.Name = "_btnCapture";
        _btnCapture.Size = new Size(28, 28);
        _btnCapture.TabIndex = 0;
        // 
        // _btnOpen
        // 
        _btnOpen.Image = Properties.Resources.Folder;
        _btnOpen.Location = new Point(34, 0);
        _btnOpen.Margin = new Padding(0, 0, 6, 0);
        _btnOpen.Name = "_btnOpen";
        _btnOpen.Size = new Size(28, 28);
        _btnOpen.TabIndex = 1;
        // 
        // _btnClear
        // 
        _btnClear.Image = Properties.Resources.Clear;
        _btnClear.Location = new Point(68, 0);
        _btnClear.Margin = new Padding(0);
        _btnClear.Name = "_btnClear";
        _btnClear.Size = new Size(28, 28);
        _btnClear.TabIndex = 2;
        // 
        // tblRight
        // 
        tblRight.ColumnCount = 3;
        tblRight.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155F));
        tblRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tblRight.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        tblRight.Controls.Add(lblSearchArea, 0, 0);
        tblRight.Controls.Add(_cmbArea, 1, 0);
        tblRight.Controls.Add(pnlAreaBtns, 2, 0);
        tblRight.Controls.Add(lblTolerance, 0, 1);
        tblRight.Controls.Add(_numTolerance, 1, 1);
        tblRight.Controls.Add(_btnTest, 2, 1);
        tblRight.Dock = DockStyle.Fill;
        tblRight.Location = new Point(273, 3);
        tblRight.Name = "tblRight";
        tblRight.RowCount = 2;
        tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        tblRight.Size = new Size(424, 143);
        tblRight.TabIndex = 1;
        // 
        // lblSearchArea
        // 
        lblSearchArea.AutoSize = true;
        lblSearchArea.Location = new Point(0, 9);
        lblSearchArea.Margin = new Padding(0, 9, 0, 0);
        lblSearchArea.Name = "lblSearchArea";
        lblSearchArea.Size = new Size(125, 15);
        lblSearchArea.TabIndex = 0;
        lblSearchArea.Text = "Define the search area:";
        // 
        // _cmbArea
        // 
        _cmbArea.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbArea.Items.AddRange(new object[] { "Entire desktop", "Focused window", "Area of desktop", "Area of focused window" });
        _cmbArea.Location = new Point(155, 6);
        _cmbArea.Margin = new Padding(0, 6, 0, 0);
        _cmbArea.Name = "_cmbArea";
        _cmbArea.Size = new Size(121, 23);
        _cmbArea.TabIndex = 1;
        // 
        // pnlAreaBtns
        // 
        pnlAreaBtns.Controls.Add(_btnDefineArea);
        pnlAreaBtns.Controls.Add(_btnConfirmArea);
        pnlAreaBtns.Dock = DockStyle.Fill;
        pnlAreaBtns.FlowDirection = FlowDirection.TopDown;
        pnlAreaBtns.Location = new Point(299, 4);
        pnlAreaBtns.Margin = new Padding(0, 4, 0, 0);
        pnlAreaBtns.Name = "pnlAreaBtns";
        pnlAreaBtns.Size = new Size(125, 60);
        pnlAreaBtns.TabIndex = 2;
        pnlAreaBtns.WrapContents = false;
        // 
        // _btnDefineArea
        // 
        _btnDefineArea.Location = new Point(3, 3);
        _btnDefineArea.Name = "_btnDefineArea";
        _btnDefineArea.Size = new Size(110, 26);
        _btnDefineArea.TabIndex = 0;
        _btnDefineArea.Text = "Define...";
        // 
        // _btnConfirmArea
        // 
        _btnConfirmArea.Location = new Point(3, 35);
        _btnConfirmArea.Name = "_btnConfirmArea";
        _btnConfirmArea.Size = new Size(110, 26);
        _btnConfirmArea.TabIndex = 1;
        _btnConfirmArea.Text = "Confirm Area";
        // 
        // lblTolerance
        // 
        lblTolerance.AutoSize = true;
        lblTolerance.Location = new Point(0, 73);
        lblTolerance.Margin = new Padding(0, 9, 0, 0);
        lblTolerance.Name = "lblTolerance";
        lblTolerance.Size = new Size(90, 15);
        lblTolerance.TabIndex = 3;
        lblTolerance.Text = "Color tolerance:";
        // 
        // _numTolerance
        // 
        _numTolerance.Location = new Point(155, 70);
        _numTolerance.Margin = new Padding(0, 6, 0, 0);
        _numTolerance.Name = "_numTolerance";
        _numTolerance.Size = new Size(80, 23);
        _numTolerance.TabIndex = 4;
        // 
        // _btnTest
        // 
        _btnTest.Location = new Point(302, 67);
        _btnTest.Name = "_btnTest";
        _btnTest.Size = new Size(80, 26);
        _btnTest.TabIndex = 5;
        _btnTest.Text = "Test";
        // 
        // grpFound
        // 
        grpFound.Controls.Add(tblFound);
        grpFound.Dock = DockStyle.Top;
        grpFound.Location = new Point(0, 185);
        grpFound.Name = "grpFound";
        grpFound.Padding = new Padding(10);
        grpFound.Size = new Size(720, 150);
        grpFound.TabIndex = 2;
        grpFound.TabStop = false;
        grpFound.Text = "If image is found";
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
        tblFound.Size = new Size(700, 114);
        tblFound.TabIndex = 0;
        // 
        // _chkMouseAction
        // 
        _chkMouseAction.AutoSize = true;
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
        _txtSaveX.Location = new Point(143, 33);
        _txtSaveX.Name = "_txtSaveX";
        _txtSaveX.Size = new Size(120, 23);
        _txtSaveX.TabIndex = 4;
        // 
        // lblAndY
        // 
        lblAndY.AutoSize = true;
        lblAndY.Location = new Point(280, 39);
        lblAndY.Margin = new Padding(0, 9, 0, 0);
        lblAndY.Name = "lblAndY";
        lblAndY.Size = new Size(54, 15);
        lblAndY.TabIndex = 5;
        lblAndY.Text = "and Y to:";
        // 
        // _txtSaveY
        // 
        _txtSaveY.Location = new Point(373, 33);
        _txtSaveY.Name = "_txtSaveY";
        _txtSaveY.Size = new Size(120, 23);
        _txtSaveY.TabIndex = 6;
        // 
        // lblGoToFound
        // 
        lblGoToFound.AutoSize = true;
        lblGoToFound.Location = new Point(0, 69);
        lblGoToFound.Margin = new Padding(0, 9, 0, 0);
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
        grpNotFound.Location = new Point(0, 335);
        grpNotFound.Name = "grpNotFound";
        grpNotFound.Padding = new Padding(10);
        grpNotFound.Size = new Size(720, 120);
        grpNotFound.TabIndex = 1;
        grpNotFound.TabStop = false;
        grpNotFound.Text = "If image is not found";
        // 
        // tblNotFound
        // 
        tblNotFound.ColumnCount = 4;
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
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
        tblNotFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tblNotFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tblNotFound.Size = new Size(700, 84);
        tblNotFound.TabIndex = 0;
        // 
        // lblContinueWaiting
        // 
        lblContinueWaiting.AutoSize = true;
        lblContinueWaiting.Location = new Point(0, 9);
        lblContinueWaiting.Margin = new Padding(0, 9, 0, 0);
        lblContinueWaiting.Name = "lblContinueWaiting";
        lblContinueWaiting.Size = new Size(97, 15);
        lblContinueWaiting.TabIndex = 0;
        lblContinueWaiting.Text = "Continue waiting";
        // 
        // _numTimeoutSec
        // 
        _numTimeoutSec.Location = new Point(173, 3);
        _numTimeoutSec.Maximum = new decimal(new int[] { 86400, 0, 0, 0 });
        _numTimeoutSec.Name = "_numTimeoutSec";
        _numTimeoutSec.Size = new Size(80, 23);
        _numTimeoutSec.TabIndex = 1;
        // 
        // lblSecondsAndThen
        // 
        lblSecondsAndThen.AutoSize = true;
        lblSecondsAndThen.Location = new Point(260, 9);
        lblSecondsAndThen.Margin = new Padding(0, 9, 0, 0);
        lblSecondsAndThen.Name = "lblSecondsAndThen";
        lblSecondsAndThen.Size = new Size(100, 15);
        lblSecondsAndThen.TabIndex = 2;
        lblSecondsAndThen.Text = "seconds and then";
        // 
        // lblGoToNotFound
        // 
        lblGoToNotFound.AutoSize = true;
        lblGoToNotFound.Location = new Point(0, 37);
        lblGoToNotFound.Margin = new Padding(0, 9, 0, 0);
        lblGoToNotFound.Name = "lblGoToNotFound";
        lblGoToNotFound.Size = new Size(36, 15);
        lblGoToNotFound.TabIndex = 3;
        lblGoToNotFound.Text = "Go to";
        // 
        // _cmbFalseGoTo
        // 
        tblNotFound.SetColumnSpan(_cmbFalseGoTo, 3);
        _cmbFalseGoTo.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbFalseGoTo.Items.AddRange(new object[] { "Next", "End", "Label..." });
        _cmbFalseGoTo.Location = new Point(173, 31);
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
        pnlBottom.Location = new Point(0, 491);
        pnlBottom.Name = "pnlBottom";
        pnlBottom.Padding = new Padding(10);
        pnlBottom.Size = new Size(720, 55);
        pnlBottom.TabIndex = 0;
        // 
        // _btnOk
        // 
        _btnOk.DialogResult = DialogResult.OK;
        _btnOk.Location = new Point(607, 13);
        _btnOk.Name = "_btnOk";
        _btnOk.Size = new Size(90, 28);
        _btnOk.TabIndex = 0;
        _btnOk.Text = "OK";
        // 
        // _btnCancel
        // 
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Location = new Point(511, 13);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new Size(90, 28);
        _btnCancel.TabIndex = 1;
        _btnCancel.Text = "Cancel";
        // 
        // FindImageDialog
        // 
        AcceptButton = _btnOk;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = _btnCancel;
        ClientSize = new Size(720, 546);
        Controls.Add(lblDesc);
        Controls.Add(pnlBottom);
        Controls.Add(grpNotFound);
        Controls.Add(grpFound);
        Controls.Add(grpSpec);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(720, 520);
        Name = "FindImageDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Search image";
        grpSpec.ResumeLayout(false);
        tblSpec.ResumeLayout(false);
        tblLeft.ResumeLayout(false);
        tblLeft.PerformLayout();
        ((ISupportInitialize)_picTemplate).EndInit();
        pnlImgBtns.ResumeLayout(false);
        tblRight.ResumeLayout(false);
        tblRight.PerformLayout();
        pnlAreaBtns.ResumeLayout(false);
        ((ISupportInitialize)_numTolerance).EndInit();
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
}
