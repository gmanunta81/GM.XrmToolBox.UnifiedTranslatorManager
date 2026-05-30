using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.ServiceModel;
using System.Xml;
using Microsoft.Xrm.Tooling.Connector;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Args;

namespace GM.XrmToolBox.UniversalTranslationManager
{
    public partial class MyPluginControl : PluginControlBase
    {
        private const string HiddenIdColumn = "__RowId";
        private const string ScopeColumn = "ScopeName";
        private const string EntityColumn = "EntityName";
        private const string ParentColumn = "ParentName";
        private const string KeyColumn = "ItemKey";
        private const string PropertyColumn = "PropertyName";
        private const string SourceColumn = "SourceText";
        private const string TargetColumn = "TargetText";
        private const string ModifiedColumn = "IsModified";

        private readonly Dictionary<Guid, TranslationRowModel> _rowsById = new Dictionary<Guid, TranslationRowModel>();
        private readonly List<SolutionItem> _solutions = new List<SolutionItem>();
        private readonly List<LanguageItem> _languages = new List<LanguageItem>();
        private readonly List<EntityItem> _entities = new List<EntityItem>();

        private TranslationSession _currentSession;
        private PendingPublishScope _pendingPublishScope = new PendingPublishScope();
        private DataTable _gridTable;
        private bool _isApplyingUiChanges;
        private bool _isApplyingFilters;

        public MyPluginControl()
        {
            InitializeComponent();
            EnableDoubleBuffer(dgvTranslations);
            HideManualBulkEditControls();
        }

        private void HideManualBulkEditControls()
        {
            // Workflow is now Export -> edit in Excel -> Import. Hide the manual/bulk edit controls
            // so only Export/Import remain visible in the bulk panel.
            lblBulkValue.Visible = false;
            txtBulkValue.Visible = false;
            btnApplyValue.Visible = false;
            btnCopySourceToTarget.Visible = false;
            btnFillEmptyFromSource.Visible = false;
            btnClearTarget.Visible = false;
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            InitializeStaticUi();
            LoadAboutButtonIcon();
            UpdateUiState();
            UpdateStatus("Connect to Dataverse, load the context, then load translations from the UI.");
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            _currentSession = null;
            _pendingPublishScope.Clear();
            _gridTable = null;
            _rowsById.Clear();
            dgvTranslations.DataSource = null;
            UpdateUiState();

            if (detail != null)
            {
                UpdateStatus("Connected to " + detail.ConnectionName + ". Load the environment context to start.");
                LogInfo("Connection switched to {0}", detail.ConnectionName);
            }
            else
            {
                UpdateStatus("Connection updated.");
            }
        }

        private void InitializeStaticUi()
        {
            _isApplyingUiChanges = true;

            try
            {
                cmbScope.DataSource = Enum.GetValues(typeof(TranslationScope))
                    .Cast<TranslationScope>()
                    .Select(scope => new ScopeOption { Value = scope, DisplayName = scope.GetDisplayName() })
                    .ToList();
                cmbScope.DisplayMember = nameof(ScopeOption.DisplayName);
                cmbScope.ValueMember = nameof(ScopeOption.Value);
                cmbScope.SelectedIndex = 0;

                cmbSolutions.DataSource = new List<SolutionItem> { SolutionItem.CreateAll() };
                cmbSolutions.DisplayMember = nameof(SolutionItem.DisplayName);
                cmbSolutions.ValueMember = nameof(SolutionItem.UniqueName);

                cmbSourceLanguage.DataSource = null;
                cmbTargetLanguage.DataSource = null;

                cmbEntity.DataSource = new List<EntityItem> { EntityItem.CreateAll() };
                cmbEntity.DisplayMember = nameof(EntityItem.DisplayName);
                cmbEntity.ValueMember = nameof(EntityItem.LogicalName);

                cmbProperty.DataSource = new List<FilterItem> { FilterItem.CreateAll() };
                cmbProperty.DisplayMember = nameof(FilterItem.DisplayName);
                cmbProperty.ValueMember = nameof(FilterItem.Value);

                txtSearch.Text = string.Empty;
                txtBulkValue.Text = string.Empty;
                chkOnlyMissingTarget.Checked = false;
                chkOnlyModified.Checked = false;

                ConfigureGridColumns();
                UpdateScopeWarning();
            }
            finally
            {
                _isApplyingUiChanges = false;
            }
        }

        private void ConfigureGridColumns()
        {
            dgvTranslations.AutoGenerateColumns = false;
            dgvTranslations.Columns.Clear();
            dgvTranslations.EditMode = DataGridViewEditMode.EditOnEnter;

            dgvTranslations.Columns.Add(CreateTextColumn(HiddenIdColumn, HiddenIdColumn, 20, true, true, false));
            dgvTranslations.Columns[HiddenIdColumn].Visible = false;

            dgvTranslations.Columns.Add(CreateTextColumn(ScopeColumn, "Scope", 110, true, true, true));
            dgvTranslations.Columns.Add(CreateTextColumn(EntityColumn, "Entity", 150, true, true, true));
            dgvTranslations.Columns.Add(CreateTextColumn(ParentColumn, "Parent", 180, true, true, true));
            dgvTranslations.Columns.Add(CreateTextColumn(KeyColumn, "Key", 190, true, true, true));
            dgvTranslations.Columns.Add(CreateTextColumn(PropertyColumn, "Property", 130, true, true, true));
            dgvTranslations.Columns.Add(CreateTextColumn(SourceColumn, "Source", 260, true, false, true));
            dgvTranslations.Columns.Add(CreateTextColumn(TargetColumn, "Target", 260, false, false, true));

            DataGridViewCheckBoxColumn modifiedColumn = new DataGridViewCheckBoxColumn
            {
                Name = ModifiedColumn,
                HeaderText = "Modified",
                DataPropertyName = ModifiedColumn,
                Width = 70,
                ReadOnly = true
            };
            dgvTranslations.Columns.Add(modifiedColumn);
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string name, string header, int width, bool readOnly, bool frozen, bool visible)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = name,
                Width = width,
                ReadOnly = readOnly,
                Frozen = frozen,
                Visible = visible,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = { NullValue = string.Empty }
            };

            return column;
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void tsbHelp_Click(object sender, EventArgs e)
        {
            ShowHelpDialog();
        }

        private void ShowHelpDialog()
        {
            const string title = "How to use the Unified Translator Manager";

            string message =
                "SINGLE-RECORD TRANSLATION (edit directly in the grid)" + Environment.NewLine +
                "  1. Click 'Load context' to fetch solutions, languages and entities from the connected environment." + Environment.NewLine +
                "  2. Choose the Mode (suggested: Table + Form + Choices to cover the most common metadata)." + Environment.NewLine +
                "  3. Pick the Solution that contains the components to translate." + Environment.NewLine +
                "     IMPORTANT: always pick a specific solution BEFORE loading the session." + Environment.NewLine +
                "     Tip: create a dedicated unmanaged solution and add only the objects you actually" + Environment.NewLine +
                "     want to translate, so the grid stays focused and short." + Environment.NewLine +
                "     If you leave the solution empty the tool will scan ALL unmanaged metadata of the" + Environment.NewLine +
                "     environment, which can take a VERY long time (minutes up to an hour or more)." + Environment.NewLine +
                "  4. Pick the Source and Target languages (the language you want to translate FROM / INTO)." + Environment.NewLine +
                "  5. Click 'Load session' to load the translatable items into the grid." + Environment.NewLine +
                "  6. Use the search textbox and filter checkboxes (Only modified, Only empty target...)" + Environment.NewLine +
                "     to find the row(s) you want to translate." + Environment.NewLine +
                "  7. Click the Target cell once - it enters edit mode immediately." + Environment.NewLine +
                "     Type the new value (you can also leave it empty to clear it)." + Environment.NewLine +
                "     When you press Tab/Enter or leave the cell, the row turns yellow and the" + Environment.NewLine +
                "     'Modified' checkbox is ticked automatically." + Environment.NewLine +
                "  8. Click 'Save changes' to push your edits to Dataverse." + Environment.NewLine +
                "  9. Click 'Publish changed' to publish only the entities you actually modified" + Environment.NewLine +
                "     (or 'Publish all' if you prefer to publish the whole solution)." + Environment.NewLine +
                Environment.NewLine +
                "BULK TRANSLATION VIA EXCEL (export -> edit in Excel -> import back)" + Environment.NewLine +
                "  1. Click 'Load context'." + Environment.NewLine +
                "  2. Choose the Mode (Table + Form + Choices is the most common combination)." + Environment.NewLine +
                "  3. Pick the Solution FIRST - select the unmanaged solution that holds the components" + Environment.NewLine +
                "     to translate (do not leave it empty unless you really want to load everything," + Environment.NewLine +
                "     which can take a very long time)." + Environment.NewLine +
                "  4. Pick the Source and Target languages." + Environment.NewLine +
                "  5. Click 'Load session' so the grid contains the rows you want to translate." + Environment.NewLine +
                "  6. Apply any filter you want (search text, Only empty target, etc.) so that the grid" + Environment.NewLine +
                "     shows only the rows you intend to send to the translator / edit in Excel." + Environment.NewLine +
                "  7. In the 'Export / Import from Excel file' section click 'Export to Excel'" + Environment.NewLine +
                "     and save the .xlsx file." + Environment.NewLine +
                "  8. Open the file in Excel. Edit only the 'Target' column (do NOT rename or remove" + Environment.NewLine +
                "     the Entity, Key and Property columns - they are used to match the rows back)." + Environment.NewLine +
                "     Save the file (keep the .xlsx format)." + Environment.NewLine +
                "  9. Back in the tool, click 'Import from Excel' and select the file you saved." + Environment.NewLine +
                "     Updated rows turn yellow and their 'Modified' checkbox is ticked." + Environment.NewLine +
                " 10. Review the import summary (changed / unchanged / not found / ambiguous rows)." + Environment.NewLine +
                " 11. Click 'Save changes' and then 'Publish changed' to apply and publish the imports." + Environment.NewLine +
                Environment.NewLine +
                "TIPS" + Environment.NewLine +
                "  - Always pick a Solution before clicking 'Load session'. Loading 'All unmanaged'" + Environment.NewLine +
                "    metadata is supported but can be extremely slow on large environments." + Environment.NewLine +
                "  - Yellow target cells = pending modifications not yet saved to Dataverse." + Environment.NewLine +
                "  - The 'Modified' column is read-only: it ticks automatically when the target value" + Environment.NewLine +
                "    differs from the original. Re-typing the original value un-ticks it." + Environment.NewLine +
                "  - You can repeat the export/import cycle as many times as needed before saving.";

            MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tsbLoadContext_Click(object sender, EventArgs e)
        {
            ExecuteMethod(LoadEnvironmentContext);
        }

        private void btnRefreshContext_Click(object sender, EventArgs e)
        {
            ExecuteMethod(LoadEnvironmentContext);
        }

        private void tsbLoadRows_Click(object sender, EventArgs e)
        {
            ExecuteMethod(LoadSessionFromUi);
        }

        private void btnLoadRows_Click(object sender, EventArgs e)
        {
            ExecuteMethod(LoadSessionFromUi);
        }

        private void tsbSaveChanges_Click(object sender, EventArgs e)
        {
            ExecuteMethod(SaveChangesToDataverse);
        }

        private void tsbPublishChanged_Click(object sender, EventArgs e)
        {
            ExecuteMethod(PublishChangedComponents);
        }

        private void tsbPublishAll_Click(object sender, EventArgs e)
        {
            ExecuteMethod(PublishAllComponents);
        }

        private void cmbScope_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isApplyingUiChanges)
            {
                return;
            }

