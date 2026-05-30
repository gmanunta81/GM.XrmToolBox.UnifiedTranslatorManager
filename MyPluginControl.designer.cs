namespace GM.XrmToolBox.UniversalTranslationManager
{
    partial class MyPluginControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.toolStripMain = new System.Windows.Forms.ToolStrip();
            this.tsbLoadContext = new System.Windows.Forms.ToolStripButton();
            this.tsbLoadRows = new System.Windows.Forms.ToolStripButton();
            this.tsbSaveChanges = new System.Windows.Forms.ToolStripButton();
            this.tsbPublishChanged = new System.Windows.Forms.ToolStripButton();
            this.tsbPublishAll = new System.Windows.Forms.ToolStripButton();
            this.tsbHelp = new System.Windows.Forms.ToolStripButton();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparatorLeft = new System.Windows.Forms.ToolStripSeparator();
            this.tslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripSeparatorMiddle = new System.Windows.Forms.ToolStripSeparator();
            this.tslCounter = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparatorRight = new System.Windows.Forms.ToolStripSeparator();
            this.btnAbout = new System.Windows.Forms.ToolStripButton();
            this.grpSource = new System.Windows.Forms.GroupBox();
            this.btnLoadRows = new System.Windows.Forms.Button();
            this.btnRefreshContext = new System.Windows.Forms.Button();
            this.cmbTargetLanguage = new System.Windows.Forms.ComboBox();
            this.lblTargetLanguage = new System.Windows.Forms.Label();
            this.cmbSourceLanguage = new System.Windows.Forms.ComboBox();
            this.lblSourceLanguage = new System.Windows.Forms.Label();
            this.cmbSolutions = new System.Windows.Forms.ComboBox();
            this.lblSolution = new System.Windows.Forms.Label();
            this.cmbScope = new System.Windows.Forms.ComboBox();
            this.lblScope = new System.Windows.Forms.Label();
            this.lblScopeWarning = new System.Windows.Forms.Label();
            this.grpFilters = new System.Windows.Forms.GroupBox();
            this.btnApplyFilters = new System.Windows.Forms.Button();
            this.chkOnlyMissingTarget = new System.Windows.Forms.CheckBox();
            this.chkOnlyModified = new System.Windows.Forms.CheckBox();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.cmbProperty = new System.Windows.Forms.ComboBox();
            this.lblProperty = new System.Windows.Forms.Label();
            this.cmbEntity = new System.Windows.Forms.ComboBox();
            this.lblEntity = new System.Windows.Forms.Label();
            this.grpBulk = new System.Windows.Forms.GroupBox();
            this.btnClearTarget = new System.Windows.Forms.Button();
            this.btnFillEmptyFromSource = new System.Windows.Forms.Button();
            this.btnCopySourceToTarget = new System.Windows.Forms.Button();
            this.btnImportExcel = new System.Windows.Forms.Button();
            this.btnExportExcel = new System.Windows.Forms.Button();
            this.btnApplyValue = new System.Windows.Forms.Button();
            this.txtBulkValue = new System.Windows.Forms.TextBox();
            this.lblBulkValue = new System.Windows.Forms.Label();
            this.dgvTranslations = new System.Windows.Forms.DataGridView();
            this.toolStripMain.SuspendLayout();
            this.grpSource.SuspendLayout();
            this.grpFilters.SuspendLayout();
            this.grpBulk.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTranslations)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStripMain
            // 
            this.toolStripMain.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStripMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbLoadContext,
            this.tsbLoadRows,
            this.tsbSaveChanges,
            this.tsbPublishChanged,
            this.tsbPublishAll,
            this.tsbHelp,
            this.tsbClose,
            this.toolStripSeparatorLeft,
            this.tslStatus,
            this.toolStripSeparatorMiddle,
            this.tslCounter,
            this.toolStripSeparatorRight,
            this.btnAbout});
            this.toolStripMain.Location = new System.Drawing.Point(0, 0);
            this.toolStripMain.Name = "toolStripMain";
            this.toolStripMain.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.toolStripMain.Size = new System.Drawing.Size(1700, 27);
            this.toolStripMain.TabIndex = 0;
            // 
            // tsbLoadContext
            // 
            this.tsbLoadContext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoadContext.Name = "tsbLoadContext";
            this.tsbLoadContext.Size = new System.Drawing.Size(78, 24);
            this.tsbLoadContext.Text = "Load context";
            this.tsbLoadContext.Click += new System.EventHandler(this.tsbLoadContext_Click);
            // 
            // tsbLoadRows
            // 
            this.tsbLoadRows.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoadRows.Name = "tsbLoadRows";
            this.tsbLoadRows.Size = new System.Drawing.Size(67, 24);
            this.tsbLoadRows.Text = "Load session";
            this.tsbLoadRows.Click += new System.EventHandler(this.tsbLoadRows_Click);
            // 
            // tsbSaveChanges
            // 
            this.tsbSaveChanges.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbSaveChanges.Name = "tsbSaveChanges";
            this.tsbSaveChanges.Size = new System.Drawing.Size(84, 24);
            this.tsbSaveChanges.Text = "Save changes";
            this.tsbSaveChanges.Click += new System.EventHandler(this.tsbSaveChanges_Click);
            // 
            // tsbPublishChanged
            // 
            this.tsbPublishChanged.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbPublishChanged.Name = "tsbPublishChanged";
            this.tsbPublishChanged.Size = new System.Drawing.Size(97, 24);
            this.tsbPublishChanged.Text = "Publish changed";
            this.tsbPublishChanged.Click += new System.EventHandler(this.tsbPublishChanged_Click);
            // 
            // tsbPublishAll
            // 
            this.tsbPublishAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbPublishAll.Name = "tsbPublishAll";
            this.tsbPublishAll.Size = new System.Drawing.Size(66, 24);
            this.tsbPublishAll.Text = "Publish all";
            this.tsbPublishAll.Click += new System.EventHandler(this.tsbPublishAll_Click);
            // 
            // tsbHelp
            // 
            this.tsbHelp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbHelp.Name = "tsbHelp";
            this.tsbHelp.Size = new System.Drawing.Size(40, 24);
            this.tsbHelp.Text = "Help";
            this.tsbHelp.ToolTipText = "Show step-by-step instructions";
            this.tsbHelp.Click += new System.EventHandler(this.tsbHelp_Click);
            // 
            // tsbClose
            // 
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Size = new System.Drawing.Size(40, 24);
            this.tsbClose.Text = "Close";
            this.tsbClose.Click += new System.EventHandler(this.tsbClose_Click);
            // 
            // toolStripSeparatorLeft
            // 
            this.toolStripSeparatorLeft.Name = "toolStripSeparatorLeft";
            this.toolStripSeparatorLeft.Size = new System.Drawing.Size(6, 27);
            // 
            // tslStatus
            // 
            this.tslStatus.Name = "tslStatus";
            this.tslStatus.Size = new System.Drawing.Size(39, 24);
            this.tslStatus.Spring = true;
            this.tslStatus.Text = "Ready";
            // 
            // toolStripSeparatorMiddle
            // 
            this.toolStripSeparatorMiddle.Name = "toolStripSeparatorMiddle";
            this.toolStripSeparatorMiddle.Size = new System.Drawing.Size(6, 27);
            // 
            // tslCounter
            // 
            this.tslCounter.Name = "tslCounter";
            this.tslCounter.Size = new System.Drawing.Size(51, 24);
            this.tslCounter.Text = "Rows: 0";
            // 
            // toolStripSeparatorRight
            // 
            this.toolStripSeparatorRight.Name = "toolStripSeparatorRight";
            this.toolStripSeparatorRight.Size = new System.Drawing.Size(6, 27);
            // 
            // btnAbout
            // 
            this.btnAbout.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnAbout.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnAbout.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(23, 24);
            this.btnAbout.Text = "About";
            this.btnAbout.ToolTipText = "About";
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            // 
            // grpSource
            // 
            this.grpSource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpSource.Controls.Add(this.btnLoadRows);
            this.grpSource.Controls.Add(this.btnRefreshContext);
            this.grpSource.Controls.Add(this.cmbTargetLanguage);
            this.grpSource.Controls.Add(this.lblTargetLanguage);
            this.grpSource.Controls.Add(this.cmbSourceLanguage);
            this.grpSource.Controls.Add(this.lblSourceLanguage);
            this.grpSource.Controls.Add(this.cmbSolutions);
            this.grpSource.Controls.Add(this.lblSolution);
            this.grpSource.Controls.Add(this.cmbScope);
            this.grpSource.Controls.Add(this.lblScope);
            this.grpSource.Location = new System.Drawing.Point(12, 38);
            this.grpSource.Name = "grpSource";
            this.grpSource.Size = new System.Drawing.Size(1676, 90);
            this.grpSource.TabIndex = 1;
            this.grpSource.TabStop = false;
            this.grpSource.Text = "Load session";
            // 
            // btnLoadRows
            // 
            this.btnLoadRows.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLoadRows.Location = new System.Drawing.Point(1540, 51);
            this.btnLoadRows.Name = "btnLoadRows";
            this.btnLoadRows.Size = new System.Drawing.Size(120, 28);
            this.btnLoadRows.TabIndex = 9;
            this.btnLoadRows.Text = "Load session";
            this.btnLoadRows.UseVisualStyleBackColor = true;
            this.btnLoadRows.Click += new System.EventHandler(this.btnLoadRows_Click);
            // 
            // btnRefreshContext
            // 
            this.btnRefreshContext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefreshContext.Location = new System.Drawing.Point(1540, 19);
            this.btnRefreshContext.Name = "btnRefreshContext";
            this.btnRefreshContext.Size = new System.Drawing.Size(120, 28);
            this.btnRefreshContext.TabIndex = 8;
            this.btnRefreshContext.Text = "Refresh context";
            this.btnRefreshContext.UseVisualStyleBackColor = true;
            this.btnRefreshContext.Click += new System.EventHandler(this.btnRefreshContext_Click);
            // 
            // cmbTargetLanguage
            // 
            this.cmbTargetLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.cmbTargetLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTargetLanguage.FormattingEnabled = true;
            this.cmbTargetLanguage.Location = new System.Drawing.Point(1040, 53);
            this.cmbTargetLanguage.Name = "cmbTargetLanguage";
            this.cmbTargetLanguage.Size = new System.Drawing.Size(370, 21);
            this.cmbTargetLanguage.TabIndex = 7;
            // 
            // lblTargetLanguage
            // 
            this.lblTargetLanguage.AutoSize = true;
            this.lblTargetLanguage.Location = new System.Drawing.Point(950, 56);
            this.lblTargetLanguage.Name = "lblTargetLanguage";
            this.lblTargetLanguage.Size = new System.Drawing.Size(84, 13);
            this.lblTargetLanguage.TabIndex = 6;
            this.lblTargetLanguage.Text = "Target language";
            // 
            // cmbSourceLanguage
            // 
            this.cmbSourceLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.cmbSourceLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSourceLanguage.FormattingEnabled = true;
            this.cmbSourceLanguage.Location = new System.Drawing.Point(562, 53);
            this.cmbSourceLanguage.Name = "cmbSourceLanguage";
            this.cmbSourceLanguage.Size = new System.Drawing.Size(370, 21);
            this.cmbSourceLanguage.TabIndex = 5;
            // 
            // lblSourceLanguage
            // 
            this.lblSourceLanguage.AutoSize = true;
            this.lblSourceLanguage.Location = new System.Drawing.Point(470, 56);
            this.lblSourceLanguage.Name = "lblSourceLanguage";
            this.lblSourceLanguage.Size = new System.Drawing.Size(86, 13);
            this.lblSourceLanguage.TabIndex = 4;
            this.lblSourceLanguage.Text = "Source language";
            // 
            // cmbSolutions
            // 
            this.cmbSolutions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.cmbSolutions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cmbSolutions.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbSolutions.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbSolutions.FormattingEnabled = true;
            this.cmbSolutions.Location = new System.Drawing.Point(86, 53);
            this.cmbSolutions.Name = "cmbSolutions";
            this.cmbSolutions.Size = new System.Drawing.Size(370, 21);
            this.cmbSolutions.TabIndex = 3;
            // 
            // lblSolution
            // 
            this.lblSolution.AutoSize = true;
            this.lblSolution.Location = new System.Drawing.Point(22, 56);
            this.lblSolution.Name = "lblSolution";
            this.lblSolution.Size = new System.Drawing.Size(45, 13);
            this.lblSolution.TabIndex = 2;
            this.lblSolution.Text = "Solution";
            // 
            // cmbScope
            // 
            this.cmbScope.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbScope.FormattingEnabled = true;
            this.cmbScope.Location = new System.Drawing.Point(86, 24);
            this.cmbScope.Name = "cmbScope";
            this.cmbScope.Size = new System.Drawing.Size(370, 21);
            this.cmbScope.TabIndex = 1;
            this.cmbScope.SelectedIndexChanged += new System.EventHandler(this.cmbScope_SelectedIndexChanged);
            // 
            // lblScope
            // 
            this.lblScope.AutoSize = true;
            this.lblScope.Location = new System.Drawing.Point(22, 27);
            this.lblScope.Name = "lblScope";
            this.lblScope.Size = new System.Drawing.Size(38, 13);
            this.lblScope.TabIndex = 0;
            this.lblScope.Text = "Mode";
            // 
            // lblScopeWarning
            // 
            this.lblScopeWarning.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblScopeWarning.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblScopeWarning.Location = new System.Drawing.Point(12, 136);
            this.lblScopeWarning.Name = "lblScopeWarning";
            this.lblScopeWarning.Padding = new System.Windows.Forms.Padding(8, 5, 8, 5);
            this.lblScopeWarning.Size = new System.Drawing.Size(1676, 36);
            this.lblScopeWarning.TabIndex = 2;
            this.lblScopeWarning.Text = "Hybrid engine information";
            this.lblScopeWarning.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpFilters
            // 
            this.grpFilters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpFilters.Controls.Add(this.btnApplyFilters);
            this.grpFilters.Controls.Add(this.chkOnlyMissingTarget);
            this.grpFilters.Controls.Add(this.chkOnlyModified);
            this.grpFilters.Controls.Add(this.txtSearch);
            this.grpFilters.Controls.Add(this.lblSearch);
            this.grpFilters.Controls.Add(this.cmbProperty);
            this.grpFilters.Controls.Add(this.lblProperty);
            this.grpFilters.Controls.Add(this.cmbEntity);
            this.grpFilters.Controls.Add(this.lblEntity);
            this.grpFilters.Location = new System.Drawing.Point(12, 180);
            this.grpFilters.Name = "grpFilters";
            this.grpFilters.Size = new System.Drawing.Size(1676, 82);
            this.grpFilters.TabIndex = 3;
            this.grpFilters.TabStop = false;
            this.grpFilters.Text = "Filters";
            // 
            // btnApplyFilters
            // 
            this.btnApplyFilters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApplyFilters.Location = new System.Drawing.Point(1540, 31);
            this.btnApplyFilters.Name = "btnApplyFilters";
            this.btnApplyFilters.Size = new System.Drawing.Size(120, 28);
            this.btnApplyFilters.TabIndex = 8;
            this.btnApplyFilters.Text = "Apply filters";
            this.btnApplyFilters.UseVisualStyleBackColor = true;
            this.btnApplyFilters.Click += new System.EventHandler(this.btnApplyFilters_Click);
            // 
            // chkOnlyMissingTarget
            // 
            this.chkOnlyMissingTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkOnlyMissingTarget.AutoSize = true;
            this.chkOnlyMissingTarget.Location = new System.Drawing.Point(1200, 38);
            this.chkOnlyMissingTarget.Name = "chkOnlyMissingTarget";
            this.chkOnlyMissingTarget.Size = new System.Drawing.Size(116, 17);
            this.chkOnlyMissingTarget.TabIndex = 7;
            this.chkOnlyMissingTarget.Text = "Only missing target";
            this.chkOnlyMissingTarget.UseVisualStyleBackColor = true;
            this.chkOnlyMissingTarget.CheckedChanged += new System.EventHandler(this.chkOnlyMissingTarget_CheckedChanged);
            // 
            // chkOnlyModified
            // 
            this.chkOnlyModified.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkOnlyModified.AutoSize = true;
            this.chkOnlyModified.Location = new System.Drawing.Point(1324, 38);
            this.chkOnlyModified.Name = "chkOnlyModified";
            this.chkOnlyModified.Size = new System.Drawing.Size(89, 17);
            this.chkOnlyModified.TabIndex = 6;
            this.chkOnlyModified.Text = "Only modified";
            this.chkOnlyModified.UseVisualStyleBackColor = true;
            this.chkOnlyModified.CheckedChanged += new System.EventHandler(this.chkOnlyModified_CheckedChanged);
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.Location = new System.Drawing.Point(706, 36);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(470, 20);
            this.txtSearch.TabIndex = 5;
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSearch_KeyDown);
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(660, 39);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(41, 13);
            this.lblSearch.TabIndex = 4;
            this.lblSearch.Text = "Search";
            // 
            // cmbProperty
            // 
            this.cmbProperty.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProperty.FormattingEnabled = true;
            this.cmbProperty.Location = new System.Drawing.Point(362, 36);
            this.cmbProperty.Name = "cmbProperty";
            this.cmbProperty.Size = new System.Drawing.Size(280, 21);
            this.cmbProperty.TabIndex = 3;
            this.cmbProperty.SelectedIndexChanged += new System.EventHandler(this.cmbProperty_SelectedIndexChanged);
            // 
            // lblProperty
            // 
            this.lblProperty.AutoSize = true;
            this.lblProperty.Location = new System.Drawing.Point(304, 39);
            this.lblProperty.Name = "lblProperty";
            this.lblProperty.Size = new System.Drawing.Size(46, 13);
            this.lblProperty.TabIndex = 2;
            this.lblProperty.Text = "Property";
            // 
            // cmbEntity
            // 
            this.cmbEntity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cmbEntity.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbEntity.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbEntity.FormattingEnabled = true;
            this.cmbEntity.Location = new System.Drawing.Point(86, 36);
            this.cmbEntity.Name = "cmbEntity";
            this.cmbEntity.Size = new System.Drawing.Size(200, 21);
            this.cmbEntity.TabIndex = 1;
            this.cmbEntity.SelectedIndexChanged += new System.EventHandler(this.cmbEntity_SelectedIndexChanged);
            // 
            // lblEntity
            // 
            this.lblEntity.AutoSize = true;
            this.lblEntity.Location = new System.Drawing.Point(22, 39);
            this.lblEntity.Name = "lblEntity";
            this.lblEntity.Size = new System.Drawing.Size(33, 13);
            this.lblEntity.TabIndex = 0;
            this.lblEntity.Text = "Entity";
            // 
            // grpBulk
            // 
            this.grpBulk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpBulk.Controls.Add(this.btnClearTarget);
            this.grpBulk.Controls.Add(this.btnFillEmptyFromSource);
            this.grpBulk.Controls.Add(this.btnCopySourceToTarget);
            this.grpBulk.Controls.Add(this.btnApplyValue);
            this.grpBulk.Controls.Add(this.btnExportExcel);
            this.grpBulk.Controls.Add(this.btnImportExcel);
            this.grpBulk.Controls.Add(this.txtBulkValue);
            this.grpBulk.Controls.Add(this.lblBulkValue);
            this.grpBulk.Location = new System.Drawing.Point(12, 270);
            this.grpBulk.Name = "grpBulk";
            this.grpBulk.Size = new System.Drawing.Size(1676, 76);
            this.grpBulk.TabIndex = 4;
            this.grpBulk.TabStop = false;
            this.grpBulk.Text = "Bulk edit";
            // 
            // btnClearTarget
            // 
            this.btnClearTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearTarget.Location = new System.Drawing.Point(1520, 30);
            this.btnClearTarget.Name = "btnClearTarget";
            this.btnClearTarget.Size = new System.Drawing.Size(140, 28);
            this.btnClearTarget.TabIndex = 6;
            this.btnClearTarget.Text = "Clear target";
            this.btnClearTarget.UseVisualStyleBackColor = true;
            this.btnClearTarget.Click += new System.EventHandler(this.btnClearTarget_Click);
            // 
            // btnFillEmptyFromSource
            // 
            this.btnFillEmptyFromSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFillEmptyFromSource.Location = new System.Drawing.Point(1350, 30);
            this.btnFillEmptyFromSource.Name = "btnFillEmptyFromSource";
            this.btnFillEmptyFromSource.Size = new System.Drawing.Size(160, 28);
            this.btnFillEmptyFromSource.TabIndex = 5;
            this.btnFillEmptyFromSource.Text = "Fill empty from source";
            this.btnFillEmptyFromSource.UseVisualStyleBackColor = true;
            this.btnFillEmptyFromSource.Click += new System.EventHandler(this.btnFillEmptyFromSource_Click);
            // 
            // btnCopySourceToTarget
            // 
            this.btnCopySourceToTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopySourceToTarget.Location = new System.Drawing.Point(1180, 30);
            this.btnCopySourceToTarget.Name = "btnCopySourceToTarget";
            this.btnCopySourceToTarget.Size = new System.Drawing.Size(160, 28);
            this.btnCopySourceToTarget.TabIndex = 4;
            this.btnCopySourceToTarget.Text = "Copy source to target";
            this.btnCopySourceToTarget.UseVisualStyleBackColor = true;
            this.btnCopySourceToTarget.Click += new System.EventHandler(this.btnCopySourceToTarget_Click);
            // 
            // btnImportExcel
            // 
            this.btnImportExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImportExcel.Location = new System.Drawing.Point(830, 30);
            this.btnImportExcel.Name = "btnImportExcel";
            this.btnImportExcel.Size = new System.Drawing.Size(160, 28);
            this.btnImportExcel.TabIndex = 2;
            this.btnImportExcel.Text = "Import from Excel";
            this.btnImportExcel.UseVisualStyleBackColor = true;
            this.btnImportExcel.Click += new System.EventHandler(this.btnImportExcel_Click);
            // 
            // btnExportExcel
            // 
            this.btnExportExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportExcel.Location = new System.Drawing.Point(660, 30);
            this.btnExportExcel.Name = "btnExportExcel";
            this.btnExportExcel.Size = new System.Drawing.Size(160, 28);
            this.btnExportExcel.TabIndex = 7;
            this.btnExportExcel.Text = "Export to Excel";
            this.btnExportExcel.UseVisualStyleBackColor = true;
            this.btnExportExcel.Click += new System.EventHandler(this.btnExportExcel_Click);
            // 
            // btnApplyValue
            // 
            this.btnApplyValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApplyValue.Location = new System.Drawing.Point(1010, 30);
            this.btnApplyValue.Name = "btnApplyValue";
            this.btnApplyValue.Size = new System.Drawing.Size(150, 28);
            this.btnApplyValue.TabIndex = 3;
            this.btnApplyValue.Text = "Apply manual value";
            this.btnApplyValue.UseVisualStyleBackColor = true;
            this.btnApplyValue.Click += new System.EventHandler(this.btnApplyValue_Click);
            // 
            // txtBulkValue
            // 
            this.txtBulkValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBulkValue.Location = new System.Drawing.Point(86, 35);
            this.txtBulkValue.Name = "txtBulkValue";
            this.txtBulkValue.Size = new System.Drawing.Size(550, 20);
            this.txtBulkValue.TabIndex = 1;
            // 
            // lblBulkValue
            // 
            this.lblBulkValue.AutoSize = true;
            this.lblBulkValue.Location = new System.Drawing.Point(22, 38);
            this.lblBulkValue.Name = "lblBulkValue";
            this.lblBulkValue.Size = new System.Drawing.Size(58, 13);
            this.lblBulkValue.TabIndex = 0;
            this.lblBulkValue.Text = "Manual value";
            // 
            // dgvTranslations
            // 
            this.dgvTranslations.AllowUserToAddRows = false;
            this.dgvTranslations.AllowUserToDeleteRows = false;
            this.dgvTranslations.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvTranslations.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTranslations.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.dgvTranslations.Location = new System.Drawing.Point(12, 355);
            this.dgvTranslations.MultiSelect = true;
            this.dgvTranslations.Name = "dgvTranslations";
            this.dgvTranslations.RowHeadersWidth = 51;
            this.dgvTranslations.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTranslations.Size = new System.Drawing.Size(1676, 533);
            this.dgvTranslations.TabIndex = 5;
            this.dgvTranslations.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvTranslations_CellEndEdit);
            this.dgvTranslations.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvTranslations_CellFormatting);
            this.dgvTranslations.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvTranslations_CellContentClick);
            this.dgvTranslations.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.dgvTranslations_EditingControlShowing);
            this.dgvTranslations.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.dgvTranslations_DataBindingComplete);
            // 
            // MyPluginControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvTranslations);
            this.Controls.Add(this.grpBulk);
            this.Controls.Add(this.grpFilters);
            this.Controls.Add(this.lblScopeWarning);
            this.Controls.Add(this.grpSource);
            this.Controls.Add(this.toolStripMain);
            this.Name = "MyPluginControl";
            this.Size = new System.Drawing.Size(1700, 900);
            this.Load += new System.EventHandler(this.MyPluginControl_Load);
            this.toolStripMain.ResumeLayout(false);
            this.toolStripMain.PerformLayout();
            this.grpSource.ResumeLayout(false);
            this.grpSource.PerformLayout();
            this.grpFilters.ResumeLayout(false);
            this.grpFilters.PerformLayout();
            this.grpBulk.ResumeLayout(false);
            this.grpBulk.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTranslations)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStripMain;
        private System.Windows.Forms.ToolStripButton tsbLoadContext;
        private System.Windows.Forms.ToolStripButton tsbLoadRows;
        private System.Windows.Forms.ToolStripButton tsbSaveChanges;
        private System.Windows.Forms.ToolStripButton tsbPublishChanged;
        private System.Windows.Forms.ToolStripButton tsbPublishAll;
        private System.Windows.Forms.ToolStripButton tsbHelp;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorLeft;
        private System.Windows.Forms.ToolStripStatusLabel tslStatus;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorMiddle;
        private System.Windows.Forms.ToolStripLabel tslCounter;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorRight;
        private System.Windows.Forms.ToolStripButton btnAbout;
        private System.Windows.Forms.GroupBox grpSource;
        private System.Windows.Forms.Button btnLoadRows;
        private System.Windows.Forms.Button btnRefreshContext;
        private System.Windows.Forms.ComboBox cmbTargetLanguage;
        private System.Windows.Forms.Label lblTargetLanguage;
        private System.Windows.Forms.ComboBox cmbSourceLanguage;
        private System.Windows.Forms.Label lblSourceLanguage;
        private System.Windows.Forms.ComboBox cmbSolutions;
        private System.Windows.Forms.Label lblSolution;
        private System.Windows.Forms.ComboBox cmbScope;
        private System.Windows.Forms.Label lblScope;
        private System.Windows.Forms.Label lblScopeWarning;
        private System.Windows.Forms.GroupBox grpFilters;
        private System.Windows.Forms.Button btnApplyFilters;
        private System.Windows.Forms.CheckBox chkOnlyMissingTarget;
        private System.Windows.Forms.CheckBox chkOnlyModified;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.ComboBox cmbProperty;
        private System.Windows.Forms.Label lblProperty;
        private System.Windows.Forms.ComboBox cmbEntity;
        private System.Windows.Forms.Label lblEntity;
        private System.Windows.Forms.GroupBox grpBulk;
        private System.Windows.Forms.Button btnClearTarget;
        private System.Windows.Forms.Button btnFillEmptyFromSource;
        private System.Windows.Forms.Button btnCopySourceToTarget;
        private System.Windows.Forms.Button btnImportExcel;
        private System.Windows.Forms.Button btnExportExcel;
        private System.Windows.Forms.Button btnApplyValue;
        private System.Windows.Forms.TextBox txtBulkValue;
        private System.Windows.Forms.Label lblBulkValue;
        private System.Windows.Forms.DataGridView dgvTranslations;
    }
}
