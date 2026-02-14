namespace MacroTool.WinForms
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            tableLayoutPanel1 = new TableLayoutPanel();
            pnlRibbon = new Panel();
            tabRibbon = new TabControl();
            tabPage2 = new TabPage();
            tsRecordEdit = new ToolStrip();
            tsbPlay = new ToolStripSplitButton();
            tsbRecord = new ToolStripSplitButton();
            tsbStop = new ToolStripSplitButton();
            toolStripSeparator1 = new ToolStripSeparator();
            tsdMouse = new ToolStripDropDownButton();
            tsdTextKey = new ToolStripDropDownButton();
            tsdWait = new ToolStripDropDownButton();
            tsdImageOcr = new ToolStripDropDownButton();
            tsdMisc = new ToolStripDropDownButton();
            toolStripSeparator2 = new ToolStripSeparator();
            tsbEdit = new ToolStripButton();
            tsbDelete = new ToolStripButton();
            tsbSearchReplace = new ToolStripButton();
            tabPage3 = new TabPage();
            tabPage4 = new TabPage();
            tabPage5 = new TabPage();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            newToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            recentFilesToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            saveToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripSeparator();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem3 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            gridActions = new DataGridView();
            colNo = new DataGridViewTextBoxColumn();
            colIcon = new DataGridViewImageColumn();
            colAction = new DataGridViewTextBoxColumn();
            colValue = new DataGridViewTextBoxColumn();
            colLabel = new DataGridViewTextBoxColumn();
            colComment = new DataGridViewTextBoxColumn();
            statusStrip1 = new StatusStrip();
            lblCount = new ToolStripStatusLabel();
            lblSpring = new ToolStripStatusLabel();
            lblTime = new ToolStripStatusLabel();
            imageList1 = new ImageList(components);
            imageListToolStrip = new ImageList(components);
            cmsActions = new ContextMenuStrip(components);
            mnuCopy = new ToolStripMenuItem();
            mnuCut = new ToolStripMenuItem();
            mnuPaste = new ToolStripMenuItem();
            mnuDelete = new ToolStripMenuItem();
            ToolStripSeparator = new ToolStripSeparator();
            tableLayoutPanel1.SuspendLayout();
            pnlRibbon.SuspendLayout();
            tabRibbon.SuspendLayout();
            tabPage2.SuspendLayout();
            tsRecordEdit.SuspendLayout();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridActions).BeginInit();
            statusStrip1.SuspendLayout();
            cmsActions.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Controls.Add(pnlRibbon, 0, 0);
            tableLayoutPanel1.Controls.Add(gridActions, 0, 1);
            tableLayoutPanel1.Controls.Add(statusStrip1, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Size = new Size(1034, 561);
            tableLayoutPanel1.TabIndex = 4;
            // 
            // pnlRibbon
            // 
            pnlRibbon.Controls.Add(tabRibbon);
            pnlRibbon.Controls.Add(menuStrip1);
            pnlRibbon.Dock = DockStyle.Fill;
            pnlRibbon.Location = new Point(3, 3);
            pnlRibbon.Name = "pnlRibbon";
            pnlRibbon.Size = new Size(1028, 134);
            pnlRibbon.TabIndex = 0;
            // 
            // tabRibbon
            // 
            tabRibbon.Controls.Add(tabPage2);
            tabRibbon.Controls.Add(tabPage3);
            tabRibbon.Controls.Add(tabPage4);
            tabRibbon.Controls.Add(tabPage5);
            tabRibbon.Dock = DockStyle.Fill;
            tabRibbon.Location = new Point(0, 24);
            tabRibbon.Name = "tabRibbon";
            tabRibbon.SelectedIndex = 0;
            tabRibbon.Size = new Size(1028, 110);
            tabRibbon.TabIndex = 1;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(tsRecordEdit);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1020, 82);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Record and Edit";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // tsRecordEdit
            // 
            tsRecordEdit.Dock = DockStyle.Fill;
            tsRecordEdit.GripStyle = ToolStripGripStyle.Hidden;
            tsRecordEdit.ImageScalingSize = new Size(32, 32);
            tsRecordEdit.Items.AddRange(new ToolStripItem[] { tsbPlay, tsbRecord, tsbStop, toolStripSeparator1, tsdMouse, tsdTextKey, tsdWait, tsdImageOcr, tsdMisc, toolStripSeparator2, tsbEdit, tsbDelete, tsbSearchReplace });
            tsRecordEdit.Location = new Point(3, 3);
            tsRecordEdit.Name = "tsRecordEdit";
            tsRecordEdit.Size = new Size(1014, 76);
            tsRecordEdit.TabIndex = 0;
            tsRecordEdit.Text = "toolStrip1";
            // 
            // tsbPlay
            // 
            tsbPlay.AutoSize = false;
            tsbPlay.Image = Properties.Resources.Play;
            tsbPlay.ImageTransparentColor = Color.Magenta;
            tsbPlay.Name = "tsbPlay";
            tsbPlay.Size = new Size(80, 70);
            tsbPlay.Text = "Play";
            tsbPlay.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // tsbRecord
            // 
            tsbRecord.AutoSize = false;
            tsbRecord.Image = Properties.Resources.Record;
            tsbRecord.ImageTransparentColor = Color.Magenta;
            tsbRecord.Name = "tsbRecord";
            tsbRecord.Size = new Size(80, 70);
            tsbRecord.Text = "Record";
            tsbRecord.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // tsbStop
            // 
            tsbStop.AutoSize = false;
            tsbStop.Image = Properties.Resources.Stop;
            tsbStop.ImageTransparentColor = Color.Magenta;
            tsbStop.Name = "tsbStop";
            tsbStop.Size = new Size(80, 70);
            tsbStop.Text = "Stop";
            tsbStop.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 76);
            // 
            // tsdMouse
            // 
            tsdMouse.AutoSize = false;
            tsdMouse.Image = Properties.Resources.Mouse;
            tsdMouse.ImageTransparentColor = Color.Magenta;
            tsdMouse.Name = "tsdMouse";
            tsdMouse.Size = new Size(80, 70);
            tsdMouse.Text = "Mouse";
            tsdMouse.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // tsdTextKey
            // 
            tsdTextKey.AutoSize = false;
            tsdTextKey.Image = Properties.Resources.Keyboard;
            tsdTextKey.ImageTransparentColor = Color.Magenta;
            tsdTextKey.Name = "tsdTextKey";
            tsdTextKey.Size = new Size(80, 70);
            tsdTextKey.Text = "Text/Key";
            tsdTextKey.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // tsdWait
            // 
            tsdWait.AutoSize = false;
            tsdWait.Image = Properties.Resources.Wait;
            tsdWait.ImageTransparentColor = Color.Magenta;
            tsdWait.Name = "tsdWait";
            tsdWait.Size = new Size(80, 70);
            tsdWait.Text = "Wait";
            tsdWait.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // tsdImageOcr
            // 
            tsdImageOcr.AutoSize = false;
            tsdImageOcr.Image = Properties.Resources.Image;
            tsdImageOcr.ImageTransparentColor = Color.Magenta;
            tsdImageOcr.Name = "tsdImageOcr";
            tsdImageOcr.Size = new Size(80, 70);
            tsdImageOcr.Text = "Image/OCR";
            tsdImageOcr.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // tsdMisc
            // 
            tsdMisc.AutoSize = false;
            tsdMisc.Image = Properties.Resources.Misc;
            tsdMisc.ImageTransparentColor = Color.Magenta;
            tsdMisc.Name = "tsdMisc";
            tsdMisc.Size = new Size(80, 70);
            tsdMisc.Text = "Misc";
            tsdMisc.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 76);
            // 
            // tsbEdit
            // 
            tsbEdit.AutoSize = false;
            tsbEdit.Image = Properties.Resources.Edit;
            tsbEdit.ImageTransparentColor = Color.Magenta;
            tsbEdit.Name = "tsbEdit";
            tsbEdit.Size = new Size(80, 70);
            tsbEdit.Text = "Edit";
            tsbEdit.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // tsbDelete
            // 
            tsbDelete.AutoSize = false;
            tsbDelete.Image = Properties.Resources.Delete;
            tsbDelete.ImageTransparentColor = Color.Magenta;
            tsbDelete.Name = "tsbDelete";
            tsbDelete.Size = new Size(80, 70);
            tsbDelete.Text = "Delete";
            tsbDelete.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // tsbSearchReplace
            // 
            tsbSearchReplace.AutoSize = false;
            tsbSearchReplace.Image = Properties.Resources.Search;
            tsbSearchReplace.ImageTransparentColor = Color.Magenta;
            tsbSearchReplace.Name = "tsbSearchReplace";
            tsbSearchReplace.Size = new Size(100, 70);
            tsbSearchReplace.Text = "Search && replace";
            tsbSearchReplace.TextImageRelation = TextImageRelation.ImageAboveText;
            // 
            // tabPage3
            // 
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(1020, 82);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Playback";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            tabPage4.Location = new Point(4, 24);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(1020, 82);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "View";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // tabPage5
            // 
            tabPage5.Location = new Point(4, 24);
            tabPage5.Name = "tabPage5";
            tabPage5.Padding = new Padding(3);
            tabPage5.Size = new Size(1020, 82);
            tabPage5.TabIndex = 4;
            tabPage5.Text = "Help";
            tabPage5.UseVisualStyleBackColor = true;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1028, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newToolStripMenuItem, openToolStripMenuItem, recentFilesToolStripMenuItem, toolStripMenuItem1, saveToolStripMenuItem, saveAsToolStripMenuItem, toolStripMenuItem2, settingsToolStripMenuItem, toolStripMenuItem3, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            newToolStripMenuItem.Size = new Size(180, 22);
            newToolStripMenuItem.Text = "New";
            newToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openToolStripMenuItem.Size = new Size(180, 22);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // recentFilesToolStripMenuItem
            // 
            recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
            recentFilesToolStripMenuItem.Size = new Size(180, 22);
            recentFilesToolStripMenuItem.Text = "Recent files";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(177, 6);
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveToolStripMenuItem.Size = new Size(180, 22);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(180, 22);
            saveAsToolStripMenuItem.Text = "Save As…";
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(177, 6);
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(180, 22);
            settingsToolStripMenuItem.Text = "Settings";
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new Size(177, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(180, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // gridActions
            // 
            gridActions.AllowUserToAddRows = false;
            gridActions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridActions.Columns.AddRange(new DataGridViewColumn[] { colNo, colIcon, colAction, colValue, colLabel, colComment });
            gridActions.Dock = DockStyle.Fill;
            gridActions.Location = new Point(3, 143);
            gridActions.Name = "gridActions";
            gridActions.RowHeadersVisible = false;
            gridActions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridActions.Size = new Size(1028, 393);
            gridActions.TabIndex = 1;
            // 
            // colNo
            // 
            colNo.DataPropertyName = "No";
            colNo.HeaderText = "#";
            colNo.Name = "colNo";
            colNo.ReadOnly = true;
            colNo.Width = 50;
            // 
            // colIcon
            // 
            colIcon.HeaderText = "";
            colIcon.ImageLayout = DataGridViewImageCellLayout.Zoom;
            colIcon.Name = "colIcon";
            colIcon.ReadOnly = true;
            colIcon.Width = 26;
            // 
            // colAction
            // 
            colAction.DataPropertyName = "Action";
            colAction.HeaderText = "Action";
            colAction.Name = "colAction";
            colAction.ReadOnly = true;
            colAction.Width = 180;
            // 
            // colValue
            // 
            colValue.DataPropertyName = "Value";
            colValue.HeaderText = "Value";
            colValue.Name = "colValue";
            colValue.ReadOnly = true;
            colValue.Width = 160;
            // 
            // colLabel
            // 
            colLabel.DataPropertyName = "Label";
            colLabel.HeaderText = "Label";
            colLabel.Name = "colLabel";
            colLabel.ReadOnly = true;
            colLabel.Width = 200;
            // 
            // colComment
            // 
            colComment.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colComment.DataPropertyName = "Comment";
            colComment.HeaderText = "Comment";
            colComment.Name = "colComment";
            colComment.ReadOnly = true;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblCount, lblSpring, lblTime });
            statusStrip1.Location = new Point(0, 539);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1034, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblCount
            // 
            lblCount.Name = "lblCount";
            lblCount.Size = new Size(54, 17);
            lblCount.Text = "0 actions";
            // 
            // lblSpring
            // 
            lblSpring.Name = "lblSpring";
            lblSpring.Size = new Size(916, 17);
            lblSpring.Spring = true;
            // 
            // lblTime
            // 
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(49, 17);
            lblTime.Text = "00:00:00";
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
            imageList1.TransparentColor = Color.Transparent;
            imageList1.Images.SetKeyName(0, "Mouse");
            imageList1.Images.SetKeyName(1, "Keyboard");
            imageList1.Images.SetKeyName(2, "Wait");
            imageList1.Images.SetKeyName(3, "Image");
            imageList1.Images.SetKeyName(4, "Misc");
            // 
            // imageListToolStrip
            // 
            imageListToolStrip.ColorDepth = ColorDepth.Depth32Bit;
            imageListToolStrip.ImageStream = (ImageListStreamer)resources.GetObject("imageListToolStrip.ImageStream");
            imageListToolStrip.TransparentColor = Color.Transparent;
            imageListToolStrip.Images.SetKeyName(0, "Play");
            imageListToolStrip.Images.SetKeyName(1, "Record");
            imageListToolStrip.Images.SetKeyName(2, "Mouse");
            imageListToolStrip.Images.SetKeyName(3, "Mouse");
            imageListToolStrip.Images.SetKeyName(4, "Keyboard");
            imageListToolStrip.Images.SetKeyName(5, "Wait");
            imageListToolStrip.Images.SetKeyName(6, "Image");
            imageListToolStrip.Images.SetKeyName(7, "Misc");
            imageListToolStrip.Images.SetKeyName(8, "Edit");
            imageListToolStrip.Images.SetKeyName(9, "Delete");
            imageListToolStrip.Images.SetKeyName(10, "Search");
            // 
            // cmsActions
            // 
            cmsActions.BackColor = SystemColors.Control;
            cmsActions.Items.AddRange(new ToolStripItem[] { mnuCopy, mnuCut, mnuPaste, ToolStripSeparator, mnuDelete });
            cmsActions.Name = "cmsActions";
            cmsActions.Size = new Size(143, 98);
            // 
            // mnuCopy
            // 
            mnuCopy.Name = "mnuCopy";
            mnuCopy.ShortcutKeyDisplayString = "Ctrl+C";
            mnuCopy.Size = new Size(142, 22);
            mnuCopy.Text = "Copy";
            // 
            // mnuCut
            // 
            mnuCut.Name = "mnuCut";
            mnuCut.ShortcutKeyDisplayString = "Ctrl+X";
            mnuCut.Size = new Size(142, 22);
            mnuCut.Text = "Cut";
            // 
            // mnuPaste
            // 
            mnuPaste.Name = "mnuPaste";
            mnuPaste.ShortcutKeyDisplayString = "Ctrl+V";
            mnuPaste.Size = new Size(142, 22);
            mnuPaste.Text = "Paste";
            // 
            // mnuDelete
            // 
            mnuDelete.Name = "mnuDelete";
            mnuDelete.ShortcutKeyDisplayString = "Del";
            mnuDelete.Size = new Size(142, 22);
            mnuDelete.Text = "Delete";
            // 
            // ToolStripSeparator
            // 
            ToolStripSeparator.Name = "ToolStripSeparator";
            ToolStripSeparator.Size = new Size(139, 6);
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1034, 561);
            Controls.Add(tableLayoutPanel1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Form1";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            pnlRibbon.ResumeLayout(false);
            pnlRibbon.PerformLayout();
            tabRibbon.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            tsRecordEdit.ResumeLayout(false);
            tsRecordEdit.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridActions).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            cmsActions.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private Panel pnlRibbon;
        private DataGridView gridActions;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblCount;
        private ToolStripStatusLabel lblSpring;
        private ToolStripStatusLabel lblTime;
        private ImageList imageList1;
        private DataGridViewTextBoxColumn colNo;
        private DataGridViewImageColumn colIcon;
        private DataGridViewTextBoxColumn colAction;
        private DataGridViewTextBoxColumn colValue;
        private DataGridViewTextBoxColumn colLabel;
        private DataGridViewTextBoxColumn colComment;
        private ImageList imageListToolStrip;
        private TabControl tabRibbon;
        private TabPage tabPage2;   
        private ToolStrip tsRecordEdit;
        private ToolStripSplitButton tsbPlay;
        private ToolStripSplitButton tsbRecord;
        private ToolStripSplitButton tsbStop;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripDropDownButton tsdMouse;
        private ToolStripDropDownButton tsdTextKey;
        private ToolStripDropDownButton tsdWait;
        private ToolStripDropDownButton tsdImageOcr;
        private ToolStripDropDownButton tsdMisc;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton tsbEdit;
        private ToolStripButton tsbDelete;
        private ToolStripButton tsbSearchReplace;
        private TabPage tabPage3;
        private TabPage tabPage4;
        private TabPage tabPage5;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem recentFilesToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem3;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ContextMenuStrip cmsActions;
        private ToolStripMenuItem mnuCopy;
        private ToolStripMenuItem mnuCut;
        private ToolStripMenuItem mnuPaste;
        private ToolStripSeparator ToolStripSeparator;
        private ToolStripMenuItem mnuDelete;
    }
}