            UpdateScopeWarning();
        }

        private void UpdateScopeWarning()
        {
            TranslationScope scope = GetSelectedScope();

            if (scope == TranslationScope.Form)
            {
                lblScopeWarning.Text = "Forms scope temporarily switches the current user's UI language while loading and saving labels.";
            }
            else if (scope == TranslationScope.Package)
            {
                lblScopeWarning.Text = "Package scope uses the standard Microsoft translation package internally, fully in memory, without exposing ZIP or Excel files in the UI.";
            }
            else if (scope == TranslationScope.Combined)
            {
                lblScopeWarning.Text = "Combined scope loads Tables, Choices and Forms together. Forms temporarily switch the user's UI language during load and save.";
            }
            else
            {
                lblScopeWarning.Text = "Edits are written directly to Dataverse metadata. Save changes first, then publish only what changed.";
            }
        }

        private void LoadEnvironmentContext()
        {
            if (!EnsureConnected())
            {
                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading environment context...",
                Work = (worker, args) =>
                {
                    args.Result = DataverseHybridTranslationService.LoadContext(Service);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        ShowError(args.Error, "Unable to load environment context");
                        return;
                    }

                    EnvironmentContextInfo context = (EnvironmentContextInfo)args.Result;
                    BindContext(context);

                    UpdateStatus(
                        "Loaded " + context.Solutions.Count.ToString("N0", CultureInfo.InvariantCulture) +
                        " unmanaged solutions, " + context.Entities.Count.ToString("N0", CultureInfo.InvariantCulture) +
                        " entities and " + context.Languages.Count.ToString("N0", CultureInfo.InvariantCulture) + " languages.");
                }
            });
        }

        private void BindContext(EnvironmentContextInfo context)
        {
            _solutions.Clear();
            _solutions.AddRange(context.Solutions);

            _languages.Clear();
            _languages.AddRange(context.Languages);

            _entities.Clear();
            _entities.AddRange(context.Entities);

            _isApplyingUiChanges = true;

            try
            {
                List<SolutionItem> solutionItems = new List<SolutionItem> { SolutionItem.CreateAll() };
                solutionItems.AddRange(_solutions.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase));

                cmbSolutions.DataSource = solutionItems;
                cmbSolutions.DisplayMember = nameof(SolutionItem.DisplayName);
                cmbSolutions.ValueMember = nameof(SolutionItem.UniqueName);

                List<LanguageItem> sourceItems = _languages.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
                List<LanguageItem> targetItems = _languages.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();

                cmbSourceLanguage.DataSource = sourceItems;
                cmbSourceLanguage.DisplayMember = nameof(LanguageItem.DisplayName);
                cmbSourceLanguage.ValueMember = nameof(LanguageItem.Lcid);

                cmbTargetLanguage.DataSource = targetItems;
                cmbTargetLanguage.DisplayMember = nameof(LanguageItem.DisplayName);
                cmbTargetLanguage.ValueMember = nameof(LanguageItem.Lcid);

                LanguageItem baseLanguage = _languages.FirstOrDefault(item => item.IsBaseLanguage);
                if (baseLanguage != null)
                {
                    SelectLanguage(cmbSourceLanguage, baseLanguage.Lcid);
                }

                LanguageItem firstNonBase = _languages.FirstOrDefault(item => !item.IsBaseLanguage) ?? _languages.FirstOrDefault();
                if (firstNonBase != null)
                {
                    SelectLanguage(cmbTargetLanguage, firstNonBase.Lcid);
                }

                BindEntityFilter(_entities);
            }
            finally
            {
                _isApplyingUiChanges = false;
            }

            UpdateUiState();
        }

        private void BindEntityFilter(IEnumerable<EntityItem> entities)
        {
            string previous = GetSelectedEntityLogicalName();

            List<EntityItem> entityItems = new List<EntityItem> { EntityItem.CreateAll() };
            entityItems.AddRange((entities ?? Enumerable.Empty<EntityItem>())
                .Where(item => item != null && !item.IsAll)
                .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase));

            cmbEntity.DataSource = entityItems;
            cmbEntity.DisplayMember = nameof(EntityItem.DisplayName);
            cmbEntity.ValueMember = nameof(EntityItem.LogicalName);

            if (!string.IsNullOrWhiteSpace(previous))
            {
                EntityItem selected = entityItems.FirstOrDefault(item => string.Equals(item.LogicalName, previous, StringComparison.OrdinalIgnoreCase));
                if (selected != null)
                {
                    cmbEntity.SelectedItem = selected;
                }
            }
        }

        private void BindPropertyFilter(IEnumerable<string> properties)
        {
            string previous = GetSelectedPropertyName();

            List<FilterItem> items = new List<FilterItem> { FilterItem.CreateAll() };
            items.AddRange((properties ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .Select(value => new FilterItem { DisplayName = value, Value = value }));

            cmbProperty.DataSource = items;
            cmbProperty.DisplayMember = nameof(FilterItem.DisplayName);
            cmbProperty.ValueMember = nameof(FilterItem.Value);

            if (!string.IsNullOrWhiteSpace(previous))
            {
                FilterItem selected = items.FirstOrDefault(item => string.Equals(item.Value, previous, StringComparison.OrdinalIgnoreCase));
                if (selected != null)
                {
                    cmbProperty.SelectedItem = selected;
                }
            }
        }

        private void SelectLanguage(ComboBox comboBox, int lcid)
        {
            if (comboBox == null || comboBox.Items.Count == 0)
            {
                return;
            }

            foreach (object item in comboBox.Items)
            {
                LanguageItem language = item as LanguageItem;
                if (language != null && language.Lcid == lcid)
                {
                    comboBox.SelectedItem = language;
                    return;
                }
            }
        }

        private void LoadSessionFromUi()
        {
            if (!EnsureConnected())
            {
                return;
            }

            if (!EnsureContextLoaded())
            {
                return;
            }

            if (!ConfirmDiscardUnsavedChanges("load a new translation session"))
            {
                return;
            }

            int? sourceLcid = GetSelectedSourceLanguageLcid();
            int? targetLcid = GetSelectedTargetLanguageLcid();

            if (!sourceLcid.HasValue || !targetLcid.HasValue)
            {
                MessageBox.Show(this, "Select both source and target languages before loading translations.", "Languages required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (sourceLcid.Value == targetLcid.Value)
            {
                MessageBox.Show(this, "The source and target languages must be different.", "Invalid language selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedSolution = GetSelectedSolutionUniqueName();
            if (string.IsNullOrWhiteSpace(selectedSolution))
            {
                int unmanagedSolutionCount = _solutions != null ? _solutions.Count : 0;
                string unmanagedHint = unmanagedSolutionCount > 0
                    ? "Detected unmanaged solutions in this environment: " + unmanagedSolutionCount.ToString("N0", CultureInfo.InvariantCulture) + "."
                    : "All unmanaged metadata in this environment will be inspected.";

                string warningMessage =
                    "No specific solution is selected, so the tool will scan ALL unmanaged metadata in the environment." + Environment.NewLine +
                    Environment.NewLine +
                    unmanagedHint + Environment.NewLine +
                    Environment.NewLine +
                    "Depending on the size of the environment this can take a VERY long time" + Environment.NewLine +
                    "(several minutes up to an hour or more) and may load thousands of rows." + Environment.NewLine +
                    Environment.NewLine +
                    "Strongly recommended:" + Environment.NewLine +
                    "  - Cancel and pick a specific solution from the 'Solution' dropdown, OR" + Environment.NewLine +
                    "  - Create a dedicated unmanaged solution containing only the components you want to translate." + Environment.NewLine +
                    Environment.NewLine +
                    "Do you still want to continue and load EVERYTHING?";

                DialogResult choice = MessageBox.Show(
                    this,
                    warningMessage,
                    "Loading all unmanaged metadata - this may take a very long time",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (choice != DialogResult.Yes)
                {
                    return;
                }
            }

            TranslationLoadRequest request = new TranslationLoadRequest
            {
                Scope = GetSelectedScope(),
                SolutionUniqueName = selectedSolution,
                EntityLogicalName = GetSelectedEntityLogicalName(),
                SourceLcid = sourceLcid.Value,
                TargetLcid = targetLcid.Value
            };

            if (request.Scope == TranslationScope.Package && string.IsNullOrWhiteSpace(request.SolutionUniqueName))
            {
                MessageBox.Show(this, "Package scope requires a specific unmanaged solution. Select a solution and try again.", "Solution required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string progressMessage = request.Scope == TranslationScope.Package
                ? "Loading translations through the hidden Microsoft translation package..."
                : "Loading translations directly from Dataverse...";

            WorkAsync(new WorkAsyncInfo
            {
                Message = progressMessage,
                Work = (worker, args) =>
                {
                    args.Result = DataverseHybridTranslationService.LoadSession(Service, request, worker);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        ShowError(args.Error, "Unable to load translations");
                        return;
                    }

                    _currentSession = (TranslationSession)args.Result;
                    _pendingPublishScope.Clear();
                    BindSession(_currentSession);
                    UpdateUiState();

                    string provider = _currentSession.PackageWorkbook != null
                        ? "the hidden translation package"
                        : "the current environment";

                    UpdateStatus(
                        "Loaded " + _currentSession.Rows.Count.ToString("N0", CultureInfo.InvariantCulture) +
                        " rows for scope '" + _currentSession.Request.Scope.GetDisplayName() +
                        "' from " + provider + ".");
                }
            });
        }

        private void BindSession(TranslationSession session)
        {
            _isApplyingUiChanges = true;

            try
            {
                _rowsById.Clear();
                _gridTable = new DataTable("TranslationRows");

                DataColumn idColumn = _gridTable.Columns.Add(HiddenIdColumn, typeof(string));
                _gridTable.Columns.Add(ScopeColumn, typeof(string));
                _gridTable.Columns.Add(EntityColumn, typeof(string));
                _gridTable.Columns.Add(ParentColumn, typeof(string));
                _gridTable.Columns.Add(KeyColumn, typeof(string));
                _gridTable.Columns.Add(PropertyColumn, typeof(string));
                _gridTable.Columns.Add(SourceColumn, typeof(string));
                _gridTable.Columns.Add(TargetColumn, typeof(string));
                _gridTable.Columns.Add(ModifiedColumn, typeof(bool));
                _gridTable.PrimaryKey = new[] { idColumn };

                _gridTable.BeginLoadData();
                try
                {
                    foreach (TranslationRowModel rowModel in session.Rows)
                    {
                        _rowsById[rowModel.RowId] = rowModel;
                        DataRow row = _gridTable.NewRow();
                        PopulateDataRow(row, rowModel);
                        _gridTable.Rows.Add(row);
                    }
                }
                finally
                {
                    _gridTable.EndLoadData();
                }

                dgvTranslations.DataSource = _gridTable.DefaultView;
                BindPropertyFilter(session.Rows.Select(row => row.PropertyName));
                BindEntityFilter(session.Rows.Select(row => new EntityItem { LogicalName = row.EntityLogicalName, DisplayName = row.EntityFilterDisplayName }).Distinct(new EntityItemComparer()));
            }
            finally
            {
                _isApplyingUiChanges = false;
            }

            ApplyFilters();
        }

        private void PopulateDataRow(DataRow row, TranslationRowModel model)
        {
            row[HiddenIdColumn] = model.RowId.ToString();
            row[ScopeColumn] = model.Scope.GetDisplayName();
            row[EntityColumn] = model.EntityGridDisplayName;
            row[ParentColumn] = model.ParentName;
            row[KeyColumn] = model.GridKey;
            row[PropertyColumn] = model.PropertyName;
            row[SourceColumn] = model.SourceText;
            row[TargetColumn] = model.TargetText;
            row[ModifiedColumn] = model.IsModified;
        }

        private bool ValidateRowsBeforeSave(List<TranslationRowModel> rows)
        {
            TranslationRowModel tooLongPackageRow = rows.FirstOrDefault(row =>
                row.Scope == TranslationScope.Package &&
                !string.IsNullOrWhiteSpace(row.TargetText) &&
                row.TargetText.Length > DataverseHybridTranslationService.MaxTranslationPackageLabelLength);

            if (tooLongPackageRow == null)
            {
                return true;
            }

            MessageBox.Show(
                this,
                "One or more package-based rows exceed the 500 character limit supported by ImportTranslation. Reduce the target text length and try again.\r\n\r\nAffected row key: " + (tooLongPackageRow.GridKey ?? string.Empty),
                "Validation error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return false;
        }

        private void SaveChangesToDataverse()
        {
            if (!EnsureConnected() || !EnsureSessionLoaded())
            {
                return;
            }

            List<TranslationRowModel> changedRows = _currentSession.Rows.Where(row => row.IsModified).ToList();
            if (changedRows.Count == 0)
            {
                MessageBox.Show(this, "There are no modified rows to save.", "Nothing to save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateRowsBeforeSave(changedRows))
            {
                return;
            }

            string progressMessage = _currentSession.PackageWorkbook != null
                ? "Saving translation changes through the hidden translation package..."
                : "Saving translation changes directly to Dataverse...";

            WorkAsync(new WorkAsyncInfo
            {
                Message = progressMessage,
                Work = (worker, args) =>
                {
                    args.Result = DataverseHybridTranslationService.SaveRows(Service, _currentSession, changedRows, worker);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        ShowError(args.Error, "Unable to save changes");
                        return;
                    }

                    TranslationSaveResult result = (TranslationSaveResult)args.Result;

                    _isApplyingUiChanges = true;
                    try
                    {
                        foreach (TranslationRowModel row in changedRows)
                        {
                            row.AcceptChanges();
                            UpdateGridRow(row);
                        }
                    }
                    finally
                    {
                        _isApplyingUiChanges = false;
                    }

                    _pendingPublishScope.Merge(result.PublishScope);
                    ApplyFilters();
                    UpdateUiState();

                    string summaryMessage = string.IsNullOrWhiteSpace(result.SummaryMessage)
                        ? "Publish is still required to see the changes in the app."
                        : result.SummaryMessage;

                    UpdateStatus(
                        "Saved " + result.SavedRows.ToString("N0", CultureInfo.InvariantCulture) +
                        " rows. " + summaryMessage);
                }
            });
        }

        private void PublishChangedComponents()
        {
            if (!EnsureConnected())
            {
                return;
            }

            if (_currentSession != null && _currentSession.Rows.Any(row => row.IsModified))
            {
                MessageBox.Show(this, "Save the current changes before publishing.", "Save required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_pendingPublishScope.IsEmpty)
            {
                MessageBox.Show(this, "There is no pending publish scope. Save some changes first or use Publish All.", "Nothing to publish", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PendingPublishScope scope = _pendingPublishScope.Clone();

            if (scope.RequiresPublishAllFallback)
            {
                string componentList = scope.NonPublishableComponents.Count > 0
                    ? string.Join(", ", scope.NonPublishableComponents.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).Take(6))
                    : "package-based components";

                DialogResult answer = MessageBox.Show(
                    this,
                    "The pending changes include components that do not support a reliable targeted publish from this scope.\r\n\r\nExamples: " + componentList + "\r\n\r\nDo you want to run Publish All now?",
                    "Publish All required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }

            string progressMessage = scope.RequiresPublishAllFallback
                ? "Publishing all customizations..."
                : "Publishing changed components...";

            WorkAsync(new WorkAsyncInfo
            {
                Message = progressMessage,
                Work = (worker, args) =>
                {
                    DataverseHybridTranslationService.PublishScope(Service, scope);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        ShowError(args.Error, "Unable to publish changed components");
                        return;
                    }

                    _pendingPublishScope.Clear();
                    UpdateUiState();
                    UpdateStatus(scope.RequiresPublishAllFallback
                        ? "Publish All completed successfully."
                        : "Targeted publish completed successfully.");
                }
            });
        }

        private void PublishAllComponents()
        {
            if (!EnsureConnected())
            {
                return;
            }

            if (_currentSession != null && _currentSession.Rows.Any(row => row.IsModified))
            {
                MessageBox.Show(this, "Save the current changes before publishing.", "Save required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Publishing all customizations...",
                Work = (worker, args) =>
                {
                    DataverseHybridTranslationService.PublishAll(Service);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        ShowError(args.Error, "Unable to publish all customizations");
                        return;
                    }

                    _pendingPublishScope.Clear();
                    UpdateUiState();
                    UpdateStatus("Publish All completed successfully.");
                }
            });
        }

        private bool ConfirmDiscardUnsavedChanges(string actionDescription)
        {
            if (_currentSession == null || !_currentSession.Rows.Any(row => row.IsModified))
            {
                return true;
            }

            DialogResult result = MessageBox.Show(
                this,
                "The current session contains unsaved changes.\r\n\r\nDo you want to discard them and " + actionDescription + "?",
                "Unsaved changes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            return result == DialogResult.Yes;
        }

        private void btnApplyFilters_Click(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void cmbEntity_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isApplyingUiChanges)
            {
                return;
            }

            if (_currentSession != null)
            {
                ApplyFilters();
            }
        }

        private void cmbProperty_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isApplyingUiChanges)
            {
                return;
            }

            if (_currentSession != null)
            {
                ApplyFilters();
            }
        }

        private void chkOnlyModified_CheckedChanged(object sender, EventArgs e)
        {
            if (_isApplyingUiChanges)
            {
                return;
            }

            if (_currentSession != null)
            {
                ApplyFilters();
            }
        }

        private void chkOnlyMissingTarget_CheckedChanged(object sender, EventArgs e)
        {
            if (_isApplyingUiChanges)
            {
                return;
            }

            if (_currentSession != null)
            {
                ApplyFilters();
            }
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_isApplyingFilters)
            {
                return;
            }

            _isApplyingFilters = true;
            bool savedApplyingUiChanges = _isApplyingUiChanges;
            _isApplyingUiChanges = true;

            try
            {
                if (_currentSession == null || _gridTable == null || _gridTable.Rows.Count == 0)
                {
                    UpdateCounter();
                    return;
                }

                string entityLogicalName = GetSelectedEntityLogicalName();
                string propertyName = GetSelectedPropertyName();
                string search = (txtSearch.Text ?? string.Empty).Trim();
                bool onlyModified = chkOnlyModified.Checked;
                bool onlyMissingTarget = chkOnlyMissingTarget.Checked;

                bool hasEntityFilter = !string.IsNullOrWhiteSpace(entityLogicalName);
                bool hasPropertyFilter = !string.IsNullOrWhiteSpace(propertyName);
                bool hasSearch = !string.IsNullOrWhiteSpace(search);

                // Build the set of visible row IDs using the in-memory dictionary (fast, no UI interaction)
                HashSet<Guid> visibleIds = new HashSet<Guid>();
                foreach (var kvp in _rowsById)
                {
                    TranslationRowModel row = kvp.Value;
                    bool visible = true;

                    if (visible && hasEntityFilter && !string.Equals(row.EntityLogicalName, entityLogicalName, StringComparison.OrdinalIgnoreCase))
                        visible = false;

                    if (visible && hasPropertyFilter && !string.Equals(row.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase))
                        visible = false;

                    if (visible && onlyModified && !row.IsModified)
                        visible = false;

                    if (visible && onlyMissingTarget && !string.IsNullOrWhiteSpace(row.TargetText))
                        visible = false;

                    if (visible && hasSearch && !RowMatchesSearch(row, search))
                        visible = false;

                        if (visible)
                            visibleIds.Add(kvp.Key);
                    }

                    // Clear current cell to avoid selection issues when filtering
                    // Wrapped in try-catch to prevent reentrant call exceptions from DataGridView
                    try
                    {
                        if (dgvTranslations.CurrentCell != null)
                        {
                            dgvTranslations.CurrentCell = null;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore reentrant call exceptions - the filter will still apply correctly
                    }

                    // Apply filter through the DataView – single native operation, no per-row UI work
                    if (visibleIds.Count == _rowsById.Count)
                {
                    _gridTable.DefaultView.RowFilter = string.Empty;
                }
                else if (visibleIds.Count == 0)
                {
                    // Impossible condition that hides all rows
                    _gridTable.DefaultView.RowFilter = HiddenIdColumn + " = '00000000-0000-0000-0000-000000000000' AND " + HiddenIdColumn + " = '00000000-0000-0000-0000-000000000001'";
                }
                else
                {
                    StringBuilder filterBuilder = new StringBuilder();
                    filterBuilder.Append(HiddenIdColumn + " IN (");
                    bool first = true;
                    foreach (Guid id in visibleIds)
                    {
                        if (!first)
                            filterBuilder.Append(',');
                        filterBuilder.Append('\'');
                        filterBuilder.Append(id.ToString());
                        filterBuilder.Append('\'');
                        first = false;
                    }
                    filterBuilder.Append(')');
                    _gridTable.DefaultView.RowFilter = filterBuilder.ToString();
                }

                UpdateCounter();
            }
            finally
            {
                _isApplyingUiChanges = savedApplyingUiChanges;
                _isApplyingFilters = false;
            }
        }

        private static bool RowMatchesSearch(TranslationRowModel row, string search)
        {
            return ContainsIgnoreCase(row.EntityGridDisplayName, search)
                || ContainsIgnoreCase(row.ParentName, search)
                || ContainsIgnoreCase(row.GridKey, search)
                || ContainsIgnoreCase(row.PropertyName, search)
                || ContainsIgnoreCase(row.SourceText, search)
                || ContainsIgnoreCase(row.TargetText, search);
        }

        private static bool ContainsIgnoreCase(string input, string search)
        {
            return !string.IsNullOrWhiteSpace(search)
                && !string.IsNullOrWhiteSpace(input)
                && input.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void btnApplyValue_Click(object sender, EventArgs e)
        {
            if (!EnsureSessionLoaded())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(txtBulkValue.Text))
            {
                MessageBox.Show(this, "Enter a manual value before applying it.", "Bulk value required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ApplyBulkChange("Apply manual value", row => row.TargetText = txtBulkValue.Text);
        }

        private void btnImportExcel_Click(object sender, EventArgs e)
        {
            if (!EnsureSessionLoaded())
            {
                return;
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Select an Excel file containing target translations";
                dialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.Multiselect = false;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    BulkExcelImportResult result = ImportBulkTargetValuesFromExcel(dialog.FileName);
                    ApplyFilters();
                    UpdateUiState();

                    string summary = BuildBulkExcelImportSummary(Path.GetFileName(dialog.FileName), result);
                    UpdateStatus("Excel bulk import completed. Changed rows: " + result.ChangedRows.ToString("N0", CultureInfo.InvariantCulture) + ".");

                    MessageBoxIcon icon = (result.NotFoundRows > 0 || result.AmbiguousRows > 0 || result.InvalidRows > 0)
                        ? MessageBoxIcon.Warning
                        : MessageBoxIcon.Information;

                    MessageBox.Show(this, summary, "Excel bulk import completed", MessageBoxButtons.OK, icon);
                }
                catch (Exception error)
                {
                    ShowError(error, "Unable to import target values from Excel");
                }
            }
        }

        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (!EnsureSessionLoaded())
            {
                return;
            }

            List<TranslationRowModel> rows = GetVisibleRows();
            if (rows.Count == 0)
            {
                MessageBox.Show(this, "There are no visible rows to export.", "Nothing to export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Save translations to an Excel file";
                dialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                dialog.FileName = "Translations_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";
                dialog.OverwritePrompt = true;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    ExportRowsToExcel(rows, dialog.FileName);
                    UpdateStatus("Exported " + rows.Count.ToString("N0", CultureInfo.InvariantCulture) + " rows to " + Path.GetFileName(dialog.FileName) + ".");
                }
                catch (Exception error)
                {
                    ShowError(error, "Unable to export translations to Excel");
                }
            }
        }

        private static readonly string[] ExcelHeaderColumns =
            { "Scope", "Entity", "Parent", "Key", "Property", "Source", "Target" };

        // Writes a minimal but valid .xlsx package using only System.IO.Compression + System.Xml.
        // This avoids any third-party dependency (ClosedXML / OpenXml SDK) and the assembly-binding
        // problems they trigger inside the XrmToolBox host.
        private static void ExportRowsToExcel(List<TranslationRowModel> rows, string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (FileStream fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
            using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                WriteZipEntry(archive, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                    "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                    "</Types>");

                WriteZipEntry(archive, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "</Relationships>");

                WriteZipEntry(archive, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                    "</Relationships>");

                WriteZipEntry(archive, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                    "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                    "<sheets><sheet name=\"Translations\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                    "</workbook>");

                ZipArchiveEntry sheetEntry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
                using (Stream sheetStream = sheetEntry.Open())
                using (StreamWriter sheetWriter = new StreamWriter(sheetStream, new UTF8Encoding(false)))
                {
                    sheetWriter.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    sheetWriter.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

                    WriteExcelRow(sheetWriter, 1, ExcelHeaderColumns);

                    int rowNumber = 2;
                    foreach (TranslationRowModel row in rows)
                    {
                        string[] values =
                        {
                            row.Scope.GetDisplayName(),
                            row.EntityGridDisplayName ?? string.Empty,
                            row.ParentName ?? string.Empty,
                            row.GridKey ?? string.Empty,
                            row.PropertyName ?? string.Empty,
                            row.SourceText ?? string.Empty,
                            row.TargetText ?? string.Empty
                        };

                        WriteExcelRow(sheetWriter, rowNumber, values);
                        rowNumber++;
                    }

                    sheetWriter.Write("</sheetData></worksheet>");
                }
            }
        }

        private static void WriteZipEntry(ZipArchive archive, string entryName, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using (Stream stream = entry.Open())
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static void WriteExcelRow(StreamWriter writer, int rowNumber, IList<string> values)
        {
            writer.Write("<row r=\"");
            writer.Write(rowNumber.ToString(CultureInfo.InvariantCulture));
            writer.Write("\">");

            for (int i = 0; i < values.Count; i++)
            {
                string cellRef = GetExcelColumnName(i) + rowNumber.ToString(CultureInfo.InvariantCulture);
                writer.Write("<c r=\"");
                writer.Write(cellRef);
                writer.Write("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">");
                writer.Write(EscapeXml(values[i] ?? string.Empty));
                writer.Write("</t></is></c>");
            }

            writer.Write("</row>");
        }

        private static string GetExcelColumnName(int zeroBasedIndex)
        {
            int index = zeroBasedIndex + 1;
            StringBuilder builder = new StringBuilder();
            while (index > 0)
            {
                int remainder = (index - 1) % 26;
                builder.Insert(0, (char)('A' + remainder));
                index = (index - 1) / 26;
            }

            return builder.ToString();
        }

        private static string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '&': builder.Append("&amp;"); break;
                    case '<': builder.Append("&lt;"); break;
                    case '>': builder.Append("&gt;"); break;
                    case '"': builder.Append("&quot;"); break;
                    case '\'': builder.Append("&apos;"); break;
                    default:
                        // Strip control characters not allowed in XML 1.0 (except TAB/CR/LF).
                        if (c < 0x20 && c != '\t' && c != '\n' && c != '\r')
                        {
                            continue;
                        }

                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        private void btnCopySourceToTarget_Click(object sender, EventArgs e)
        {
            if (!EnsureSessionLoaded())
            {
                return;
            }

            ApplyBulkChange("Copy source to target", row => row.TargetText = row.SourceText ?? string.Empty);
        }

        private void btnFillEmptyFromSource_Click(object sender, EventArgs e)
        {
            if (!EnsureSessionLoaded())
            {
                return;
            }

            ApplyBulkChange(
                "Fill empty target values",
                row =>
                {
                    if (string.IsNullOrWhiteSpace(row.TargetText))
                    {
                        row.TargetText = row.SourceText ?? string.Empty;
                    }
                });
        }

        private void btnClearTarget_Click(object sender, EventArgs e)
        {
            if (!EnsureSessionLoaded())
            {
                return;
            }

            ApplyBulkChange("Clear target values", row => row.TargetText = string.Empty);
        }

        private void ApplyBulkChange(string actionName, Action<TranslationRowModel> changeAction)
        {
            List<TranslationRowModel> targets = GetBulkTargets(actionName);
            if (targets.Count == 0)
            {
                return;
            }

            int changedCount = 0;

            _isApplyingUiChanges = true;
            try
            {
                foreach (TranslationRowModel row in targets)
                {
                    string before = row.TargetText ?? string.Empty;
                    changeAction(row);
                    string after = row.TargetText ?? string.Empty;

                    if (!TranslationRowModel.TextEquals(before, after))
                    {
                        changedCount++;
                        UpdateGridRow(row);
                    }
                    else if (row.IsModified)
                    {
                        UpdateGridRow(row);
                    }
                }
            }
            finally
            {
                _isApplyingUiChanges = false;
            }

            ApplyFilters();
            UpdateUiState();
            UpdateStatus(actionName + " completed. Changed rows: " + changedCount.ToString("N0", CultureInfo.InvariantCulture) + ".");
        }

        private BulkExcelImportResult ImportBulkTargetValuesFromExcel(string filePath)
        {
            if (_currentSession == null)
            {
                throw new InvalidOperationException("A translation session must be loaded before importing from Excel.");
            }

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("The selected Excel file could not be found.", filePath);
            }

            Dictionary<string, List<TranslationRowModel>> lookup = BuildBulkImportLookup(_currentSession.Rows);
            BulkExcelImportResult result = new BulkExcelImportResult { WorksheetName = Path.GetFileNameWithoutExtension(filePath) };

            List<List<string>> sheetRows = ReadExcelFile(filePath);
            if (sheetRows.Count == 0)
            {
                throw new InvalidOperationException("The Excel file is empty.");
            }

            List<string> headerValues = sheetRows[0];
            Dictionary<string, int> headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerValues.Count; i++)
            {
                string normalized = NormalizeExcelHeader(headerValues[i]);
                if (string.IsNullOrWhiteSpace(normalized) || headerMap.ContainsKey(normalized))
                {
                    continue;
                }

                headerMap[normalized] = i;
            }

            int entityColumn = ResolveExcelColumnIndex(headerMap, "entity", "table", "tablename", "entityname", "entitylogicalname", "entita");
            int keyColumn = ResolveExcelColumnIndex(headerMap, "key", "itemkey", "gridkey");
            int propertyColumn = ResolveExcelColumnIndex(headerMap, "property", "propertyname", "proprieta");
            int targetColumn = ResolveExcelColumnIndex(headerMap, "target", "targettext", "translatedtext", "translation", "translatedvalue", "traduzione");
            int parentColumn = ResolveExcelColumnIndex(headerMap, "parent", "parentname");
            int scopeColumn = ResolveExcelColumnIndex(headerMap, "scope", "mode");

            if (entityColumn < 0 || keyColumn < 0 || propertyColumn < 0 || targetColumn < 0)
            {
                throw new InvalidOperationException(
                    "The Excel file must contain the columns Entity, Key, Property and Target. Optional columns: Parent, Scope.");
            }

            Dictionary<string, BulkExcelImportRow> importRows = new Dictionary<string, BulkExcelImportRow>(StringComparer.OrdinalIgnoreCase);

            for (int rowIndex = 1; rowIndex < sheetRows.Count; rowIndex++)
            {
                List<string> values = sheetRows[rowIndex];

                BulkExcelImportRow importRow = new BulkExcelImportRow
                {
                    RowNumber = rowIndex + 1,
                    Scope = GetExcelValue(values, scopeColumn),
                    Entity = GetExcelValue(values, entityColumn),
                    Parent = GetExcelValue(values, parentColumn),
                    Key = GetExcelValue(values, keyColumn),
                    Property = GetExcelValue(values, propertyColumn),
                    Target = GetExcelValue(values, targetColumn)
                };

                if (importRow.IsCompletelyEmpty)
                {
                    continue;
                }

                result.ReadRows++;

                if (string.IsNullOrWhiteSpace(importRow.Entity) || string.IsNullOrWhiteSpace(importRow.Key) || string.IsNullOrWhiteSpace(importRow.Property))
                {
                    result.InvalidRows++;
                    AddBulkExcelExample(result.InvalidExamples, DescribeBulkExcelRow(importRow) + " -> missing Entity, Key or Property.");
                    continue;
                }

                string importKey = BuildBulkImportFileKey(importRow);
                if (importRows.ContainsKey(importKey))
                {
                    result.DuplicateInputRows++;
                    AddBulkExcelExample(result.DuplicateExamples, DescribeBulkExcelRow(importRow) + " -> duplicate key detected, last row wins.");
                }

                importRows[importKey] = importRow;
            }

            _isApplyingUiChanges = true;
            try
            {
                foreach (BulkExcelImportRow importRow in importRows.Values.OrderBy(row => row.RowNumber))
                {
                    string primaryKey = BuildBulkPrimaryKey(importRow.Entity, importRow.Key, importRow.Property);
                    List<TranslationRowModel> candidates;
                    if (!lookup.TryGetValue(primaryKey, out candidates) || candidates.Count == 0)
                    {
                        result.NotFoundRows++;
                        AddBulkExcelExample(result.NotFoundExamples, DescribeBulkExcelRow(importRow));
                        continue;
                    }

                    List<TranslationRowModel> filtered = candidates
                        .Where(row => MatchesBulkOptionalValues(row, importRow))
                        .Distinct()
                        .ToList();

                    if (filtered.Count == 0)
                    {
                        result.NotFoundRows++;
                        AddBulkExcelExample(result.NotFoundExamples, DescribeBulkExcelRow(importRow));
                        continue;
                    }

                    if (filtered.Count > 1)
                    {
                        result.AmbiguousRows++;
                        AddBulkExcelExample(result.AmbiguousExamples, DescribeBulkExcelRow(importRow));
                        continue;
                    }

                    TranslationRowModel rowModel = filtered[0];
                    string before = rowModel.TargetText ?? string.Empty;
                    string after = importRow.Target ?? string.Empty;

                    rowModel.TargetText = after;
                    UpdateGridRow(rowModel);

                    if (!TranslationRowModel.TextEquals(before, after))
                    {
                        result.ChangedRows++;
                    }
                    else
                    {
                        result.UnchangedRows++;
                    }
                }
            }
            finally
            {
                _isApplyingUiChanges = false;
            }

            return result;
        }

        private static string GetExcelValue(List<string> values, int columnIndex)
        {
            if (values == null || columnIndex < 0 || columnIndex >= values.Count)
            {
                return string.Empty;
            }

            return (values[columnIndex] ?? string.Empty).Trim();
        }

        private static int ResolveExcelColumnIndex(Dictionary<string, int> headerMap, params string[] aliases)
        {
            if (headerMap == null || aliases == null)
            {
                return -1;
            }

            foreach (string alias in aliases)
            {
                int columnIndex;
                if (headerMap.TryGetValue(NormalizeExcelHeader(alias), out columnIndex))
                {
                    return columnIndex;
                }
            }

            return -1;
        }

        // Reads an .xlsx file using only System.IO.Compression + System.Xml (no third-party
        // dependencies). Reads the first worksheet declared in the workbook. Inline strings and
        // shared strings are both supported. Returns one entry per row, each containing the cell
        // text values in column order (gaps padded with empty strings).
        private static List<List<string>> ReadExcelFile(string filePath)
        {
            List<List<string>> rows = new List<List<string>>();

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
            {
                List<string> sharedStrings = ReadSharedStrings(archive);
                string sheetPath = ResolveFirstWorksheetPath(archive);

                ZipArchiveEntry sheetEntry = archive.GetEntry(sheetPath);
                if (sheetEntry == null)
                {
                    throw new InvalidOperationException("The selected Excel file does not contain a readable worksheet.");
                }

                using (Stream sheetStream = sheetEntry.Open())
                using (XmlReader reader = XmlReader.Create(sheetStream, new XmlReaderSettings { IgnoreWhitespace = true }))
                {
                    List<string> currentRow = null;
                    string currentCellRef = null;
                    string currentCellType = null;
                    StringBuilder currentValue = new StringBuilder();
                    bool insideValue = false;
                    bool insideInlineText = false;

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.LocalName)
                            {
                                case "row":
                                    currentRow = new List<string>();
                                    break;
                                case "c":
                                    currentCellRef = reader.GetAttribute("r");
                                    currentCellType = reader.GetAttribute("t");
                                    currentValue.Length = 0;
                                    break;
                                case "v":
                                    insideValue = !reader.IsEmptyElement;
                                    break;
                                case "t":
                                    // Inline string text (inside <is>) or shared-string text.
                                    insideInlineText = !reader.IsEmptyElement;
                                    break;
                            }
                        }
                        else if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.SignificantWhitespace)
                        {
                            if (insideValue || insideInlineText)
                            {
                                currentValue.Append(reader.Value);
                            }
                        }
                        else if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            switch (reader.LocalName)
                            {
                                case "v":
                                    insideValue = false;
                                    break;
                                case "t":
                                    insideInlineText = false;
                                    break;
                                case "c":
                                    if (currentRow != null)
                                    {
                                        int columnIndex = ParseColumnIndexFromCellRef(currentCellRef, currentRow.Count);
                                        while (currentRow.Count < columnIndex)
                                        {
                                            currentRow.Add(string.Empty);
                                        }

                                        string text = currentValue.ToString();
                                        if (string.Equals(currentCellType, "s", StringComparison.Ordinal))
                                        {
                                            int sharedIndex;
                                            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out sharedIndex)
                                                && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                                            {
                                                text = sharedStrings[sharedIndex];
                                            }
                                            else
                                            {
                                                text = string.Empty;
                                            }
                                        }

                                        currentRow.Add(text ?? string.Empty);
                                    }

                                    currentCellRef = null;
                                    currentCellType = null;
                                    currentValue.Length = 0;
                                    break;
                                case "row":
                                    if (currentRow != null)
                                    {
                                        rows.Add(currentRow);
                                        currentRow = null;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            return rows;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            List<string> sharedStrings = new List<string>();
            ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
            {
                return sharedStrings;
            }

            using (Stream stream = entry.Open())
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = false }))
            {
                StringBuilder currentString = new StringBuilder();
                bool insideSi = false;
                bool insideText = false;

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.LocalName == "si")
                        {
                            insideSi = true;
                            currentString.Length = 0;
                        }
                        else if (reader.LocalName == "t" && insideSi)
                        {
                            insideText = !reader.IsEmptyElement;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.SignificantWhitespace)
                    {
                        if (insideText)
                        {
                            currentString.Append(reader.Value);
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.LocalName == "t")
                        {
                            insideText = false;
                        }
                        else if (reader.LocalName == "si")
                        {
                            sharedStrings.Add(currentString.ToString());
                            currentString.Length = 0;
                            insideSi = false;
                        }
                    }
                }
            }

            return sharedStrings;
        }

        private static string ResolveFirstWorksheetPath(ZipArchive archive)
        {
            // Default location used by virtually every producer (including this exporter).
            const string defaultPath = "xl/worksheets/sheet1.xml";
            if (archive.GetEntry(defaultPath) != null)
            {
                return defaultPath;
            }

            // Fallback: look up the first sheet via the workbook relationships.
            ZipArchiveEntry workbookEntry = archive.GetEntry("xl/workbook.xml");
            ZipArchiveEntry relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
            if (workbookEntry == null || relsEntry == null)
            {
                return defaultPath;
            }

            string firstSheetRid = null;
            using (Stream stream = workbookEntry.Open())
            using (XmlReader reader = XmlReader.Create(stream))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "sheet")
                    {
                        firstSheetRid = reader.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
                            ?? reader.GetAttribute("r:id");
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(firstSheetRid))
            {
                return defaultPath;
            }

            using (Stream stream = relsEntry.Open())
            using (XmlReader reader = XmlReader.Create(stream))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Relationship"
                        && string.Equals(reader.GetAttribute("Id"), firstSheetRid, StringComparison.Ordinal))
                    {
                        string target = reader.GetAttribute("Target") ?? string.Empty;
                        if (target.StartsWith("/", StringComparison.Ordinal))
                        {
                            return target.TrimStart('/');
                        }

                        return "xl/" + target;
                    }
                }
            }

            return defaultPath;
        }

        private static int ParseColumnIndexFromCellRef(string cellRef, int fallback)
        {
            if (string.IsNullOrEmpty(cellRef))
            {
                return fallback;
            }

            int columnNumber = 0;
            for (int i = 0; i < cellRef.Length; i++)
            {
                char c = cellRef[i];
                if (c >= 'A' && c <= 'Z')
                {
                    columnNumber = (columnNumber * 26) + (c - 'A' + 1);
                }
                else if (c >= 'a' && c <= 'z')
                {
                    columnNumber = (columnNumber * 26) + (c - 'a' + 1);
                }
                else
                {
                    break;
                }
            }

            return columnNumber > 0 ? columnNumber - 1 : fallback;
        }

        private static Dictionary<string, List<TranslationRowModel>> BuildBulkImportLookup(IEnumerable<TranslationRowModel> rows)
        {
            Dictionary<string, List<TranslationRowModel>> lookup = new Dictionary<string, List<TranslationRowModel>>(StringComparer.OrdinalIgnoreCase);

            foreach (TranslationRowModel row in rows ?? Enumerable.Empty<TranslationRowModel>())
            {
                HashSet<string> entityAliases = GetBulkEntityAliases(row);
                HashSet<string> keyAliases = GetBulkKeyAliases(row);

                foreach (string entityAlias in entityAliases)
                {
                    foreach (string keyAlias in keyAliases)
                    {
                        string primaryKey = BuildBulkPrimaryKey(entityAlias, keyAlias, row.PropertyName);
                        if (string.IsNullOrWhiteSpace(primaryKey))
                        {
                            continue;
                        }

                        List<TranslationRowModel> bucket;
                        if (!lookup.TryGetValue(primaryKey, out bucket))
                        {
                            bucket = new List<TranslationRowModel>();
                            lookup[primaryKey] = bucket;
                        }

                        if (!bucket.Contains(row))
                        {
                            bucket.Add(row);
                        }
                    }
                }
            }

            return lookup;
        }

        private static HashSet<string> GetBulkEntityAliases(TranslationRowModel row)
        {
            HashSet<string> values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddBulkAlias(values, row != null ? row.EntityLogicalName : null);
            AddBulkAlias(values, row != null ? row.EntityDisplayName : null);
            AddBulkAlias(values, row != null ? row.EntityGridDisplayName : null);
            AddBulkAlias(values, row != null ? row.EntityFilterDisplayName : null);
            return values;
        }

        private static HashSet<string> GetBulkKeyAliases(TranslationRowModel row)
        {
            HashSet<string> values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddBulkAlias(values, row != null ? row.GridKey : null);
            AddBulkAlias(values, row != null ? row.KeyDisplayName : null);
            return values;
        }

        private static void AddBulkAlias(ICollection<string> values, string value)
        {
            if (values == null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string normalized = value.Trim();
            if (!string.IsNullOrWhiteSpace(normalized) && !values.Contains(normalized))
            {
                values.Add(normalized);
            }
        }

        private static bool MatchesBulkOptionalValues(TranslationRowModel row, BulkExcelImportRow importRow)
        {
            if (row == null || importRow == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(importRow.Scope) &&
                !MatchesBulkValue(importRow.Scope, row.Scope.GetDisplayName(), row.Scope.ToString()))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(importRow.Parent) &&
                !MatchesBulkValue(importRow.Parent, row.ParentName))
            {
                return false;
            }

            return true;
        }

        private static bool MatchesBulkValue(string expected, params string[] candidates)
        {
            string normalizedExpected = NormalizeBulkKeyComponent(expected);
            if (string.IsNullOrWhiteSpace(normalizedExpected))
            {
                return true;
            }

            return (candidates ?? Array.Empty<string>())
                .Any(candidate => string.Equals(normalizedExpected, NormalizeBulkKeyComponent(candidate), StringComparison.Ordinal));
        }

        private static string NormalizeExcelHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string decomposed = value.Trim().Normalize(NormalizationForm.FormD);
            StringBuilder builder = new StringBuilder();

            foreach (char character in decomposed)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
        }

        private static string BuildBulkPrimaryKey(string entity, string key, string property)
        {
            return NormalizeBulkKeyComponent(entity) + "\u001F" +
                   NormalizeBulkKeyComponent(key) + "\u001F" +
                   NormalizeBulkKeyComponent(property);
        }

        private static string BuildBulkImportFileKey(BulkExcelImportRow row)
        {
            if (row == null)
            {
                return string.Empty;
            }

            return BuildBulkPrimaryKey(row.Entity, row.Key, row.Property) + "\u001F" +
                   NormalizeBulkKeyComponent(row.Scope) + "\u001F" +
                   NormalizeBulkKeyComponent(row.Parent);
        }

        private static string NormalizeBulkKeyComponent(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string DescribeBulkExcelRow(BulkExcelImportRow row)
        {
            if (row == null)
            {
                return string.Empty;
            }

            return "[Row " + row.RowNumber.ToString(CultureInfo.InvariantCulture) + "] " +
                   (row.Entity ?? string.Empty) + " | " +
                   (row.Key ?? string.Empty) + " | " +
                   (row.Property ?? string.Empty);
        }

        private static void AddBulkExcelExample(ICollection<string> values, string value)
        {
            if (values == null || string.IsNullOrWhiteSpace(value) || values.Count >= 5)
            {
                return;
            }

            values.Add(value);
        }

        private static void AppendBulkExcelExamples(StringBuilder builder, string title, IEnumerable<string> values)
        {
            List<string> items = values != null
                ? values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList()
                : new List<string>();

            if (items.Count == 0)
            {
                return;
            }

            builder.AppendLine();
            builder.AppendLine(title + ":");
            foreach (string item in items)
            {
                builder.AppendLine("- " + item);
            }
        }

        private static string BuildBulkExcelImportSummary(string fileName, BulkExcelImportResult result)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Excel bulk import completed.");
            builder.AppendLine();
            builder.AppendLine("File: " + (fileName ?? string.Empty));

            if (!string.IsNullOrWhiteSpace(result != null ? result.WorksheetName : null))
            {
                builder.AppendLine("Worksheet: " + result.WorksheetName);
            }

            builder.AppendLine("Rows read: " + (result != null ? result.ReadRows.ToString("N0", CultureInfo.InvariantCulture) : "0"));
            builder.AppendLine("Rows changed: " + (result != null ? result.ChangedRows.ToString("N0", CultureInfo.InvariantCulture) : "0"));
            builder.AppendLine("Rows unchanged: " + (result != null ? result.UnchangedRows.ToString("N0", CultureInfo.InvariantCulture) : "0"));
            builder.AppendLine("Rows not found: " + (result != null ? result.NotFoundRows.ToString("N0", CultureInfo.InvariantCulture) : "0"));
            builder.AppendLine("Rows ambiguous: " + (result != null ? result.AmbiguousRows.ToString("N0", CultureInfo.InvariantCulture) : "0"));
            builder.AppendLine("Rows invalid: " + (result != null ? result.InvalidRows.ToString("N0", CultureInfo.InvariantCulture) : "0"));
            builder.AppendLine("Duplicate Excel keys: " + (result != null ? result.DuplicateInputRows.ToString("N0", CultureInfo.InvariantCulture) : "0"));
            builder.AppendLine();
            builder.AppendLine("Matching key: Entity + Key + Property.");
            builder.AppendLine("Optional columns Scope and Parent are used only to disambiguate duplicates.");

            if (result != null)
            {
                AppendBulkExcelExamples(builder, "Examples of rows not found", result.NotFoundExamples);
                AppendBulkExcelExamples(builder, "Examples of ambiguous rows", result.AmbiguousExamples);
                AppendBulkExcelExamples(builder, "Examples of invalid rows", result.InvalidExamples);
                AppendBulkExcelExamples(builder, "Examples of duplicate Excel rows", result.DuplicateExamples);
            }

            return builder.ToString().Trim();
        }

        private List<TranslationRowModel> GetBulkTargets(string actionName)
        {
            List<TranslationRowModel> selected = GetSelectedRows();
            if (selected.Count > 0)
            {
                return selected;
            }

            List<TranslationRowModel> visible = GetVisibleRows();
            if (visible.Count == 0)
            {
                return visible;
            }

            DialogResult answer = MessageBox.Show(
                this,
                "No rows are selected.\r\n\r\nDo you want to apply '" + actionName + "' to all currently visible rows?",
                "Apply to visible rows",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            return answer == DialogResult.Yes ? visible : new List<TranslationRowModel>();
        }

        private List<TranslationRowModel> GetSelectedRows()
        {
            HashSet<Guid> ids = new HashSet<Guid>();

            foreach (DataGridViewRow gridRow in dgvTranslations.SelectedRows)
            {
                Guid id = GetRowId(gridRow);
                if (id != Guid.Empty)
                {
                    ids.Add(id);
                }
            }

            if (ids.Count == 0)
            {
                foreach (DataGridViewCell cell in dgvTranslations.SelectedCells)
                {
                    if (cell.RowIndex < 0)
                    {
                        continue;
                    }

                    Guid id = GetRowId(dgvTranslations.Rows[cell.RowIndex]);
                    if (id != Guid.Empty)
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids.Where(id => _rowsById.ContainsKey(id)).Select(id => _rowsById[id]).ToList();
        }

        private List<TranslationRowModel> GetVisibleRows()
        {
            List<TranslationRowModel> rows = new List<TranslationRowModel>();

            if (_gridTable == null)
            {
                return rows;
            }

            foreach (DataRowView drv in _gridTable.DefaultView)
            {
                object value = drv[HiddenIdColumn];
                Guid id;
                if (value is Guid)
                {
                    id = (Guid)value;
                }
                else if (value == null || !Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out id))
                {
                    id = Guid.Empty;
                }

                TranslationRowModel row;
                if (id != Guid.Empty && _rowsById.TryGetValue(id, out row))
                {
                    rows.Add(row);
                }
            }

            return rows;
        }

        private Guid GetRowId(DataGridViewRow gridRow)
        {
            if (gridRow == null || gridRow.Cells[HiddenIdColumn].Value == null)
            {
                return Guid.Empty;
            }

            object value = gridRow.Cells[HiddenIdColumn].Value;
            if (value is Guid)
            {
                return (Guid)value;
            }

            Guid parsed;
            return Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out parsed) ? parsed : Guid.Empty;
        }

        private void UpdateGridRow(TranslationRowModel rowModel)
        {
            if (_gridTable == null)
            {
                return;
            }

            DataRow row = _gridTable.Rows.Find(rowModel.RowId.ToString());
            if (row == null)
            {
                return;
            }

            PopulateDataRow(row, rowModel);
        }

        private void dgvTranslations_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_isApplyingUiChanges || _currentSession == null || e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            DataGridViewColumn column = dgvTranslations.Columns[e.ColumnIndex];
            if (column == null || !string.Equals(column.Name, TargetColumn, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Guid rowId = GetRowId(dgvTranslations.Rows[e.RowIndex]);
            TranslationRowModel row;
            if (rowId == Guid.Empty || !_rowsById.TryGetValue(rowId, out row))
            {
                return;
            }

            row.TargetText = Convert.ToString(dgvTranslations.Rows[e.RowIndex].Cells[TargetColumn].Value, CultureInfo.InvariantCulture) ?? string.Empty;

            _isApplyingUiChanges = true;
            try
            {
                UpdateGridRow(row);
            }
            finally
            {
                _isApplyingUiChanges = false;
            }

            UpdateCounter();
            UpdateUiState();
        }

        private void dgvTranslations_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            e.Control.TextChanged -= EditingControl_TextChanged;

            if (dgvTranslations.CurrentCell != null &&
                dgvTranslations.CurrentCell.ColumnIndex >= 0 &&
                string.Equals(dgvTranslations.Columns[dgvTranslations.CurrentCell.ColumnIndex].Name, TargetColumn, StringComparison.OrdinalIgnoreCase))
            {
                e.Control.TextChanged += EditingControl_TextChanged;
            }
        }

        private void EditingControl_TextChanged(object sender, EventArgs e)
        {
            if (_isApplyingUiChanges || _currentSession == null)
            {
                return;
            }

            DataGridViewCell currentCell = dgvTranslations.CurrentCell;
            if (currentCell == null || currentCell.RowIndex < 0)
            {
                return;
            }

            Control editControl = sender as Control;
            if (editControl == null)
            {
                return;
            }

            Guid rowId = GetRowId(dgvTranslations.Rows[currentCell.RowIndex]);
            TranslationRowModel row;
            if (rowId == Guid.Empty || !_rowsById.TryGetValue(rowId, out row))
            {
                return;
            }

            // Only update the in-memory model while typing. Do NOT write back to the bound
            // DataTable here: any change to the row (even just the Modified flag) raises a
            // RowChanged event that the DataGridView reacts to by re-binding the row, which
            // aborts the editing control (visible symptom: cannot commit an empty string, and
            // the user must click multiple times to resume editing). The Modified checkbox and
            // the yellow highlight are refreshed once, cleanly, in CellEndEdit -> UpdateGridRow.
            row.TargetText = editControl.Text ?? string.Empty;
        }

        private void dgvTranslations_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // The Modified column is read-only and informational. It is updated automatically when
            // the user commits an edit on the Target cell (see CellEndEdit -> UpdateGridRow), so
            // there is no work to do here. The previous implementation reverted the row to the
            // original target value when the user clicked the Modified cell, which produced the
            // confusing 'I must untick Modified before I can edit again' workflow.
        }

        private void dgvTranslations_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_currentSession == null || e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            DataGridViewColumn column = dgvTranslations.Columns[e.ColumnIndex];
            if (column == null)
            {
                return;
            }

            if (string.Equals(column.Name, SourceColumn, StringComparison.OrdinalIgnoreCase))
            {
                e.CellStyle.BackColor = Color.Gainsboro;
                return;
            }

            if (!string.Equals(column.Name, TargetColumn, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Guid rowId = GetRowId(dgvTranslations.Rows[e.RowIndex]);
            TranslationRowModel row;
            if (rowId == Guid.Empty || !_rowsById.TryGetValue(rowId, out row))
            {
                return;
            }

            if (row.IsModified)
            {
                e.CellStyle.BackColor = Color.LightGoldenrodYellow;
            }
        }

        private void dgvTranslations_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // Intentionally left empty.
            // Previously this handler called ApplyFilters(), which caused the DataGridView to lose
            // focus and edit state on every cell edit (because updating the Modified column triggers
            // a binding refresh). Filters are already applied explicitly from BindSession and from
            // every filter-related UI event, so no work is needed here.
        }

        private bool EnsureConnected()
        {
            if (Service != null)
            {
                return true;
            }

            MessageBox.Show(this, "Connect to Dataverse from XrmToolBox before using this tool.", "Connection required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private bool EnsureContextLoaded()
        {
            if (_languages.Count > 0 && _entities.Count > 0)
            {
                return true;
            }

            MessageBox.Show(this, "Load the environment context first.", "Context required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private bool EnsureSessionLoaded()
        {
            if (_currentSession != null)
            {
                return true;
            }

            MessageBox.Show(this, "Load a translation session first.", "Session required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private TranslationScope GetSelectedScope()
        {
            ScopeOption option = cmbScope.SelectedItem as ScopeOption;
            return option != null ? option.Value : TranslationScope.Table;
        }

        private string GetSelectedSolutionUniqueName()
        {
            SolutionItem solution = cmbSolutions.SelectedItem as SolutionItem;
            return solution != null && !solution.IsAll ? solution.UniqueName : null;
        }

        private int? GetSelectedSourceLanguageLcid()
        {
            LanguageItem language = cmbSourceLanguage.SelectedItem as LanguageItem;
            return language != null ? (int?)language.Lcid : null;
        }

        private int? GetSelectedTargetLanguageLcid()
        {
            LanguageItem language = cmbTargetLanguage.SelectedItem as LanguageItem;
            return language != null ? (int?)language.Lcid : null;
        }

        private string GetSelectedEntityLogicalName()
        {
            EntityItem entity = cmbEntity.SelectedItem as EntityItem;
            return entity != null && !entity.IsAll ? entity.LogicalName : null;
        }

        private string GetSelectedPropertyName()
        {
            FilterItem item = cmbProperty.SelectedItem as FilterItem;
            return item != null && !item.IsAll ? item.Value : null;
        }

        private void UpdateUiState()
        {
            bool hasConnection = Service != null;
            bool hasContext = _languages.Count > 0;
            bool hasSession = _currentSession != null;
            bool hasUnsavedChanges = hasSession && _currentSession.Rows.Any(row => row.IsModified);
            bool hasPendingPublish = !_pendingPublishScope.IsEmpty;

            tsbLoadContext.Enabled = hasConnection;
            tsbLoadRows.Enabled = hasConnection && hasContext;
            tsbSaveChanges.Enabled = hasConnection && hasSession && hasUnsavedChanges;
            tsbPublishChanged.Enabled = hasConnection && hasPendingPublish && !hasUnsavedChanges;
            tsbPublishAll.Enabled = hasConnection && !hasUnsavedChanges;

            btnRefreshContext.Enabled = hasConnection;
            btnLoadRows.Enabled = hasConnection && hasContext;

            grpSource.Enabled = hasConnection;
            grpFilters.Enabled = hasSession;
            grpBulk.Enabled = hasSession;
            dgvTranslations.Enabled = hasSession;
        }

        private void UpdateStatus(string message)
        {
            tslStatus.Text = message ?? string.Empty;
        }

        private void UpdateCounter()
        {
            int total = _gridTable != null ? _gridTable.Rows.Count : 0;
            int visible = _gridTable != null ? _gridTable.DefaultView.Count : 0;
            int modified = _rowsById.Values.Count(row => row.IsModified);
            int pendingPublish = _pendingPublishScope.Entities.Count + _pendingPublishScope.GlobalChoices.Count + _pendingPublishScope.NonPublishableComponents.Count;
            string publishText = _pendingPublishScope.RequiresPublishAllFallback
                ? "All"
                : pendingPublish.ToString("N0", CultureInfo.InvariantCulture);

            tslCounter.Text = "Rows: " + visible.ToString("N0", CultureInfo.InvariantCulture) +
                " / " + total.ToString("N0", CultureInfo.InvariantCulture) +
                " | Modified: " + modified.ToString("N0", CultureInfo.InvariantCulture) +
                " | Publish: " + publishText;
        }

        private void ShowError(Exception error, string caption)
        {
            string message = error != null ? error.Message : "Unknown error.";
            LogError(error != null ? error.ToString() : caption);
            UpdateStatus(caption + ": " + message);
            MessageBox.Show(this, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void EnableDoubleBuffer(DataGridView grid)
        {
            if (grid == null)
            {
                return;
            }

            try
            {
                typeof(DataGridView)
                    .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(grid, true, null);
            }
            catch
            {
                // Best effort only.
            }
        }

        private void LoadAboutButtonIcon()
        {
            try
            {
                string assemblyFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string assetPath = Path.Combine(assemblyFolder ?? string.Empty, "assets", "icon.png");

                if (File.Exists(assetPath))
                {
                    using (Image image = Image.FromFile(assetPath))
                    {
                        btnAbout.Image = new Bitmap(image, 20, 20);
                    }

                    btnAbout.DisplayStyle = ToolStripItemDisplayStyle.Image;
                    return;
                }
            }
            catch
            {
                // Ignore and use the embedded fallback icon.
            }

            try
            {
                byte[] iconBytes = Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAADf0lEQVR42u2Xa0hTYRjH/2eXc3brzNXUuUJDaxOnrVgXukmEZZkfihYliRRBYWQWRCn1qaB1nUVhsT5UUISaIFnR5UPQfUZmMaNsoEsj03ltc3q2efrQWrNpTmquDz1wvpyH9/3/zvM+z3OeF4iwESN6iizsX1U6kkqEBlBkYVmDJiEsX1tcb/sVhDecOFFcbwsHAGvQJBCwsIEQnPES/xEB1qBJCDxePwnLsvHhFA+KBEF8/BmBv51woZhPkxPpMvwPEHEA3kgOikcgf95ErEujoYmlICY5cDCDaHd48d4+gJxrLXAwg7izOQGZKgkAoMHOINn4AawvpUkugaZ9KsRN+C5zta4HuWUto0dALubCvD0RJdkK8Fwd3cv1m0olsuhNM1NU+cUFW05L7PUN1O19RjB9/YHrVHISWY3Gc/AhbNBK/eIAgPqbD1FdVDJqBExrlNDGCeDsczHZ6brCdp4iFmvO7rfFqKfaHPbuSuNzK9pau+Bl3AAEAMAwjIckSd6uLTkLbpke1SIpXbdr4SQE+kI6glgJD6tTaABARXnZ4/ZuB4P80gMQyb6/lCqjIVVGQ71sfuC6urq6Rqk0SpyRkaHVHL9YGpO4UjdLKYC59s3HKDHFVavVk0NKwpQYCoSvP1qt1s9IzlwAkYy+sn4KWIPG/+xZLB/a3ViWPX2p3AwAhfol03fPFbgAoOTY4QrwKDLkKiCIIZsCsvg4AMgta4FQt/aM3/ngxGWYL1UFrr1cUf2yy+Fy5+XlLVmVFito/tzWW1lZ+QwCWhIywNu2AX8WJyUlKYYQZR0q+F1J9bn63Rde9HAoiuJzOARx5tTJKo9ILgMpFoYM0PrVg2pLhxsA9Hr9/ChPR9dY6vqsuZfr8Q6yTqez/4LJdA+6javG3Ii23rDz31ptdpqmRVWGbYu0E70ukksgTSEYFaC5xw3+zgcdElqa2+0c8EKrXzbmRvTF4cGc8838Amn5jXVZSzWPt0/jCoRCtrd/kGho/eq2PL3/sqam5gO4c2cPuyutkGPv6+t/1An7+DLp0Z6lmUf3XL2Fd3fPobPpEzwDbpASIUQyGtHT4jE7dfqKizagbNtBND55BeUM1XB7JRutgCl7BzqbPkGTnR48E47DNBQ0kPjmw/9/w38EYIRLQ1jNp8kJGpnHKwGDjuBIKhFuiMDs/2euZhG/nEbcvgEhmWjf6ekl5gAAAABJRU5ErkJggg==");

                using (MemoryStream ms = new MemoryStream(iconBytes))
                {
                    btnAbout.Image = new Bitmap(Image.FromStream(ms), 20, 20);
                }

                btnAbout.DisplayStyle = ToolStripItemDisplayStyle.Image;
            }
            catch
            {
                btnAbout.Text = "About";
                btnAbout.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "About - Universal Translation Manager";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ClientSize = new Size(520, 310);

                if (btnAbout.Image != null)
                {
                    PictureBox picture = new PictureBox
                    {
                        Image = btnAbout.Image,
                        Location = new Point(24, 24),
                        Size = new Size(64, 64),
                        SizeMode = PictureBoxSizeMode.CenterImage
                    };
                    dialog.Controls.Add(picture);
                }

                System.Windows.Forms.Label title = new System.Windows.Forms.Label
                {
                    AutoSize = true,
                    Font = new Font(Font.FontFamily, 13F, FontStyle.Bold),
                    Location = new Point(104, 24),
                    Text = "Universal Translation Manager"
                };
                dialog.Controls.Add(title);

                string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version != null
                    ? System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()
                    : "1.0.0.0";

                System.Windows.Forms.Label subtitle = new System.Windows.Forms.Label
                {
                    AutoSize = true,
                    Location = new Point(106, 58),
                    Text = "Version " + version + " - by Giovanni Manunta"
                };
                dialog.Controls.Add(subtitle);

                System.Windows.Forms.Label body = new System.Windows.Forms.Label
                {
                    AutoSize = false,
                    Location = new Point(24, 105),
                    Size = new Size(470, 150),
                    Text =
                        "UI-only XrmToolBox plugin for Dataverse and Dynamics 365 metadata translations.\r\n\r\n" +
                        "Hybrid engine:\r\n" +
                        "- Direct metadata updates for tables, columns and choices\r\n" +
                        "- Form label editing from the connected environment\r\n" +
                        "- Hidden Microsoft translation package in memory for broader solution components\r\n\r\n" +
                        "The UI never asks the user to work with ZIP or Excel files.",
                    TextAlign = ContentAlignment.TopLeft
                };
                dialog.Controls.Add(body);

                Button closeButton = new Button
                {
                    Text = "Close",
                    DialogResult = DialogResult.OK,
                    Location = new Point(404, 266),
                    Size = new Size(90, 28)
                };
                dialog.Controls.Add(closeButton);
                dialog.AcceptButton = closeButton;
                dialog.CancelButton = closeButton;

                dialog.ShowDialog(this);
            }
        }
    }

    internal static class DataverseHybridTranslationService
    {
        public static EnvironmentContextInfo LoadContext(IOrganizationService service)
        {
            int baseLanguage = GetBaseLanguageCode(service);
            List<int> provisionedLanguages = GetProvisionedLanguages(service);

            EnvironmentContextInfo context = new EnvironmentContextInfo
            {
                BaseLanguageCode = baseLanguage,
                Languages = provisionedLanguages
                    .Distinct()
                    .OrderBy(lcid => lcid)
                    .Select(lcid => new LanguageItem
                    {
                        Lcid = lcid,
                        IsBaseLanguage = lcid == baseLanguage,
                        DisplayName = BuildLanguageDisplayName(lcid, lcid == baseLanguage)
                    })
                    .ToList(),
                Solutions = GetUnmanagedSolutions(service),
                Entities = GetAllEntityItems(service, baseLanguage)
            };

            return context;
        }

        public static TranslationSession LoadSession(IOrganizationService service, TranslationLoadRequest request, BackgroundWorker worker)
        {
            TranslationSession session = new TranslationSession { Request = request };

            if (request.Scope == TranslationScope.Package)
            {
                if (string.IsNullOrWhiteSpace(request.SolutionUniqueName))
                {
                    throw new InvalidOperationException("Package scope requires a specific unmanaged solution.");
                }

                worker.ReportProgressIfPossible(0, "Exporting the hidden translation package...");
                session.PackageWorkbook = ExportTranslationWorkbook(service, request.SolutionUniqueName);
                worker.ReportProgressIfPossible(0, "Parsing the translation package...");
                session.Rows = LoadPackageRows(session.PackageWorkbook, request);
            }
            else
            {
                worker.ReportProgressIfPossible(0, "Resolving entities...");
                List<EntityMetadata> entityHeaders = GetRelevantEntityHeaders(service, request.SolutionUniqueName, request.EntityLogicalName);
                worker.ReportProgressIfPossible(0, "Loading translation rows...");

                switch (request.Scope)
                {
                    case TranslationScope.Table:
                        session.Rows = LoadTableRows(entityHeaders, request);
                        break;

                    case TranslationScope.Column:
                        session.Rows = LoadColumnRows(service, entityHeaders, request, worker);
                        break;

                    case TranslationScope.Choice:
                        session.Rows = LoadChoiceRows(service, entityHeaders, request, worker);
                        break;

                    case TranslationScope.Form:
                        session.Rows = LoadFormRows(service, entityHeaders, request, worker);
                        break;

                    case TranslationScope.Combined:
                        worker.ReportProgressIfPossible(0, "Loading table translations...");
                        session.Rows = LoadTableRows(entityHeaders, request);
                        worker.ReportProgressIfPossible(0, "Loading choice translations...");
                        session.Rows.AddRange(LoadChoiceRows(service, entityHeaders, request, worker));
                        worker.ReportProgressIfPossible(0, "Loading form translations...");
                        session.Rows.AddRange(LoadFormRows(service, entityHeaders, request, worker));
                        break;

                    default:
                        throw new NotSupportedException("Unsupported translation scope: " + request.Scope + ".");
                }
            }

            session.Rows = session.Rows
                .OrderBy(row => row.EntityGridDisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.ParentName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.GridKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.PropertyName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return session;
        }

        public static TranslationSaveResult SaveRows(IOrganizationService service, TranslationSession session, List<TranslationRowModel> rows, BackgroundWorker worker)
        {
            TranslationSaveResult result = new TranslationSaveResult();

            List<TranslationRowModel> tableRows = rows.Where(row => row.Scope == TranslationScope.Table).ToList();
            List<TranslationRowModel> columnRows = rows.Where(row => row.Scope == TranslationScope.Column).ToList();
            List<TranslationRowModel> choiceRows = rows.Where(row => row.Scope == TranslationScope.Choice).ToList();
            List<TranslationRowModel> formRows = rows.Where(row => row.Scope == TranslationScope.Form).ToList();
            List<TranslationRowModel> packageRows = rows.Where(row => row.Scope == TranslationScope.Package).ToList();

            if (tableRows.Count > 0)
            {
                worker.ReportProgressIfPossible(0, "Saving table labels...");
                SaveTableRows(service, session, tableRows, result);
            }

            if (columnRows.Count > 0)
            {
                worker.ReportProgressIfPossible(0, "Saving column labels...");
                SaveColumnRows(service, session, columnRows, result);
            }

            if (choiceRows.Count > 0)
            {
                worker.ReportProgressIfPossible(0, "Saving choice labels...");
                SaveChoiceRows(service, session, choiceRows, result);
            }

            if (formRows.Count > 0)
            {
                worker.ReportProgressIfPossible(0, "Saving form labels...");
                SaveFormRows(service, session, formRows, result);
            }

            if (packageRows.Count > 0)
            {
                worker.ReportProgressIfPossible(0, "Saving solution package translations...");
                SavePackageRows(service, session, packageRows, result);
            }

            result.SavedRows = rows.Count;

            if (string.IsNullOrWhiteSpace(result.SummaryMessage))
            {
                result.SummaryMessage = packageRows.Count > 0
                    ? "Changes were imported through the hidden translation package. Publish All is recommended for package-based components."
                    : "Publish is still required to see the changes in the app.";
            }

            return result;
        }

        public static void PublishScope(IOrganizationService service, PendingPublishScope scope)
        {
            if (scope == null || scope.IsEmpty)
            {
                return;
            }

            if (scope.RequiresPublishAllFallback)
            {
                PublishAll(service);
                return;
            }

            StringBuilder xml = new StringBuilder();
            xml.Append("<importexportxml>");

            if (scope.Entities.Count > 0)
            {
                xml.Append("<entities>");
                foreach (string entity in scope.Entities.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                {
                    xml.Append("<entity>");
                    xml.Append(SecurityElement.Escape(entity));
                    xml.Append("</entity>");
                }
                xml.Append("</entities>");
            }

            if (scope.GlobalChoices.Count > 0)
            {
                xml.Append("<optionsets>");
                foreach (string optionSet in scope.GlobalChoices.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                {
                    xml.Append("<optionset>");
                    xml.Append(SecurityElement.Escape(optionSet));
                    xml.Append("</optionset>");
                }
                xml.Append("</optionsets>");
            }

            xml.Append("</importexportxml>");

            ExecuteLongRunningRequest(service, new PublishXmlRequest { ParameterXml = xml.ToString() });
        }

        public static void PublishAll(IOrganizationService service)
        {
            ExecuteLongRunningRequest(service, new PublishAllXmlRequest());
        }

        private const string TranslationWorkbookEntryName = "CrmTranslations.xml";
        private const string SpreadsheetNamespaceUri = "urn:schemas-microsoft-com:office:spreadsheet";
        private const string DisplayStringsWorksheetName = "Display Strings";
        private const string LocalizedLabelsWorksheetName = "Localized Labels";
        public const int MaxTranslationPackageLabelLength = 500;
        private static readonly TimeSpan LongRunningOperationTimeout = TimeSpan.FromMinutes(10);
        private const int LongRunningOperationMaxRetries = 3;
        private static readonly TimeSpan LongRunningOperationRetryDelay = TimeSpan.FromSeconds(10);

        private static TranslationWorkbook ExportTranslationWorkbook(IOrganizationService service, string solutionUniqueName)
        {
            ExportTranslationResponse response = (ExportTranslationResponse)ExecuteLongRunningRequest(
                service,
                new ExportTranslationRequest { SolutionName = solutionUniqueName });

            if (response == null || response.ExportTranslationFile == null || response.ExportTranslationFile.Length == 0)
            {
                throw new InvalidOperationException("The translation package returned by Dataverse is empty.");
            }

            return new TranslationWorkbook
            {
                SolutionUniqueName = solutionUniqueName,
                PackageZipBytes = response.ExportTranslationFile,
                WorkbookDocument = ExtractWorkbookDocument(response.ExportTranslationFile)
            };
        }

        private static List<TranslationRowModel> LoadPackageRows(TranslationWorkbook workbook, TranslationLoadRequest request)
        {
            List<TranslationRowModel> rows = new List<TranslationRowModel>();

            if (workbook == null || workbook.WorkbookDocument == null)
            {
                return rows;
            }

            rows.AddRange(LoadDisplayStringRows(workbook, request));
            rows.AddRange(LoadLocalizedLabelRows(workbook, request));
            return rows;
        }

        private static List<TranslationRowModel> LoadDisplayStringRows(TranslationWorkbook workbook, TranslationLoadRequest request)
        {
            List<TranslationRowModel> rows = new List<TranslationRowModel>();
            List<XmlElement> worksheetRows = GetWorksheetRows(workbook.WorkbookDocument, DisplayStringsWorksheetName);

            if (worksheetRows.Count <= 1)
            {
                return rows;
            }

            List<string> header = GetSpreadsheetRowValues(worksheetRows[0]);
            int sourceColumnIndex = ResolveLanguageColumnIndex(header, request.SourceLcid);
            int targetColumnIndex = ResolveLanguageColumnIndex(header, request.TargetLcid);

            if (sourceColumnIndex < 0)
            {
                throw new InvalidOperationException("The translation package does not contain the source language column " + request.SourceLcid.ToString(CultureInfo.InvariantCulture) + ".");
            }

            if (targetColumnIndex < 0)
            {
                throw new InvalidOperationException("The translation package does not contain the target language column " + request.TargetLcid.ToString(CultureInfo.InvariantCulture) + ".");
            }

            for (int rowIndex = 1; rowIndex < worksheetRows.Count; rowIndex++)
            {
                List<string> values = GetSpreadsheetRowValues(worksheetRows[rowIndex]);
                string rawEntityName = GetSpreadsheetValue(values, 0);
                string entityName = string.IsNullOrWhiteSpace(rawEntityName) ? "(Global)" : rawEntityName;
                string key = GetSpreadsheetValue(values, 1);

                if (!string.IsNullOrWhiteSpace(request.EntityLogicalName) &&
                    !string.Equals(rawEntityName, request.EntityLogicalName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entityName) && string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                string sourceText = GetSpreadsheetValue(values, sourceColumnIndex);
                string targetText = GetSpreadsheetValue(values, targetColumnIndex);

                rows.Add(new TranslationRowModel
                {
                    Scope = TranslationScope.Package,
                    EntityLogicalName = entityName,
                    EntityDisplayName = entityName,
                    ParentName = DisplayStringsWorksheetName,
                    GridKey = key,
                    KeyDisplayName = key,
                    PropertyName = "DisplayString",
                    SourceText = sourceText,
                    TargetText = targetText,
                    OriginalTargetText = targetText,
                    PackageEntry = new TranslationEntry
                    {
                        WorksheetName = DisplayStringsWorksheetName,
                        RowIndex = rowIndex,
                        SourceColumnIndex = sourceColumnIndex,
                        TargetColumnIndex = targetColumnIndex
                    }
                });
            }

            return rows;
        }

        private static List<TranslationRowModel> LoadLocalizedLabelRows(TranslationWorkbook workbook, TranslationLoadRequest request)
        {
            List<TranslationRowModel> rows = new List<TranslationRowModel>();
            List<XmlElement> worksheetRows = GetWorksheetRows(workbook.WorkbookDocument, LocalizedLabelsWorksheetName);

            if (worksheetRows.Count <= 1)
            {
                return rows;
            }

            List<string> header = GetSpreadsheetRowValues(worksheetRows[0]);
            int sourceColumnIndex = ResolveLanguageColumnIndex(header, request.SourceLcid);
            int targetColumnIndex = ResolveLanguageColumnIndex(header, request.TargetLcid);

            if (sourceColumnIndex < 0)
            {
                throw new InvalidOperationException("The translation package does not contain the source language column " + request.SourceLcid.ToString(CultureInfo.InvariantCulture) + ".");
            }

            if (targetColumnIndex < 0)
            {
                throw new InvalidOperationException("The translation package does not contain the target language column " + request.TargetLcid.ToString(CultureInfo.InvariantCulture) + ".");
            }

            for (int rowIndex = 1; rowIndex < worksheetRows.Count; rowIndex++)
            {
                List<string> values = GetSpreadsheetRowValues(worksheetRows[rowIndex]);
                string rawEntityName = GetSpreadsheetValue(values, 0);
                string entityName = string.IsNullOrWhiteSpace(rawEntityName) ? "(Global)" : rawEntityName;
                string objectId = GetSpreadsheetValue(values, 1);
                string objectColumnName = GetSpreadsheetValue(values, 2);

                if (!string.IsNullOrWhiteSpace(request.EntityLogicalName) &&
                    !string.Equals(rawEntityName, request.EntityLogicalName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entityName) && string.IsNullOrWhiteSpace(objectId) && string.IsNullOrWhiteSpace(objectColumnName))
                {
                    continue;
                }

                string sourceText = GetSpreadsheetValue(values, sourceColumnIndex);
                string targetText = GetSpreadsheetValue(values, targetColumnIndex);

                rows.Add(new TranslationRowModel
                {
                    Scope = TranslationScope.Package,
                    EntityLogicalName = entityName,
                    EntityDisplayName = entityName,
                    ParentName = objectId,
                    GridKey = objectColumnName,
                    KeyDisplayName = objectColumnName,
                    PropertyName = "LocalizedLabel",
                    SourceText = sourceText,
                    TargetText = targetText,
                    OriginalTargetText = targetText,
                    PackageEntry = new TranslationEntry
                    {
                        WorksheetName = LocalizedLabelsWorksheetName,
                        RowIndex = rowIndex,
                        SourceColumnIndex = sourceColumnIndex,
                        TargetColumnIndex = targetColumnIndex
                    }
                });
            }

            return rows;
        }

        private static void SavePackageRows(IOrganizationService service, TranslationSession session, List<TranslationRowModel> rows, TranslationSaveResult result)
        {
            if (session == null || session.PackageWorkbook == null || session.PackageWorkbook.WorkbookDocument == null)
            {
                throw new InvalidOperationException("The current translation session does not contain an in-memory translation package.");
            }

            foreach (TranslationRowModel row in rows)
            {
                if (row.PackageEntry == null)
                {
                    continue;
                }

                SetPackageCellValue(
                    session.PackageWorkbook.WorkbookDocument,
                    row.PackageEntry.WorksheetName,
                    row.PackageEntry.RowIndex,
                    row.PackageEntry.TargetColumnIndex,
                    row.TargetText ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(row.PackageEntry.WorksheetName))
                {
                    result.PublishScope.NonPublishableComponents.Add(row.PackageEntry.WorksheetName);
                }
            }

            byte[] translationFile = BuildTranslationPackageZip(session.PackageWorkbook);

            ExecuteLongRunningRequest(service, new ImportTranslationRequest { TranslationFile = translationFile });

            session.PackageWorkbook.PackageZipBytes = translationFile;

            result.PublishScope.RequiresPublishAllFallback = true;
            result.SummaryMessage = "Changes were imported through the hidden translation package. Publish All is recommended for package-based components.";
        }

        private static CrmServiceClient CreateLongRunningServiceClient(IOrganizationService service)
        {
            CrmServiceClient client = service as CrmServiceClient;
            if (client == null)
            {
                return null;
            }

            TimeSpan previousTimeout = CrmServiceClient.MaxConnectionTimeout;
            CrmServiceClient.MaxConnectionTimeout = LongRunningOperationTimeout;
            try
            {
                return client.Clone();
            }
            finally
            {
                CrmServiceClient.MaxConnectionTimeout = previousTimeout;
            }
        }

        private static OrganizationResponse ExecuteLongRunningRequest(IOrganizationService service, OrganizationRequest request)
        {
            for (int attempt = 1; attempt <= LongRunningOperationMaxRetries; attempt++)
            {
                using (CrmServiceClient clone = CreateLongRunningServiceClient(service))
                {
                    IOrganizationService target = clone ?? service;
                    try
                    {
                        return target.Execute(request);
                    }
                    catch (CommunicationException) when (attempt < LongRunningOperationMaxRetries)
                    {
                        Thread.Sleep(LongRunningOperationRetryDelay);
                    }
                }
            }

            throw new InvalidOperationException("All retry attempts for the long-running request have been exhausted.");
        }

        private static XmlDocument ExtractWorkbookDocument(byte[] translationFile)
        {
            using (MemoryStream packageStream = new MemoryStream(translationFile))
            using (ZipArchive archive = new ZipArchive(packageStream, ZipArchiveMode.Read, false))
            {
                ZipArchiveEntry workbookEntry = archive.Entries.FirstOrDefault(entry =>
                    string.Equals(entry.FullName, TranslationWorkbookEntryName, StringComparison.OrdinalIgnoreCase));

                if (workbookEntry == null)
                {
                    throw new InvalidOperationException("The translation package does not contain CrmTranslations.xml.");
                }

                XmlDocument document = new XmlDocument
                {
                    PreserveWhitespace = true
                };

                using (Stream entryStream = workbookEntry.Open())
                {
                    document.Load(entryStream);
                }

                return document;
            }
        }

        private static byte[] BuildTranslationPackageZip(TranslationWorkbook workbook)
        {
            if (workbook == null || workbook.PackageZipBytes == null || workbook.PackageZipBytes.Length == 0)
            {
                throw new InvalidOperationException("The in-memory translation package is empty.");
            }

            byte[] sourceBytes = workbook.PackageZipBytes.ToArray();

            using (MemoryStream packageStream = new MemoryStream())
            {
                packageStream.Write(sourceBytes, 0, sourceBytes.Length);
                packageStream.Position = 0;

                using (ZipArchive archive = new ZipArchive(packageStream, ZipArchiveMode.Update, true))
                {
                    ZipArchiveEntry existingEntry = archive.Entries.FirstOrDefault(entry =>
                        string.Equals(entry.FullName, TranslationWorkbookEntryName, StringComparison.OrdinalIgnoreCase));

                    if (existingEntry != null)
                    {
                        existingEntry.Delete();
                    }

                    ZipArchiveEntry workbookEntry = archive.CreateEntry(TranslationWorkbookEntryName, CompressionLevel.Optimal);
                    using (Stream entryStream = workbookEntry.Open())
                    using (XmlWriter writer = XmlWriter.Create(entryStream, new XmlWriterSettings
                    {
                        Encoding = new UTF8Encoding(false),
                        Indent = false,
                        OmitXmlDeclaration = false
                    }))
                    {
                        workbook.WorkbookDocument.Save(writer);
                    }
                }

                return packageStream.ToArray();
            }
        }

        private static List<XmlElement> GetWorksheetRows(XmlDocument document, string worksheetName)
        {
            if (document == null)
            {
                return new List<XmlElement>();
            }

            XmlNamespaceManager namespaceManager = CreateSpreadsheetNamespaceManager(document);
            XmlNodeList worksheetNodes = document.SelectNodes("/ss:Workbook/ss:Worksheet", namespaceManager);

            foreach (XmlElement worksheet in worksheetNodes.Cast<XmlNode>().OfType<XmlElement>())
            {
                string currentName = worksheet.GetAttribute("Name", SpreadsheetNamespaceUri);
                if (!string.Equals(currentName, worksheetName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                XmlNodeList rowNodes = worksheet.SelectNodes("ss:Table/ss:Row", namespaceManager);
                return rowNodes.Cast<XmlNode>().OfType<XmlElement>().ToList();
            }

            return new List<XmlElement>();
        }

        private static List<string> GetSpreadsheetRowValues(XmlElement row)
        {
            List<string> values = new List<string>();
            XmlNamespaceManager namespaceManager = CreateSpreadsheetNamespaceManager(row.OwnerDocument);
            int expectedIndex = 1;

            foreach (XmlElement cell in row.SelectNodes("ss:Cell", namespaceManager).Cast<XmlNode>().OfType<XmlElement>())
            {
                int actualIndex = expectedIndex;
                string indexValue = cell.GetAttribute("Index", SpreadsheetNamespaceUri);
                if (!string.IsNullOrWhiteSpace(indexValue))
                {
                    int parsedIndex;
                    if (int.TryParse(indexValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedIndex) && parsedIndex > 0)
                    {
                        actualIndex = parsedIndex;
                    }
                }

                while (values.Count < actualIndex - 1)
                {
                    values.Add(null);
                }

                values.Add(GetSpreadsheetCellText(cell, namespaceManager));
                expectedIndex = actualIndex + 1;
            }

            return values;
        }

        private static string GetSpreadsheetValue(List<string> values, int zeroBasedIndex)
        {
            return values != null && zeroBasedIndex >= 0 && zeroBasedIndex < values.Count
                ? values[zeroBasedIndex] ?? string.Empty
                : string.Empty;
        }

        private static int ResolveLanguageColumnIndex(List<string> headerValues, int lcid)
        {
            string columnName = lcid.ToString(CultureInfo.InvariantCulture);

            for (int index = 0; index < headerValues.Count; index++)
            {
                if (string.Equals(headerValues[index], columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        private static string GetSpreadsheetCellText(XmlElement cell, XmlNamespaceManager namespaceManager)
        {
            XmlElement dataElement = cell.SelectSingleNode("ss:Data", namespaceManager) as XmlElement;
            return dataElement != null ? dataElement.InnerText : string.Empty;
        }

        private static void SetPackageCellValue(XmlDocument document, string worksheetName, int rowIndex, int columnIndex, string value)
        {
            List<XmlElement> worksheetRows = GetWorksheetRows(document, worksheetName);

            if (rowIndex < 0 || rowIndex >= worksheetRows.Count)
            {
                throw new InvalidOperationException("The translation package row index is outside the available worksheet range.");
            }

            XmlElement row = worksheetRows[rowIndex];
            List<string> values = GetSpreadsheetRowValues(row);

            while (values.Count <= columnIndex)
            {
                values.Add(null);
            }

            values[columnIndex] = value ?? string.Empty;
            RewriteSpreadsheetRow(row, values);
        }

        private static void RewriteSpreadsheetRow(XmlElement row, List<string> values)
        {
            XmlDocument document = row.OwnerDocument;
            XmlNamespaceManager namespaceManager = CreateSpreadsheetNamespaceManager(document);
            List<XmlElement> existingCells = row.SelectNodes("ss:Cell", namespaceManager).Cast<XmlNode>().OfType<XmlElement>().ToList();

            foreach (XmlElement cell in existingCells)
            {
                row.RemoveChild(cell);
            }

            int expectedIndex = 1;
            for (int index = 0; index < values.Count; index++)
            {
                if (values[index] == null)
                {
                    continue;
                }

                int actualIndex = index + 1;
                XmlElement cell = CreateSpreadsheetCell(document, actualIndex != expectedIndex ? (int?)actualIndex : null, values[index]);
                row.AppendChild(cell);
                expectedIndex = actualIndex + 1;
            }
        }

        private static XmlElement CreateSpreadsheetCell(XmlDocument document, int? columnIndex, string value)
        {
            XmlElement cell = document.CreateElement("ss", "Cell", SpreadsheetNamespaceUri);
            if (columnIndex.HasValue)
            {
                XmlAttribute indexAttribute = document.CreateAttribute("ss", "Index", SpreadsheetNamespaceUri);
                indexAttribute.Value = columnIndex.Value.ToString(CultureInfo.InvariantCulture);
                cell.Attributes.Append(indexAttribute);
            }

            XmlElement data = document.CreateElement("ss", "Data", SpreadsheetNamespaceUri);
            XmlAttribute typeAttribute = document.CreateAttribute("ss", "Type", SpreadsheetNamespaceUri);
            typeAttribute.Value = "String";
            data.Attributes.Append(typeAttribute);
            data.InnerText = value ?? string.Empty;
            cell.AppendChild(data);
            return cell;
        }

        private static XmlNamespaceManager CreateSpreadsheetNamespaceManager(XmlDocument document)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("ss", SpreadsheetNamespaceUri);
            return namespaceManager;
        }

        private static List<TranslationRowModel> LoadTableRows(List<EntityMetadata> entities, TranslationLoadRequest request)
        {
            List<TranslationRowModel> rows = new List<TranslationRowModel>();

            foreach (EntityMetadata entity in entities)
            {
                string entityDisplayName = GetBestLabelText(entity.DisplayName, request.SourceLcid, entity.LogicalName);

                rows.Add(CreateTableRow(entity, entityDisplayName, "DisplayName", entity.DisplayName, request));
                rows.Add(CreateTableRow(entity, entityDisplayName, "DisplayCollectionName", entity.DisplayCollectionName, request));
                rows.Add(CreateTableRow(entity, entityDisplayName, "Description", entity.Description, request));
            }

            return rows;
        }

        private static TranslationRowModel CreateTableRow(EntityMetadata entity, string entityDisplayName, string propertyName, Microsoft.Xrm.Sdk.Label label, TranslationLoadRequest request)
        {
            return new TranslationRowModel
            {
                Scope = TranslationScope.Table,
                EntityLogicalName = entity.LogicalName,
                EntityDisplayName = entityDisplayName,
                ParentName = string.Empty,
                GridKey = entity.LogicalName,
                PropertyName = propertyName,
                SourceText = GetExactLabelText(label, request.SourceLcid),
                TargetText = GetExactLabelText(label, request.TargetLcid),
                OriginalTargetText = GetExactLabelText(label, request.TargetLcid)
            };
        }

        private static List<TranslationRowModel> LoadColumnRows(IOrganizationService service, List<EntityMetadata> entityHeaders, TranslationLoadRequest request, BackgroundWorker worker)
        {
            List<TranslationRowModel> rows = new List<TranslationRowModel>();

            List<EntityMetadata> entities = RetrieveEntitiesWithAttributes(service, entityHeaders, worker, "Loading columns in batch");

            worker.ReportProgressIfPossible(0, "Building column translation rows...");

            foreach (EntityMetadata entity in entities)
            {
                if (entity.Attributes == null)
                {
                    continue;
                }

                string entityDisplayName = GetBestLabelText(entity.DisplayName, request.SourceLcid, entity.LogicalName);

                foreach (AttributeMetadata attribute in entity.Attributes.OrderBy(item => item.LogicalName, StringComparer.OrdinalIgnoreCase))
                {
                    if (!ShouldIncludeAttribute(attribute))
                    {
                        continue;
                    }

                    string attributeDisplayName = GetBestLabelText(attribute.DisplayName, request.SourceLcid, attribute.LogicalName);

                    rows.Add(new TranslationRowModel
                    {
                        Scope = TranslationScope.Column,
                        EntityLogicalName = entity.LogicalName,
                        EntityDisplayName = entityDisplayName,
                        ParentName = attribute.LogicalName,
                        GridKey = attributeDisplayName,
                        PropertyName = "DisplayName",
                        SourceText = GetExactLabelText(attribute.DisplayName, request.SourceLcid),
                        TargetText = GetExactLabelText(attribute.DisplayName, request.TargetLcid),
                        OriginalTargetText = GetExactLabelText(attribute.DisplayName, request.TargetLcid),
                        AttributeLogicalName = attribute.LogicalName
                    });

                    rows.Add(new TranslationRowModel
                    {
                        Scope = TranslationScope.Column,
                        EntityLogicalName = entity.LogicalName,
                        EntityDisplayName = entityDisplayName,
                        ParentName = attribute.LogicalName,
                        GridKey = attributeDisplayName,
                        PropertyName = "Description",
                        SourceText = GetExactLabelText(attribute.Description, request.SourceLcid),
                        TargetText = GetExactLabelText(attribute.Description, request.TargetLcid),
                        OriginalTargetText = GetExactLabelText(attribute.Description, request.TargetLcid),
                        AttributeLogicalName = attribute.LogicalName
                    });
                }
            }

            return rows;
        }

        private static List<TranslationRowModel> LoadChoiceRows(IOrganizationService service, List<EntityMetadata> entityHeaders, TranslationLoadRequest request, BackgroundWorker worker)
        {
            List<TranslationRowModel> rows = new List<TranslationRowModel>();
            HashSet<string> loadedGlobalChoices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            List<EntityMetadata> entities = RetrieveEntitiesWithAttributes(service, entityHeaders, worker, "Loading choices in batch");

            worker.ReportProgressIfPossible(0, "Building choice translation rows...");

            foreach (EntityMetadata entity in entities)
            {
                if (entity.Attributes == null)
                {
                    continue;
                }

                string entityDisplayName = GetBestLabelText(entity.DisplayName, request.SourceLcid, entity.LogicalName);

                foreach (AttributeMetadata attribute in entity.Attributes.OrderBy(item => item.LogicalName, StringComparer.OrdinalIgnoreCase))
                {
                    if (!ShouldIncludeAttribute(attribute))
                    {
                        continue;
                    }

                    if (attribute is PicklistAttributeMetadata picklist)
                    {
                        AddOptionSetRows(rows, entity, entityDisplayName, attribute, picklist.OptionSet, request, loadedGlobalChoices);
                    }
                    else if (attribute is MultiSelectPicklistAttributeMetadata multiSelect)
                    {
                        AddOptionSetRows(rows, entity, entityDisplayName, attribute, multiSelect.OptionSet, request, loadedGlobalChoices);
                    }
                    else if (attribute is BooleanAttributeMetadata booleanAttribute)
                    {
                        AddBooleanRows(rows, entity, entityDisplayName, booleanAttribute, request);
                    }
                    else if (attribute is StateAttributeMetadata stateAttribute)
                    {
                        AddStateRows(rows, entity, entityDisplayName, stateAttribute, request);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(request.EntityLogicalName))
            {
                worker.ReportProgressIfPossible(0, "Loading standalone global choices...");
                RetrieveAllOptionSetsResponse response = (RetrieveAllOptionSetsResponse)service.Execute(new RetrieveAllOptionSetsRequest { RetrieveAsIfPublished = true });

                foreach (OptionSetMetadataBase optionSetBase in response.OptionSetMetadata)
                {
                    OptionSetMetadata optionSet = optionSetBase as OptionSetMetadata;
                    if (optionSet == null || string.IsNullOrWhiteSpace(optionSet.Name) || loadedGlobalChoices.Contains(optionSet.Name))
                    {
                        continue;
                    }

                    string choiceDisplayName = GetBestLabelText(optionSet.DisplayName, request.SourceLcid, optionSet.Name);

                    foreach (OptionMetadata option in optionSet.Options.Where(item => item != null && item.Value.HasValue).OrderBy(item => GetBestLabelText(item.Label, request.SourceLcid, item.Value.Value.ToString(CultureInfo.InvariantCulture)), StringComparer.OrdinalIgnoreCase))
                    {
                        rows.Add(new TranslationRowModel
                        {
                            Scope = TranslationScope.Choice,
                            EntityLogicalName = string.Empty,
                            EntityDisplayName = "(Global)",
                            ParentName = optionSet.Name,
                            GridKey = option.Value.Value.ToString(CultureInfo.InvariantCulture),
                            PropertyName = "OptionLabel",
                            SourceText = GetExactLabelText(option.Label, request.SourceLcid),
                            TargetText = GetExactLabelText(option.Label, request.TargetLcid),
                            OriginalTargetText = GetExactLabelText(option.Label, request.TargetLcid),
                            GlobalChoiceName = optionSet.Name,
                            ChoiceKind = ChoiceTranslationKind.GlobalOption,
                            OptionValue = option.Value.Value,
                            KeyDisplayName = choiceDisplayName + " / " + GetBestLabelText(option.Label, request.SourceLcid, option.Value.Value.ToString(CultureInfo.InvariantCulture))
                        });
                    }
                }
            }

            return rows;
        }

        private static void AddOptionSetRows(List<TranslationRowModel> rows, EntityMetadata entity, string entityDisplayName, AttributeMetadata attribute, OptionSetMetadataBase optionSet, TranslationLoadRequest request, HashSet<string> loadedGlobalChoices)
        {
            OptionSetMetadata metadata = optionSet as OptionSetMetadata;
            if (metadata == null)
            {
                return;
            }

            string attributeDisplayName = GetBestLabelText(attribute.DisplayName, request.SourceLcid, attribute.LogicalName);
            string globalChoiceName = metadata.IsGlobal.GetValueOrDefault() ? metadata.Name : null;
            if (!string.IsNullOrWhiteSpace(globalChoiceName))
            {
                loadedGlobalChoices.Add(globalChoiceName);
            }

            foreach (OptionMetadata option in metadata.Options.Where(item => item != null && item.Value.HasValue).OrderBy(item => item.Value.Value))
            {
                string optionText = GetBestLabelText(option.Label, request.SourceLcid, option.Value.Value.ToString(CultureInfo.InvariantCulture));
                rows.Add(new TranslationRowModel
                {
                    Scope = TranslationScope.Choice,
                    EntityLogicalName = entity.LogicalName,
                    EntityDisplayName = entityDisplayName,
                    ParentName = attribute.LogicalName,
                    GridKey = option.Value.Value.ToString(CultureInfo.InvariantCulture),
                    PropertyName = "OptionLabel",
                    SourceText = GetExactLabelText(option.Label, request.SourceLcid),
                    TargetText = GetExactLabelText(option.Label, request.TargetLcid),
                    OriginalTargetText = GetExactLabelText(option.Label, request.TargetLcid),
                    AttributeLogicalName = attribute.LogicalName,
                    GlobalChoiceName = globalChoiceName,
                    ChoiceKind = string.IsNullOrWhiteSpace(globalChoiceName) ? ChoiceTranslationKind.LocalOption : ChoiceTranslationKind.GlobalOption,
                    OptionValue = option.Value.Value,
                    KeyDisplayName = attributeDisplayName + " / " + optionText
                });
            }
        }

        private static void AddBooleanRows(List<TranslationRowModel> rows, EntityMetadata entity, string entityDisplayName, BooleanAttributeMetadata attribute, TranslationLoadRequest request)
        {
            if (attribute.OptionSet == null)
            {
                return;
            }

            string attributeDisplayName = GetBestLabelText(attribute.DisplayName, request.SourceLcid, attribute.LogicalName);

            OptionMetadata trueOption = attribute.OptionSet.TrueOption;
            if (trueOption != null && trueOption.Value.HasValue)
            {
                rows.Add(new TranslationRowModel
                {
                    Scope = TranslationScope.Choice,
                    EntityLogicalName = entity.LogicalName,
                    EntityDisplayName = entityDisplayName,
                    ParentName = attribute.LogicalName,
                    GridKey = trueOption.Value.Value.ToString(CultureInfo.InvariantCulture),
                    PropertyName = "OptionLabel",
                    SourceText = GetExactLabelText(trueOption.Label, request.SourceLcid),
                    TargetText = GetExactLabelText(trueOption.Label, request.TargetLcid),
                    OriginalTargetText = GetExactLabelText(trueOption.Label, request.TargetLcid),
                    AttributeLogicalName = attribute.LogicalName,
                    ChoiceKind = ChoiceTranslationKind.BooleanOption,
                    OptionValue = trueOption.Value.Value,
                    KeyDisplayName = attributeDisplayName + " / " + GetBestLabelText(trueOption.Label, request.SourceLcid, "True")
                });
            }

            OptionMetadata falseOption = attribute.OptionSet.FalseOption;
            if (falseOption != null && falseOption.Value.HasValue)
            {
                rows.Add(new TranslationRowModel
                {
                    Scope = TranslationScope.Choice,
                    EntityLogicalName = entity.LogicalName,
                    EntityDisplayName = entityDisplayName,
                    ParentName = attribute.LogicalName,
                    GridKey = falseOption.Value.Value.ToString(CultureInfo.InvariantCulture),
                    PropertyName = "OptionLabel",
                    SourceText = GetExactLabelText(falseOption.Label, request.SourceLcid),
                    TargetText = GetExactLabelText(falseOption.Label, request.TargetLcid),
                    OriginalTargetText = GetExactLabelText(falseOption.Label, request.TargetLcid),
                    AttributeLogicalName = attribute.LogicalName,
                    ChoiceKind = ChoiceTranslationKind.BooleanOption,
                    OptionValue = falseOption.Value.Value,
                    KeyDisplayName = attributeDisplayName + " / " + GetBestLabelText(falseOption.Label, request.SourceLcid, "False")
                });
            }
        }

        private static void AddStateRows(List<TranslationRowModel> rows, EntityMetadata entity, string entityDisplayName, StateAttributeMetadata attribute, TranslationLoadRequest request)
        {
            if (attribute.OptionSet == null)
            {
                return;
            }

            string attributeDisplayName = GetBestLabelText(attribute.DisplayName, request.SourceLcid, attribute.LogicalName);

            foreach (OptionMetadata option in attribute.OptionSet.Options.Where(item => item != null && item.Value.HasValue).OrderBy(item => item.Value.Value))
            {
                rows.Add(new TranslationRowModel
                {
                    Scope = TranslationScope.Choice,
                    EntityLogicalName = entity.LogicalName,
                    EntityDisplayName = entityDisplayName,
                    ParentName = attribute.LogicalName,
                    GridKey = option.Value.Value.ToString(CultureInfo.InvariantCulture),
                    PropertyName = "StateLabel",
                    SourceText = GetExactLabelText(option.Label, request.SourceLcid),
                    TargetText = GetExactLabelText(option.Label, request.TargetLcid),
                    OriginalTargetText = GetExactLabelText(option.Label, request.TargetLcid),
                    AttributeLogicalName = attribute.LogicalName,
                    ChoiceKind = ChoiceTranslationKind.StateOption,
                    OptionValue = option.Value.Value,
                    KeyDisplayName = attributeDisplayName + " / " + GetBestLabelText(option.Label, request.SourceLcid, option.Value.Value.ToString(CultureInfo.InvariantCulture))
                });
            }
        }

        private static List<TranslationRowModel> LoadFormRows(IOrganizationService service, List<EntityMetadata> entityHeaders, TranslationLoadRequest request, BackgroundWorker worker)
        {
            List<TranslationRowModel> rows = new List<TranslationRowModel>();
            int totalEntities = entityHeaders.Count;

            Dictionary<string, Dictionary<string, FormNodeSnapshot>> sourceSnapshots = new Dictionary<string, Dictionary<string, FormNodeSnapshot>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, Dictionary<string, FormNodeSnapshot>> targetSnapshots = new Dictionary<string, Dictionary<string, FormNodeSnapshot>>(StringComparer.OrdinalIgnoreCase);

            worker.ReportProgressIfPossible(0, "Switching to source language and loading all form labels...");
            using (new TemporaryUserLanguageScope(service, request.SourceLcid))
            {
                int processed = 0;
                foreach (EntityMetadata entity in entityHeaders)
                {
                    processed++;
                    worker.ReportProgressIfPossible(0, "Loading source form labels for " + entity.LogicalName + " (" + processed.ToString(CultureInfo.InvariantCulture) + "/" + totalEntities.ToString(CultureInfo.InvariantCulture) + ")...");
                    sourceSnapshots[entity.LogicalName] = ReadFormSnapshotForEntity(service, entity.LogicalName, request.SourceLcid);
                }
            }

            worker.ReportProgressIfPossible(0, "Switching to target language and loading all form labels...");
            using (new TemporaryUserLanguageScope(service, request.TargetLcid))
            {
                int processed = 0;
                foreach (EntityMetadata entity in entityHeaders)
                {
                    processed++;
                    worker.ReportProgressIfPossible(0, "Loading target form labels for " + entity.LogicalName + " (" + processed.ToString(CultureInfo.InvariantCulture) + "/" + totalEntities.ToString(CultureInfo.InvariantCulture) + ")...");
                    targetSnapshots[entity.LogicalName] = ReadFormSnapshotForEntity(service, entity.LogicalName, request.TargetLcid);
                }
            }

            worker.ReportProgressIfPossible(0, "Building form translation rows...");
            foreach (EntityMetadata entity in entityHeaders)
            {
                Dictionary<string, FormNodeSnapshot> sourceSnapshot;
                Dictionary<string, FormNodeSnapshot> targetSnapshot;
                if (!sourceSnapshots.TryGetValue(entity.LogicalName, out sourceSnapshot))
                    sourceSnapshot = new Dictionary<string, FormNodeSnapshot>(StringComparer.OrdinalIgnoreCase);
                if (!targetSnapshots.TryGetValue(entity.LogicalName, out targetSnapshot))
                    targetSnapshot = new Dictionary<string, FormNodeSnapshot>(StringComparer.OrdinalIgnoreCase);

                HashSet<string> keys = new HashSet<string>(sourceSnapshot.Keys, StringComparer.OrdinalIgnoreCase);
                keys.UnionWith(targetSnapshot.Keys);

                foreach (string key in keys.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                {
                    FormNodeSnapshot sourceNode = sourceSnapshot.ContainsKey(key) ? sourceSnapshot[key] : targetSnapshot[key];
                    FormNodeSnapshot targetNode = targetSnapshot.ContainsKey(key) ? targetSnapshot[key] : sourceSnapshot[key];

                    rows.Add(new TranslationRowModel
                    {
                        Scope = TranslationScope.Form,
                        EntityLogicalName = entity.LogicalName,
                        EntityDisplayName = GetBestLabelText(entity.DisplayName, request.SourceLcid, entity.LogicalName),
                        ParentName = sourceNode.ParentPath,
                        GridKey = sourceNode.DisplayKey,
                        PropertyName = sourceNode.NodeType.GetDisplayName(),
                        SourceText = sourceNode.Text,
                        TargetText = targetNode.Text,
                        OriginalTargetText = targetNode.Text,
                        FormId = sourceNode.FormId,
                        FormNodeType = sourceNode.NodeType,
                        FormNodeId = sourceNode.NodeId,
                        FormLookupAttributeName = sourceNode.LookupAttributeName,
                        FormDisplayName = sourceNode.FormName
                    });
                }
            }

            return rows;
        }

        private static Dictionary<string, FormNodeSnapshot> ReadFormSnapshotForLanguage(IOrganizationService service, string entityLogicalName, int lcid)
        {
            using (new TemporaryUserLanguageScope(service, lcid))
            {
                return ReadFormSnapshotForEntity(service, entityLogicalName, lcid);
            }
        }

        private static Dictionary<string, FormNodeSnapshot> ReadFormSnapshotForEntity(IOrganizationService service, string entityLogicalName, int lcid)
        {
            Dictionary<string, FormNodeSnapshot> snapshot = new Dictionary<string, FormNodeSnapshot>(StringComparer.OrdinalIgnoreCase);

            QueryExpression query = new QueryExpression("systemform")
            {
                ColumnSet = new ColumnSet("formid", "name", "formxml", "type", "objecttypecode"),
                NoLock = true,
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("objecttypecode", ConditionOperator.Equal, entityLogicalName)
                    }
                }
            };

            foreach (Entity form in RetrieveAll(service, query).OrderBy(item => item.GetAttributeValue<string>("name") ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                Guid formId = form.Id;
                string formName = form.GetAttributeValue<string>("name") ?? string.Empty;

                AddSnapshot(snapshot, new FormNodeSnapshot
                {
                    FormId = formId,
                    FormName = formName,
                    NodeType = FormNodeType.FormName,
                    NodeId = "name",
                    LookupAttributeName = "name",
                    ParentPath = string.Empty,
                    DisplayKey = formId.ToString(),
                    Text = formName
                });

                string formXml = form.GetAttributeValue<string>("formxml");
                if (string.IsNullOrWhiteSpace(formXml))
                {
                    continue;
                }

                XmlDocument document = new XmlDocument();
                document.LoadXml(formXml);

                foreach (XmlNode tabNode in document.SelectNodes("//tab"))
                {
                    string tabId = GetNodeAttribute(tabNode, "id");
                    if (string.IsNullOrWhiteSpace(tabId))
                    {
                        continue;
                    }

                    string tabText = GetXmlNodeLabel(tabNode, lcid);
                    AddSnapshot(snapshot, new FormNodeSnapshot
                    {
                        FormId = formId,
                        FormName = formName,
                        NodeType = FormNodeType.Tab,
                        NodeId = tabId,
                        LookupAttributeName = "id",
                        ParentPath = formName,
                        DisplayKey = tabText,
                        Text = tabText
                    });

                    foreach (XmlNode sectionNode in tabNode.SelectNodes("columns/column/sections/section"))
                    {
                        string sectionId = GetNodeAttribute(sectionNode, "id");
                        if (string.IsNullOrWhiteSpace(sectionId))
                        {
                            continue;
                        }

                        string sectionText = GetXmlNodeLabel(sectionNode, lcid);
                        string sectionPath = formName + " / " + tabText;

                        AddSnapshot(snapshot, new FormNodeSnapshot
                        {
                            FormId = formId,
                            FormName = formName,
                            NodeType = FormNodeType.Section,
                            NodeId = sectionId,
                            LookupAttributeName = "id",
                            ParentPath = sectionPath,
                            DisplayKey = sectionText,
                            Text = sectionText
                        });

                        foreach (XmlNode cellNode in sectionNode.SelectNodes("rows/row/cell"))
                        {
                            AddFieldSnapshot(snapshot, cellNode, formId, formName, sectionPath + " / " + sectionText, lcid);
                        }
                    }
                }

                foreach (XmlNode cellNode in document.DocumentElement.SelectNodes("header/rows/row/cell"))
                {
                    AddFieldSnapshot(snapshot, cellNode, formId, formName, formName + " / Header", lcid);
                }

                foreach (XmlNode cellNode in document.DocumentElement.SelectNodes("footer/rows/row/cell"))
                {
                    AddFieldSnapshot(snapshot, cellNode, formId, formName, formName + " / Footer", lcid);
                }
            }

            return snapshot;
        }

        private static void AddFieldSnapshot(Dictionary<string, FormNodeSnapshot> snapshot, XmlNode cellNode, Guid formId, string formName, string parentPath, int lcid)
        {
            if (cellNode == null)
            {
                return;
            }

            string lookupAttributeName = null;
            string nodeId = GetCellLabelIdentifier(cellNode, out lookupAttributeName);
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return;
            }

            string fieldLabel = GetXmlNodeLabel(cellNode, lcid);
            string displayKey = GetFieldDisplayKey(cellNode, fieldLabel, nodeId);

            AddSnapshot(snapshot, new FormNodeSnapshot
            {
                FormId = formId,
                FormName = formName,
                NodeType = FormNodeType.Field,
                NodeId = nodeId,
                LookupAttributeName = lookupAttributeName,
                ParentPath = parentPath,
                DisplayKey = displayKey,
                Text = fieldLabel
            });
        }

        private static void AddSnapshot(Dictionary<string, FormNodeSnapshot> snapshot, FormNodeSnapshot item)
        {
            string key = BuildFormSnapshotKey(item.FormId, item.NodeType, item.NodeId, item.LookupAttributeName);
            snapshot[key] = item;
        }

        private static string BuildFormSnapshotKey(Guid formId, FormNodeType nodeType, string nodeId, string lookupAttributeName)
        {
            return formId.ToString("D") + "|" + nodeType + "|" + (nodeId ?? string.Empty).Trim().ToLowerInvariant() + "|" + (lookupAttributeName ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string GetFieldDisplayKey(XmlNode cellNode, string labelText, string fallbackId)
        {
            XmlNode controlNode = cellNode.SelectSingleNode("control");
            string fieldName = GetNodeAttribute(controlNode, "datafieldname");
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                fieldName = GetNodeAttribute(controlNode, "id");
            }

            if (!string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(labelText))
            {
                return fieldName + " (" + labelText + ")";
            }

            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                return fieldName;
            }

            return !string.IsNullOrWhiteSpace(labelText) ? labelText : fallbackId;
        }

        private static string GetCellLabelIdentifier(XmlNode cellNode, out string lookupAttributeName)
        {
            string labelId = GetNodeAttribute(cellNode, "labelid");
            if (!string.IsNullOrWhiteSpace(labelId))
            {
                lookupAttributeName = "labelid";
                return labelId;
            }

            string cellId = GetNodeAttribute(cellNode, "id");
            if (!string.IsNullOrWhiteSpace(cellId))
            {
                lookupAttributeName = "id";
                return cellId;
            }

            lookupAttributeName = null;
            return null;
        }

        private static string GetXmlNodeLabel(XmlNode node, int lcid)
        {
            if (node == null)
            {
                return string.Empty;
            }

            XmlNode specific = node.SelectSingleNode("labels/label[@languagecode='" + lcid.ToString(CultureInfo.InvariantCulture) + "']");
            if (specific != null)
            {
                return GetNodeAttribute(specific, "description") ?? string.Empty;
            }

            XmlNode first = node.SelectSingleNode("labels/label");
            if (first != null)
            {
                return GetNodeAttribute(first, "description") ?? string.Empty;
            }

            return string.Empty;
        }

        private static void SaveTableRows(IOrganizationService service, TranslationSession session, List<TranslationRowModel> rows, TranslationSaveResult result)
        {
            foreach (IGrouping<string, TranslationRowModel> group in rows.GroupBy(row => row.EntityLogicalName, StringComparer.OrdinalIgnoreCase))
            {
                RetrieveEntityRequest retrieveRequest = new RetrieveEntityRequest
                {
                    LogicalName = group.Key,
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = true
                };

                RetrieveEntityResponse retrieveResponse = (RetrieveEntityResponse)service.Execute(retrieveRequest);
                EntityMetadata entity = retrieveResponse.EntityMetadata;

                foreach (TranslationRowModel row in group)
                {
                    if (string.Equals(row.PropertyName, "DisplayName", StringComparison.OrdinalIgnoreCase))
                    {
                        UpsertLocalizedLabel(entity.DisplayName, session.Request.TargetLcid, row.TargetText);
                    }
                    else if (string.Equals(row.PropertyName, "DisplayCollectionName", StringComparison.OrdinalIgnoreCase))
                    {
                        UpsertLocalizedLabel(entity.DisplayCollectionName, session.Request.TargetLcid, row.TargetText);
                    }
                    else if (string.Equals(row.PropertyName, "Description", StringComparison.OrdinalIgnoreCase))
                    {
                        UpsertLocalizedLabel(entity.Description, session.Request.TargetLcid, row.TargetText);
                    }

                    result.PublishScope.Entities.Add(group.Key);
                }

                UpdateEntityRequest updateRequest = new UpdateEntityRequest
                {
                    Entity = entity,
                    MergeLabels = true,
                    SolutionUniqueName = session.Request.SolutionUniqueName
                };

                service.Execute(updateRequest);
            }
        }

        private static void SaveColumnRows(IOrganizationService service, TranslationSession session, List<TranslationRowModel> rows, TranslationSaveResult result)
        {
            foreach (IGrouping<string, TranslationRowModel> entityGroup in rows.GroupBy(row => row.EntityLogicalName, StringComparer.OrdinalIgnoreCase))
            {
                foreach (IGrouping<string, TranslationRowModel> attributeGroup in entityGroup.GroupBy(row => row.AttributeLogicalName, StringComparer.OrdinalIgnoreCase))
                {
                    RetrieveAttributeRequest retrieveRequest = new RetrieveAttributeRequest
                    {
                        EntityLogicalName = entityGroup.Key,
                        LogicalName = attributeGroup.Key,
                        RetrieveAsIfPublished = true
                    };

                    RetrieveAttributeResponse retrieveResponse = (RetrieveAttributeResponse)service.Execute(retrieveRequest);
                    AttributeMetadata attribute = retrieveResponse.AttributeMetadata;

                    foreach (TranslationRowModel row in attributeGroup)
                    {
                        if (string.Equals(row.PropertyName, "DisplayName", StringComparison.OrdinalIgnoreCase))
                        {
                            UpsertLocalizedLabel(attribute.DisplayName, session.Request.TargetLcid, row.TargetText);
                        }
                        else if (string.Equals(row.PropertyName, "Description", StringComparison.OrdinalIgnoreCase))
                        {
                            UpsertLocalizedLabel(attribute.Description, session.Request.TargetLcid, row.TargetText);
                        }
                    }

                    UpdateAttributeRequest updateRequest = new UpdateAttributeRequest
                    {
                        EntityName = entityGroup.Key,
                        Attribute = attribute,
                        MergeLabels = true,
                        SolutionUniqueName = session.Request.SolutionUniqueName
                    };

                    service.Execute(updateRequest);
                    result.PublishScope.Entities.Add(entityGroup.Key);
                }
            }
        }

        private static void SaveChoiceRows(IOrganizationService service, TranslationSession session, List<TranslationRowModel> rows, TranslationSaveResult result)
        {
            foreach (TranslationRowModel row in rows)
            {
                if (!row.OptionValue.HasValue)
                {
                    continue;
                }

                if (row.ChoiceKind == ChoiceTranslationKind.StateOption)
                {
                    UpdateStateValueRequest stateRequest = new UpdateStateValueRequest
                    {
                        EntityLogicalName = row.EntityLogicalName,
                        AttributeLogicalName = row.AttributeLogicalName,
                        Value = row.OptionValue.Value,
                        Label = new Microsoft.Xrm.Sdk.Label(row.TargetText ?? string.Empty, session.Request.TargetLcid),
                        MergeLabels = true
                    };

                    service.Execute(stateRequest);
                    result.PublishScope.Entities.Add(row.EntityLogicalName);
                    continue;
                }

                UpdateOptionValueRequest optionRequest = new UpdateOptionValueRequest
                {
                    Value = row.OptionValue.Value,
                    Label = new Microsoft.Xrm.Sdk.Label(row.TargetText ?? string.Empty, session.Request.TargetLcid),
                    MergeLabels = true,
                    SolutionUniqueName = session.Request.SolutionUniqueName
                };

                if (!string.IsNullOrWhiteSpace(row.GlobalChoiceName))
                {
                    optionRequest.OptionSetName = row.GlobalChoiceName;
                    result.PublishScope.GlobalChoices.Add(row.GlobalChoiceName);
                }
                else
                {
                    optionRequest.EntityLogicalName = row.EntityLogicalName;
                    optionRequest.AttributeLogicalName = row.AttributeLogicalName;
                    result.PublishScope.Entities.Add(row.EntityLogicalName);
                }

                service.Execute(optionRequest);
            }
        }

        private static void SaveFormRows(IOrganizationService service, TranslationSession session, List<TranslationRowModel> rows, TranslationSaveResult result)
        {
            int targetLcid = session.Request.TargetLcid;

            using (new TemporaryUserLanguageScope(service, targetLcid))
            {
                foreach (IGrouping<Guid, TranslationRowModel> formGroup in rows.Where(row => row.FormId.HasValue).GroupBy(row => row.FormId.Value))
                {
                    Entity form = service.Retrieve("systemform", formGroup.Key, new ColumnSet("formxml"));
                    string xml = form.GetAttributeValue<string>("formxml") ?? "<form />";
                    XmlDocument document = new XmlDocument();
                    document.LoadXml(xml);
                    bool xmlChanged = false;

                    foreach (TranslationRowModel row in formGroup)
                    {
                        if (row.FormNodeType == FormNodeType.FormName)
                        {
                            SetLocalizedRecordAttributeLabel(service, "systemform", formGroup.Key, "name", targetLcid, row.TargetText ?? string.Empty);
                        }
                        else
                        {
                            xmlChanged |= UpdateFormLabel(document, row, targetLcid, row.TargetText ?? string.Empty);
                        }

                        result.PublishScope.Entities.Add(row.EntityLogicalName);
                    }

                    if (xmlChanged)
                    {
                        form["formxml"] = document.OuterXml;
                        service.Update(form);
                    }
                }
            }
        }

        private static bool UpdateFormLabel(XmlDocument document, TranslationRowModel row, int lcid, string text)
        {
            XmlNode targetNode = null;
            string normalizedId = NormalizeXmlIdentifier(row.FormNodeId);

            if (row.FormNodeType == FormNodeType.Tab)
            {
                targetNode = document.SelectSingleNode("//tab[translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZ{}','abcdefghijklmnopqrstuvwxyz')='" + normalizedId + "']");
            }
            else if (row.FormNodeType == FormNodeType.Section)
            {
                targetNode = document.SelectSingleNode("//section[translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZ{}','abcdefghijklmnopqrstuvwxyz')='" + normalizedId + "']");
            }
            else if (row.FormNodeType == FormNodeType.Field)
            {
                string lookupAttribute = string.IsNullOrWhiteSpace(row.FormLookupAttributeName) ? "id" : row.FormLookupAttributeName;
                targetNode = document.SelectSingleNode("//cell[translate(@" + lookupAttribute + ",'ABCDEFGHIJKLMNOPQRSTUVWXYZ{}','abcdefghijklmnopqrstuvwxyz')='" + normalizedId + "']");
            }

            if (targetNode == null)
            {
                return false;
            }

            XmlNode labelsNode = targetNode.SelectSingleNode("labels");
            if (labelsNode == null)
            {
                labelsNode = document.CreateElement("labels");
                targetNode.AppendChild(labelsNode);
            }

            XmlNode labelNode = labelsNode.SelectSingleNode("label[@languagecode='" + lcid.ToString(CultureInfo.InvariantCulture) + "']");
            if (labelNode == null)
            {
                labelNode = document.CreateElement("label");
                XmlAttribute languageAttribute = document.CreateAttribute("languagecode");
                languageAttribute.Value = lcid.ToString(CultureInfo.InvariantCulture);
                labelNode.Attributes.Append(languageAttribute);
                labelsNode.AppendChild(labelNode);
            }

            SetOrCreateXmlAttribute(document, labelNode, "description", text ?? string.Empty);
            return true;
        }

        private static void SetOrCreateXmlAttribute(XmlDocument document, XmlNode node, string attributeName, string value)
        {
            if (node == null)
            {
                return;
            }

            XmlAttribute attribute = node.Attributes[attributeName] ?? document.CreateAttribute(attributeName);
            attribute.Value = value ?? string.Empty;
            if (node.Attributes[attributeName] == null)
            {
                node.Attributes.Append(attribute);
            }
        }

        private static void SetLocalizedRecordAttributeLabel(IOrganizationService service, string entityLogicalName, Guid recordId, string attributeName, int lcid, string text)
        {
            RetrieveLocLabelsRequest retrieveRequest = new RetrieveLocLabelsRequest
            {
                EntityMoniker = new EntityReference(entityLogicalName, recordId),
                AttributeName = attributeName,
                IncludeUnpublished = true
            };

            RetrieveLocLabelsResponse retrieveResponse = (RetrieveLocLabelsResponse)service.Execute(retrieveRequest);
            List<LocalizedLabel> labels = retrieveResponse.Label != null && retrieveResponse.Label.LocalizedLabels != null
                ? retrieveResponse.Label.LocalizedLabels.ToList()
                : new List<LocalizedLabel>();

            LocalizedLabel existing = labels.FirstOrDefault(label => label.LanguageCode == lcid);
            if (existing == null)
            {
                labels.Add(new LocalizedLabel(text ?? string.Empty, lcid));
            }
            else
            {
                existing.Label = text ?? string.Empty;
            }

            SetLocLabelsRequest updateRequest = new SetLocLabelsRequest
            {
                EntityMoniker = new EntityReference(entityLogicalName, recordId),
                AttributeName = attributeName,
                Labels = labels.ToArray()
            };

            service.Execute(updateRequest);
        }

        private static string NormalizeXmlIdentifier(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            Guid parsed;
            if (Guid.TryParse(id, out parsed))
            {
                return parsed.ToString("D");
            }

            return id.Trim().Trim('{', '}').ToLowerInvariant();
        }

        private static List<SolutionItem> GetUnmanagedSolutions(IOrganizationService service)
        {
            QueryExpression query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("friendlyname", "uniquename", "solutionid", "version"),
                NoLock = true,
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("ismanaged", ConditionOperator.Equal, false)
                    }
                }
            };
            query.Orders.Add(new OrderExpression("friendlyname", OrderType.Ascending));

            return RetrieveAll(service, query)
                .Select(solution => new SolutionItem
                {
                    SolutionId = solution.Id,
                    UniqueName = solution.GetAttributeValue<string>("uniquename"),
                    DisplayName = BuildSolutionDisplayName(solution)
                })
                .Where(solution => !string.IsNullOrWhiteSpace(solution.UniqueName))
                .ToList();
        }

        private static string BuildSolutionDisplayName(Entity solution)
        {
            string friendlyName = solution.GetAttributeValue<string>("friendlyname");
            string uniqueName = solution.GetAttributeValue<string>("uniquename");
            string version = solution.GetAttributeValue<string>("version");

            if (!string.IsNullOrWhiteSpace(friendlyName) && !string.IsNullOrWhiteSpace(version))
            {
                return friendlyName + " (" + uniqueName + ", " + version + ")";
            }

            if (!string.IsNullOrWhiteSpace(friendlyName))
            {
                return friendlyName + " (" + uniqueName + ")";
            }

            return uniqueName;
        }

        private static int GetBaseLanguageCode(IOrganizationService service)
        {
            QueryExpression query = new QueryExpression("organization")
            {
                ColumnSet = new ColumnSet("languagecode"),
                NoLock = true,
                TopCount = 1
            };

            Entity organization = service.RetrieveMultiple(query).Entities.FirstOrDefault();
            return organization != null ? organization.GetAttributeValue<int>("languagecode") : 1033;
        }

        private static List<int> GetProvisionedLanguages(IOrganizationService service)
        {
            RetrieveProvisionedLanguagesResponse response = (RetrieveProvisionedLanguagesResponse)service.Execute(new RetrieveProvisionedLanguagesRequest());
            return response.RetrieveProvisionedLanguages != null
                ? response.RetrieveProvisionedLanguages.Distinct().ToList()
                : new List<int>();
        }

        private static List<EntityItem> GetAllEntityItems(IOrganizationService service, int baseLanguageCode)
        {
            RetrieveAllEntitiesResponse response = (RetrieveAllEntitiesResponse)service.Execute(new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity,
                RetrieveAsIfPublished = true
            });

            return response.EntityMetadata
                .Where(ShouldIncludeEntity)
                .OrderBy(entity => GetBestLabelText(entity.DisplayName, baseLanguageCode, entity.LogicalName), StringComparer.OrdinalIgnoreCase)
                .Select(entity => new EntityItem
                {
                    LogicalName = entity.LogicalName,
                    DisplayName = GetBestLabelText(entity.DisplayName, baseLanguageCode, entity.LogicalName) + " (" + entity.LogicalName + ")"
                })
                .ToList();
        }

        private static List<EntityMetadata> GetRelevantEntityHeaders(IOrganizationService service, string solutionUniqueName, string selectedEntityLogicalName)
        {
            List<EntityMetadata> entities;

            if (string.IsNullOrWhiteSpace(solutionUniqueName))
            {
                RetrieveAllEntitiesResponse response = (RetrieveAllEntitiesResponse)service.Execute(new RetrieveAllEntitiesRequest
                {
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = true
                });

                entities = response.EntityMetadata.Where(ShouldIncludeEntity).ToList();
            }
            else
            {
                Guid solutionId = GetSolutionId(service, solutionUniqueName);
                if (solutionId == Guid.Empty)
                {
                    return new List<EntityMetadata>();
                }

                List<Guid> metadataIds = RetrieveAll(service, new QueryExpression("solutioncomponent")
                {
                    ColumnSet = new ColumnSet("objectid"),
                    NoLock = true,
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId),
                            new ConditionExpression("componenttype", ConditionOperator.Equal, 1)
                        }
                    }
                })
                .Select(component => component.GetAttributeValue<Guid>("objectid"))
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

                entities = new List<EntityMetadata>();
                int page = 0;
                List<Guid> batch = metadataIds.Skip(page * 100).Take(100).ToList();

                while (batch.Count > 0)
                {
                    EntityQueryExpression query = new EntityQueryExpression
                    {
                        Criteria = new MetadataFilterExpression(LogicalOperator.Or),
                        Properties = new MetadataPropertiesExpression
                        {
                            AllProperties = false,
                            PropertyNames = { "MetadataId", "LogicalName", "DisplayName", "DisplayCollectionName", "Description" }
                        }
                    };

                    foreach (Guid metadataId in batch)
                    {
                        query.Criteria.Conditions.Add(new MetadataConditionExpression("MetadataId", MetadataConditionOperator.Equals, metadataId));
                    }

                    RetrieveMetadataChangesResponse response = (RetrieveMetadataChangesResponse)service.Execute(new RetrieveMetadataChangesRequest
                    {
                        Query = query,
                        ClientVersionStamp = null
                    });

                    entities.AddRange(response.EntityMetadata.Where(ShouldIncludeEntity));
                    page++;
                    batch = metadataIds.Skip(page * 100).Take(100).ToList();
                }
            }

            if (!string.IsNullOrWhiteSpace(selectedEntityLogicalName))
            {
                entities = entities
                    .Where(entity => string.Equals(entity.LogicalName, selectedEntityLogicalName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return entities.OrderBy(entity => entity.LogicalName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static Guid GetSolutionId(IOrganizationService service, string solutionUniqueName)
        {
            QueryExpression query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid"),
                NoLock = true,
                TopCount = 1,
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, solutionUniqueName)
                    }
                }
            };

            Entity solution = service.RetrieveMultiple(query).Entities.FirstOrDefault();
            return solution != null ? solution.Id : Guid.Empty;
        }

        private static EntityMetadata RetrieveEntity(IOrganizationService service, string logicalName, EntityFilters filters)
        {
            RetrieveEntityResponse response = (RetrieveEntityResponse)service.Execute(new RetrieveEntityRequest
            {
                LogicalName = logicalName,
                EntityFilters = filters,
                RetrieveAsIfPublished = true
            });

            return response.EntityMetadata;
        }

        private static List<EntityMetadata> RetrieveEntitiesWithAttributes(IOrganizationService service, List<EntityMetadata> entityHeaders, BackgroundWorker worker, string progressLabel)
        {
            List<EntityMetadata> result = new List<EntityMetadata>();
            const int batchSize = 50;
            int totalCount = entityHeaders.Count;
            int processed = 0;

            for (int offset = 0; offset < totalCount; offset += batchSize)
            {
                List<EntityMetadata> batch = entityHeaders.Skip(offset).Take(batchSize).ToList();
                processed += batch.Count;
                worker.ReportProgressIfPossible(0, progressLabel + " (" + processed.ToString(CultureInfo.InvariantCulture) + "/" + totalCount.ToString(CultureInfo.InvariantCulture) + ")...");

                EntityQueryExpression query = new EntityQueryExpression
                {
                    Criteria = new MetadataFilterExpression(LogicalOperator.Or),
                    Properties = new MetadataPropertiesExpression { AllProperties = true },
                    AttributeQuery = new AttributeQueryExpression
                    {
                        Properties = new MetadataPropertiesExpression { AllProperties = true }
                    }
                };

                foreach (EntityMetadata header in batch)
                {
                    query.Criteria.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, header.LogicalName));
                }

                RetrieveMetadataChangesResponse response = (RetrieveMetadataChangesResponse)service.Execute(new RetrieveMetadataChangesRequest
                {
                    Query = query,
                    ClientVersionStamp = null
                });

                result.AddRange(response.EntityMetadata);
            }

            return result;
        }

        private static bool ShouldIncludeEntity(EntityMetadata entity)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.LogicalName))
            {
                return false;
            }

            if (entity.IsIntersect.HasValue && entity.IsIntersect.Value)
            {
                return false;
            }

            return entity.IsCustomizable == null || entity.IsCustomizable.Value || !(entity.IsManaged.HasValue && entity.IsManaged.Value);
        }

        private static bool ShouldIncludeAttribute(AttributeMetadata attribute)
        {
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.LogicalName))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(attribute.AttributeOf))
            {
                return false;
            }

            return attribute.IsCustomizable == null || attribute.IsCustomizable.Value;
        }

        private static void UpsertLocalizedLabel(Microsoft.Xrm.Sdk.Label label, int lcid, string text)
        {
            if (label == null)
            {
                return;
            }

            LocalizedLabel existing = label.LocalizedLabels.FirstOrDefault(l => l.LanguageCode == lcid);
            if (existing != null)
            {
                existing.Label = text ?? string.Empty;
            }
            else
            {
                label.LocalizedLabels.Add(new LocalizedLabel(text ?? string.Empty, lcid));
            }
        }

        private static string GetExactLabelText(Microsoft.Xrm.Sdk.Label label, int lcid)
        {
            if (label == null || label.LocalizedLabels == null)
            {
                return string.Empty;
            }

            LocalizedLabel localized = label.LocalizedLabels.FirstOrDefault(item => item.LanguageCode == lcid);
            return localized != null ? localized.Label ?? string.Empty : string.Empty;
        }

        private static string GetBestLabelText(Microsoft.Xrm.Sdk.Label label, int lcid, string fallback)
        {
            string exact = GetExactLabelText(label, lcid);
            if (!string.IsNullOrWhiteSpace(exact))
            {
                return exact;
            }

            if (label != null && label.UserLocalizedLabel != null && !string.IsNullOrWhiteSpace(label.UserLocalizedLabel.Label))
            {
                return label.UserLocalizedLabel.Label;
            }

            LocalizedLabel first = label != null && label.LocalizedLabels != null ? label.LocalizedLabels.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Label)) : null;
            if (first != null)
            {
                return first.Label;
            }

            return fallback ?? string.Empty;
        }

        private static string BuildLanguageDisplayName(int lcid, bool isBaseLanguage)
        {
            string cultureName;

            try
            {
                cultureName = CultureInfo.GetCultureInfo(lcid).EnglishName;
            }
            catch
            {
                cultureName = "LCID " + lcid.ToString(CultureInfo.InvariantCulture);
            }

            return lcid.ToString(CultureInfo.InvariantCulture) + " - " + cultureName + (isBaseLanguage ? " (Base)" : string.Empty);
        }

        private static string GetNodeAttribute(XmlNode node, string attributeName)
        {
            return node != null && node.Attributes != null && node.Attributes[attributeName] != null
                ? node.Attributes[attributeName].Value
                : string.Empty;
        }

        private static List<Entity> RetrieveAll(IOrganizationService service, QueryExpression query)
        {
            List<Entity> entities = new List<Entity>();
            query.PageInfo = new PagingInfo
            {
                Count = 5000,
                PageNumber = 1,
                PagingCookie = null
            };

            while (true)
            {
                EntityCollection response = service.RetrieveMultiple(query);
                entities.AddRange(response.Entities);

                if (!response.MoreRecords)
                {
                    break;
                }

                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = response.PagingCookie;
            }

            return entities;
        }

        private sealed class TemporaryUserLanguageScope : IDisposable
        {
            private readonly IOrganizationService _service;
            private readonly Entity _settings;
            private readonly int _originalLocale;
            private readonly int _originalUiLanguage;
            private readonly int _originalHelpLanguage;
            private readonly bool _changed;

            public TemporaryUserLanguageScope(IOrganizationService service, int targetLcid)
            {
                _service = service;
                _settings = RetrieveCurrentUserSettings(service);

                _originalLocale = _settings.GetAttributeValue<int>("localeid");
                _originalUiLanguage = _settings.GetAttributeValue<int>("uilanguageid");
                _originalHelpLanguage = _settings.GetAttributeValue<int>("helplanguageid");

                if (_originalLocale == targetLcid && _originalUiLanguage == targetLcid && _originalHelpLanguage == targetLcid)
                {
                    _changed = false;
                    return;
                }

                Entity update = new Entity("usersettings") { Id = _settings.Id };
                update["localeid"] = targetLcid;
                update["uilanguageid"] = targetLcid;
                update["helplanguageid"] = targetLcid;
                service.Update(update);
                Thread.Sleep(1000);
                _changed = true;
            }

            public void Dispose()
            {
                if (!_changed)
                {
                    return;
                }

                Entity update = new Entity("usersettings") { Id = _settings.Id };
                update["localeid"] = _originalLocale;
                update["uilanguageid"] = _originalUiLanguage;
                update["helplanguageid"] = _originalHelpLanguage;
                _service.Update(update);
                Thread.Sleep(750);
            }

            private static Entity RetrieveCurrentUserSettings(IOrganizationService service)
            {
                WhoAmIResponse whoAmI = (WhoAmIResponse)service.Execute(new WhoAmIRequest());
                QueryExpression query = new QueryExpression("usersettings")
                {
                    ColumnSet = new ColumnSet("localeid", "uilanguageid", "helplanguageid"),
                    NoLock = true,
                    TopCount = 1,
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("systemuserid", ConditionOperator.Equal, whoAmI.UserId)
                        }
                    }
                };

                Entity settings = service.RetrieveMultiple(query).Entities.FirstOrDefault();
                if (settings == null)
                {
                    throw new InvalidOperationException("Unable to retrieve the current user's UI language settings.");
                }

                return settings;
            }
        }
    }

    internal sealed class EnvironmentContextInfo
    {
        public int BaseLanguageCode { get; set; }
        public List<SolutionItem> Solutions { get; set; } = new List<SolutionItem>();
        public List<LanguageItem> Languages { get; set; } = new List<LanguageItem>();
        public List<EntityItem> Entities { get; set; } = new List<EntityItem>();
    }

    internal sealed class TranslationLoadRequest
    {
        public TranslationScope Scope { get; set; }
        public string SolutionUniqueName { get; set; }
        public string EntityLogicalName { get; set; }
        public int SourceLcid { get; set; }
        public int TargetLcid { get; set; }
    }

    internal sealed class TranslationSession
    {
        public TranslationLoadRequest Request { get; set; }
        public List<TranslationRowModel> Rows { get; set; } = new List<TranslationRowModel>();
        public TranslationWorkbook PackageWorkbook { get; set; }
    }

    internal sealed class TranslationSaveResult
    {
        public int SavedRows { get; set; }
        public string SummaryMessage { get; set; }
        public PendingPublishScope PublishScope { get; } = new PendingPublishScope();
    }

    internal sealed class PendingPublishScope
    {
        public HashSet<string> Entities { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> GlobalChoices { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> NonPublishableComponents { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool RequiresPublishAllFallback { get; set; }
        public bool IsEmpty => Entities.Count == 0 && GlobalChoices.Count == 0 && NonPublishableComponents.Count == 0;

        public void Merge(PendingPublishScope other)
        {
            if (other == null)
            {
                return;
            }

            foreach (string entity in other.Entities)
            {
                Entities.Add(entity);
            }

            foreach (string optionSet in other.GlobalChoices)
            {
                GlobalChoices.Add(optionSet);
            }

            foreach (string component in other.NonPublishableComponents)
            {
                NonPublishableComponents.Add(component);
            }

            RequiresPublishAllFallback |= other.RequiresPublishAllFallback;
        }

        public PendingPublishScope Clone()
        {
            PendingPublishScope clone = new PendingPublishScope();
            clone.Merge(this);
            return clone;
        }

        public void Clear()
        {
            Entities.Clear();
            GlobalChoices.Clear();
            NonPublishableComponents.Clear();
            RequiresPublishAllFallback = false;
        }
    }

    internal sealed class TranslationRowModel
    {
        public Guid RowId { get; } = Guid.NewGuid();
        public TranslationScope Scope { get; set; }
        public string EntityLogicalName { get; set; }
        public string EntityDisplayName { get; set; }
        public string ParentName { get; set; }
        public string GridKey { get; set; }
        public string PropertyName { get; set; }
        public string SourceText { get; set; }
        public string TargetText { get; set; }
        public string OriginalTargetText { get; set; }
        public string AttributeLogicalName { get; set; }
        public string GlobalChoiceName { get; set; }
        public int? OptionValue { get; set; }
        public ChoiceTranslationKind ChoiceKind { get; set; }
        public Guid? FormId { get; set; }
        public FormNodeType FormNodeType { get; set; }
        public string FormNodeId { get; set; }
        public string FormLookupAttributeName { get; set; }
        public string FormDisplayName { get; set; }
        public string KeyDisplayName { get; set; }
        public TranslationEntry PackageEntry { get; set; }

        public string EntityGridDisplayName => string.IsNullOrWhiteSpace(EntityDisplayName) ? (string.IsNullOrWhiteSpace(EntityLogicalName) ? "(Global)" : EntityLogicalName) : EntityDisplayName;
        public string EntityFilterDisplayName => string.IsNullOrWhiteSpace(EntityDisplayName) ? EntityLogicalName : EntityDisplayName + " (" + EntityLogicalName + ")";
        public bool IsModified => !TextEquals(TargetText, OriginalTargetText);

        public void AcceptChanges()
        {
            OriginalTargetText = TargetText ?? string.Empty;
        }

        public static bool TextEquals(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
        }
    }

    internal sealed class TranslationWorkbook
    {
        public string SolutionUniqueName { get; set; }
        public byte[] PackageZipBytes { get; set; }
        public XmlDocument WorkbookDocument { get; set; }
    }

    internal sealed class TranslationEntry
    {
        public string WorksheetName { get; set; }
        public int RowIndex { get; set; }
        public int SourceColumnIndex { get; set; }
        public int TargetColumnIndex { get; set; }
    }

    internal sealed class BulkExcelImportRow
    {
        public int RowNumber { get; set; }
        public string Scope { get; set; }
        public string Entity { get; set; }
        public string Parent { get; set; }
        public string Key { get; set; }
        public string Property { get; set; }
        public string Target { get; set; }

        public bool IsCompletelyEmpty =>
            string.IsNullOrWhiteSpace(Scope) &&
            string.IsNullOrWhiteSpace(Entity) &&
            string.IsNullOrWhiteSpace(Parent) &&
            string.IsNullOrWhiteSpace(Key) &&
            string.IsNullOrWhiteSpace(Property) &&
            string.IsNullOrWhiteSpace(Target);
    }

    internal sealed class BulkExcelImportResult
    {
        public string WorksheetName { get; set; }
        public int ReadRows { get; set; }
        public int ChangedRows { get; set; }
        public int UnchangedRows { get; set; }
        public int NotFoundRows { get; set; }
        public int AmbiguousRows { get; set; }
        public int InvalidRows { get; set; }
        public int DuplicateInputRows { get; set; }
        public List<string> NotFoundExamples { get; } = new List<string>();
        public List<string> AmbiguousExamples { get; } = new List<string>();
        public List<string> InvalidExamples { get; } = new List<string>();
        public List<string> DuplicateExamples { get; } = new List<string>();
    }

    internal sealed class SolutionItem
    {
        public Guid SolutionId { get; set; }
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public bool IsAll { get; set; }

        public static SolutionItem CreateAll()
        {
            return new SolutionItem
            {
                DisplayName = "(All unmanaged metadata)",
                UniqueName = null,
                IsAll = true
            };
        }
    }

    internal sealed class LanguageItem
    {
        public int Lcid { get; set; }
        public string DisplayName { get; set; }
        public bool IsBaseLanguage { get; set; }
    }

    internal sealed class EntityItem
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public bool IsAll { get; set; }

        public static EntityItem CreateAll()
        {
            return new EntityItem
            {
                LogicalName = null,
                DisplayName = "(All)",
                IsAll = true
            };
        }
    }

    internal sealed class FilterItem
    {
        public string Value { get; set; }
        public string DisplayName { get; set; }
        public bool IsAll { get; set; }

        public static FilterItem CreateAll()
        {
            return new FilterItem
            {
                Value = null,
                DisplayName = "(All)",
                IsAll = true
            };
        }
    }

    internal sealed class ScopeOption
    {
        public TranslationScope Value { get; set; }
        public string DisplayName { get; set; }
    }

    internal sealed class FormNodeSnapshot
    {
        public Guid FormId { get; set; }
        public string FormName { get; set; }
        public FormNodeType NodeType { get; set; }
        public string NodeId { get; set; }
        public string LookupAttributeName { get; set; }
        public string ParentPath { get; set; }
        public string DisplayKey { get; set; }
        public string Text { get; set; }
    }

    internal sealed class EntityItemComparer : IEqualityComparer<EntityItem>
    {
        public bool Equals(EntityItem x, EntityItem y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return string.Equals(x.LogicalName, y.LogicalName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(EntityItem obj)
        {
            return obj != null && !string.IsNullOrWhiteSpace(obj.LogicalName)
                ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.LogicalName)
                : 0;
        }
    }

    internal enum TranslationScope
    {
        Table,
        Column,
        Choice,
        Form,
        Package,
        Combined
    }

    internal enum ChoiceTranslationKind
    {
        LocalOption,
        GlobalOption,
        BooleanOption,
        StateOption
    }

    internal enum FormNodeType
    {
        FormName,
        Tab,
        Section,
        Field
    }

    internal static class TranslationScopeExtensions
    {
        public static string GetDisplayName(this TranslationScope scope)
        {
            switch (scope)
            {
                case TranslationScope.Table:
                    return "Tables";
                case TranslationScope.Column:
                    return "Columns";
                case TranslationScope.Choice:
                    return "Choices";
                case TranslationScope.Form:
                    return "Forms";
                case TranslationScope.Package:
                    return "Solution Package";
                case TranslationScope.Combined:
                    return "Tables + Choices + Forms";
                default:
                    return scope.ToString();
            }
        }
    }

    internal static class FormNodeTypeExtensions
    {
        public static string GetDisplayName(this FormNodeType nodeType)
        {
            switch (nodeType)
            {
                case FormNodeType.FormName:
                    return "FormName";
                case FormNodeType.Tab:
                    return "TabLabel";
                case FormNodeType.Section:
                    return "SectionLabel";
                case FormNodeType.Field:
                    return "FieldLabel";
                default:
                    return nodeType.ToString();
            }
        }
    }

    internal static class BackgroundWorkerExtensions
    {
        public static void ReportProgressIfPossible(this BackgroundWorker worker, int progressPercentage, string message)
        {
            if (worker != null && worker.WorkerReportsProgress)
            {
                worker.ReportProgress(progressPercentage, message);
            }
        }
    }
}
